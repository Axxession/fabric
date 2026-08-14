using Fabric.Hardware.Contracts.Capabilities;
using Fabric.Server.Core;
using Fabric.Server.Desfire.Contracts;
using Fabric.Server.Desfire.Domain;
using Fabric.Server.Desfire.Persistence;
using Fabric.Server.Hardware.Domain;
using Fabric.Server.Hardware.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Desfire.Endpoints;

public static class DesfireEncoderEndpoints
{
    public static IEndpointRouteBuilder MapDesfireEncoderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder encoders = app.MapGroup("/api/desfire/encoders");
        encoders.MapGet("", ListEncoders).Produces<Page<EncoderResponse>>();
        encoders.MapPost("", CreateEncoder).Produces<EncoderResponse>(StatusCodes.Status201Created);
        encoders.MapGet("/{id:guid}", GetEncoder).Produces<EncoderResponse>().Produces(StatusCodes.Status404NotFound);
        encoders.MapPut("/{id:guid}", UpdateEncoder).Produces<EncoderResponse>().Produces(StatusCodes.Status404NotFound);
        encoders.MapDelete("/{id:guid}", DeleteEncoder).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> ListEncoders([AsParameters] BaseListRequest request, DesfireDbContext db, HardwareDbContext hardwareDb, CancellationToken cancellationToken = default)
    {
        IPaged<DesfireEncoder> result = await db.Encoders.AsNoTracking().OrderBy(encoder => encoder.Name).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        Dictionary<(string AgentId, string DeviceId), HardwareDevice> devicesByBinding = await LoadDevicesByBindingAsync(hardwareDb, result.Items, cancellationToken);
        return Results.Ok(result.Map(encoder => encoder.ToResponse(devicesByBinding.GetValueOrDefault((encoder.AgentId, encoder.DeviceId)))));
    }

    private static async Task<IResult> CreateEncoder([FromBody] CreateEncoderRequest request, DesfireDbContext db, HardwareDbContext hardwareDb, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        EncoderHardwareValidation validation = await ValidateRequestAsync(request.Name, request.AgentId, request.DeviceId, hardwareDb, cancellationToken);
        if (validation.Failure is not null)
            return validation.Failure;

        string name = request.Name.Trim();
        string agentId = HardwareAgent.NormalizeId(request.AgentId);
        string deviceId = HardwareDevice.NormalizeDeviceId(request.DeviceId);
        bool exists = await db.Encoders.AnyAsync(encoder => encoder.Name == name || (encoder.AgentId == agentId && encoder.DeviceId == deviceId), cancellationToken);
        if (exists)
            return Results.Problem("Encoder name or hardware device is already in use.", statusCode: StatusCodes.Status409Conflict);

        DesfireEncoder encoder = DesfireEncoder.Create(name, agentId, deviceId, validation.SupportsEncoding, validation.SupportsPrinting, request.Enabled, timeProvider.GetUtcNow());
        db.Encoders.Add(encoder);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/desfire/encoders/{encoder.Id}", encoder.ToResponse());
    }

    private static async Task<IResult> GetEncoder(Guid id, DesfireDbContext db, HardwareDbContext hardwareDb, CancellationToken cancellationToken = default)
    {
        DesfireEncoder? encoder = await db.Encoders.AsNoTracking().SingleOrDefaultAsync(encoder => encoder.Id == id, cancellationToken);
        if (encoder is null)
            return Results.NotFound();

        HardwareDevice? device = await hardwareDb.Devices.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.AgentId == encoder.AgentId && candidate.DeviceId == encoder.DeviceId, cancellationToken);
        return Results.Ok(encoder.ToResponse(device));
    }

    private static async Task<IResult> UpdateEncoder(Guid id, [FromBody] UpdateEncoderRequest request, DesfireDbContext db, HardwareDbContext hardwareDb, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        DesfireEncoder? encoder = await db.Encoders.SingleOrDefaultAsync(encoder => encoder.Id == id, cancellationToken);
        if (encoder is null)
            return Results.NotFound();

        EncoderHardwareValidation validation = await ValidateRequestAsync(request.Name, request.AgentId, request.DeviceId, hardwareDb, cancellationToken);
        if (validation.Failure is not null)
            return validation.Failure;

        string name = request.Name.Trim();
        string agentId = HardwareAgent.NormalizeId(request.AgentId);
        string deviceId = HardwareDevice.NormalizeDeviceId(request.DeviceId);
        bool duplicate = await db.Encoders.AnyAsync(candidate => candidate.Id != id && (candidate.Name == name || (candidate.AgentId == agentId && candidate.DeviceId == deviceId)), cancellationToken);
        if (duplicate)
            return Results.Problem("Encoder name or hardware device is already in use.", statusCode: StatusCodes.Status409Conflict);

        bool bindingChanged = encoder.AgentId != agentId || encoder.DeviceId != deviceId;
        if (bindingChanged && await IsReferencedAsync(id, db, cancellationToken))
            return Results.Problem("Cannot change hardware binding after an encoder has been used by a print batch.", statusCode: StatusCodes.Status409Conflict);

        if (bindingChanged)
            encoder.Update(name, agentId, deviceId, validation.SupportsEncoding, validation.SupportsPrinting, request.Enabled, timeProvider.GetUtcNow());
        else
            encoder.UpdateMetadata(name, validation.SupportsEncoding, validation.SupportsPrinting, request.Enabled, timeProvider.GetUtcNow());

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(encoder.ToResponse());
    }

    private static async Task<IResult> DeleteEncoder(Guid id, DesfireDbContext db, CancellationToken cancellationToken = default)
    {
        DesfireEncoder? encoder = await db.Encoders.SingleOrDefaultAsync(encoder => encoder.Id == id, cancellationToken);
        if (encoder is null)
            return Results.NotFound();

        if (await IsReferencedAsync(id, db, cancellationToken))
            return Results.Problem("Cannot delete an encoder referenced by print history.", statusCode: StatusCodes.Status409Conflict);

        db.Encoders.Remove(encoder);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<EncoderHardwareValidation> ValidateRequestAsync(string name, string agentId, string deviceId, HardwareDbContext hardwareDb, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new EncoderHardwareValidation(false, false, Results.Problem("Encoder name is required.", statusCode: StatusCodes.Status400BadRequest));

        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(deviceId))
            return new EncoderHardwareValidation(false, false, Results.Problem("Encoder hardware device is required.", statusCode: StatusCodes.Status400BadRequest));

        string normalizedAgentId = HardwareAgent.NormalizeId(agentId);
        string normalizedDeviceId = HardwareDevice.NormalizeDeviceId(deviceId);
        HardwareDevice? device = await hardwareDb.Devices.AsNoTracking().SingleOrDefaultAsync(device => device.AgentId == normalizedAgentId && device.DeviceId == normalizedDeviceId, cancellationToken);
        if (device is null)
            return new EncoderHardwareValidation(false, false, Results.Problem("Hardware device does not exist.", statusCode: StatusCodes.Status409Conflict));

        bool supportsEncoding = SupportsFullEncodingWorkflow(device.Capabilities);
        bool supportsPrinting = device.Capabilities.Contains(HardwareCapabilities.CardPrint, StringComparer.OrdinalIgnoreCase);
        return supportsEncoding || SupportsPrintOnlyWorkflow(device.Capabilities)
            ? new EncoderHardwareValidation(true, supportsPrinting, null)
            : new EncoderHardwareValidation(false, false, Results.Problem("Hardware device must support either full RFID encoding workflow or print-only badge workflow.", statusCode: StatusCodes.Status409Conflict));
    }

    private static bool SupportsFullEncodingWorkflow(IReadOnlyList<string> capabilities) =>
        capabilities.Contains(HardwareCapabilities.CardPresent, StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains(HardwareCapabilities.RfidApduExchange, StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains(HardwareCapabilities.CardEject, StringComparer.OrdinalIgnoreCase);

    private static bool SupportsPrintOnlyWorkflow(IReadOnlyList<string> capabilities) =>
        capabilities.Contains(HardwareCapabilities.CardPresent, StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains(HardwareCapabilities.CardPrint, StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains(HardwareCapabilities.CardEject, StringComparer.OrdinalIgnoreCase);

    private static async Task<bool> IsReferencedAsync(Guid id, DesfireDbContext db, CancellationToken cancellationToken) =>
        await db.BadgeBatches.AnyAsync(batch => batch.EncoderId == id, cancellationToken)
        || await db.BadgeJobs.AnyAsync(run => run.EncoderId == id, cancellationToken);

    private static async Task<Dictionary<(string AgentId, string DeviceId), HardwareDevice>> LoadDevicesByBindingAsync(HardwareDbContext hardwareDb, IEnumerable<DesfireEncoder> encoders, CancellationToken cancellationToken)
    {
        DesfireEncoder[] encoderArray = encoders.ToArray();
        if (encoderArray.Length == 0)
            return [];

        string[] agentIds = encoderArray.Select(encoder => encoder.AgentId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        List<HardwareDevice> devices = await hardwareDb.Devices
            .AsNoTracking()
            .Where(device => agentIds.Contains(device.AgentId))
            .ToListAsync(cancellationToken);

        return devices.ToDictionary(device => (device.AgentId, device.DeviceId));
    }
}

public sealed record EncoderHardwareValidation(bool SupportsEncoding, bool SupportsPrinting, IResult? Failure);
