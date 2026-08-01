using Fabric.Server.Core;
using Fabric.Server.Reception.Application;
using Fabric.Server.Reception.Contracts;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Endpoints;

public static class ReceptionDeskWorkstationEndpoints
{
    public static IEndpointRouteBuilder MapReceptionDeskWorkstationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder workstations = app.MapGroup("/api/reception/workstations");

        workstations.MapGet("", ListReceptionDeskWorkstations)
            .WithDescription("List reception desk workstations")
            .WithSummary("List reception desk workstations")
            .Produces<Page<ReceptionDeskWorkstationResponse>>();
        workstations.MapGet("/{id:guid}", GetReceptionDeskWorkstation)
            .WithDescription("Retrieve a reception desk workstation")
            .WithSummary("Retrieve reception desk workstation")
            .Produces<ReceptionDeskWorkstationResponse>()
            .Produces(StatusCodes.Status404NotFound);
        workstations.MapPost("", CreateReceptionDeskWorkstation)
            .WithDescription("Create a reception desk workstation")
            .WithSummary("Create reception desk workstation")
            .Produces<ReceptionDeskWorkstationKeyResponse>(StatusCodes.Status201Created);
        workstations.MapPut("/{id:guid}", UpdateReceptionDeskWorkstation)
            .WithDescription("Update a reception desk workstation")
            .WithSummary("Update reception desk workstation")
            .Produces<ReceptionDeskWorkstationResponse>()
            .Produces(StatusCodes.Status404NotFound);
        workstations.MapPost("/{id:guid}/rotate-key", RotateReceptionDeskWorkstationKey)
            .WithDescription("Rotate a reception desk workstation API key")
            .WithSummary("Rotate reception desk workstation key")
            .Produces<ReceptionDeskWorkstationKeyResponse>()
            .Produces(StatusCodes.Status404NotFound);
        workstations.MapDelete("/{id:guid}", DisableReceptionDeskWorkstation)
            .WithDescription("Disable a reception desk workstation")
            .WithSummary("Disable reception desk workstation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListReceptionDeskWorkstations(
        [AsParameters] BaseListRequest request,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        IPaged<ReceptionDeskWorkstation> result = await db.ReceptionDeskWorkstations
            .AsNoTracking()
            .OrderBy(workstation => workstation.Name)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(workstation => workstation.ToResponse()));
    }

    private static async Task<IResult> GetReceptionDeskWorkstation(
        Guid id,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        ReceptionDeskWorkstation? workstation = await db.ReceptionDeskWorkstations
            .AsNoTracking()
            .SingleOrDefaultAsync(workstation => workstation.Id == id, cancellationToken);

        return workstation is null ? Results.NotFound() : Results.Ok(workstation.ToResponse());
    }

    private static async Task<IResult> CreateReceptionDeskWorkstation(
        [FromBody] CreateReceptionDeskWorkstationRequest request,
        ReceptionDbContext db,
        ReceptionKioskKeyHasher keyHasher,
        CancellationToken cancellationToken = default)
    {
        ReceptionKioskKey key = keyHasher.CreateKey();
        ReceptionDeskWorkstation workstation = ReceptionDeskWorkstation.Create(
            request.Name,
            request.LocationId,
            key.Hash,
            key.Salt);

        db.ReceptionDeskWorkstations.Add(workstation);
        await db.SaveChangesAsync(cancellationToken);

        var response = new ReceptionDeskWorkstationKeyResponse(workstation.ToResponse(), key.Key);
        return Results.Created($"/api/reception/workstations/{workstation.Id}", response);
    }

    private static async Task<IResult> UpdateReceptionDeskWorkstation(
        Guid id,
        [FromBody] UpdateReceptionDeskWorkstationRequest request,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        ReceptionDeskWorkstation? workstation = await db.ReceptionDeskWorkstations.SingleOrDefaultAsync(workstation => workstation.Id == id, cancellationToken);
        if (workstation is null)
            return Results.NotFound();

        workstation.Update(request.Name, request.LocationId, request.Enabled);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(workstation.ToResponse());
    }

    private static async Task<IResult> RotateReceptionDeskWorkstationKey(
        Guid id,
        ReceptionDbContext db,
        ReceptionKioskKeyHasher keyHasher,
        CancellationToken cancellationToken = default)
    {
        ReceptionDeskWorkstation? workstation = await db.ReceptionDeskWorkstations.SingleOrDefaultAsync(workstation => workstation.Id == id, cancellationToken);
        if (workstation is null)
            return Results.NotFound();

        ReceptionKioskKey key = keyHasher.CreateKey();
        workstation.RotateKey(key.Hash, key.Salt);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new ReceptionDeskWorkstationKeyResponse(workstation.ToResponse(), key.Key));
    }

    private static async Task<IResult> DisableReceptionDeskWorkstation(
        Guid id,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        ReceptionDeskWorkstation? workstation = await db.ReceptionDeskWorkstations.SingleOrDefaultAsync(workstation => workstation.Id == id, cancellationToken);
        if (workstation is null)
            return Results.NotFound();

        workstation.Disable();
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
