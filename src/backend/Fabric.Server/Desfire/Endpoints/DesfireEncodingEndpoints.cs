using System.Text.Json;
using Fabric.Server.Core;
using Fabric.Server.Desfire.Application;
using Fabric.Server.Desfire.Contracts;
using Fabric.Server.Desfire.Domain;
using Fabric.Server.Desfire.Persistence;
using Fabric.Server.Printing.Domain;
using Fabric.Server.Printing.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Desfire.Endpoints;

public static class DesfireEncodingEndpoints
{
    public static IEndpointRouteBuilder MapDesfireEncodingEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder batches = app.MapGroup("/api/desfire/badge-batches");
        batches.MapGet("", ListBatches).Produces<Page<BadgeBatchResponse>>();
        batches.MapPost("", CreateBatch).Produces<BadgeBatchResponse>(StatusCodes.Status201Created);
        batches.MapGet("/{id:guid}", GetBatch).Produces<BadgeBatchResponse>().Produces(StatusCodes.Status404NotFound);

        RouteGroupBuilder jobs = app.MapGroup("/api/desfire/badge-jobs");
        jobs.MapGet("", ListRuns).Produces<Page<BadgeJobResponse>>();
        jobs.MapGet("/{id:guid}", GetRun).Produces<BadgeJobResponse>().Produces(StatusCodes.Status404NotFound);

        jobs.MapPost("", CreateBadgeJob).Produces<BadgeJobResponse>().Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        return app;
    }

    private static async Task<IResult> ListBatches([AsParameters] BaseListRequest request, DesfireDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<BadgeBatch> result = await db.BadgeBatches.AsNoTracking().OrderByDescending(batch => batch.CreatedAt).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        Dictionary<Guid, BadgeBatchJobSummary> summaries = await GetBatchRunSummariesAsync(db, result.Items.Select(batch => batch.Id).ToArray(), cancellationToken);
        return Results.Ok(result.Map(batch => batch.ToResponse(summaries.GetValueOrDefault(batch.Id))));
    }

    private static async Task<IResult> CreateBatch([FromBody] CreateBadgeBatchRequest request, DesfireDbContext db, PrintingDbContext printingDb, DesfireEncodingWakeChannel wakeChannel, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.Problem("Print batch name is required.", statusCode: StatusCodes.Status400BadRequest);

        if (request.TransformationId is null && request.PrintDesignId is null)
            return Results.Problem("Badge batch must specify transformation, print design, or both.", statusCode: StatusCodes.Status400BadRequest);

        Transformation? transformation = null;
        if (request.TransformationId is not null)
        {
            transformation = await db.Transformations.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == request.TransformationId, cancellationToken);
            if (transformation is null)
                return Results.Problem("Transformation does not exist.", statusCode: StatusCodes.Status409Conflict);
        }

        DesfireEncoder? encoder = await db.Encoders.AsNoTracking().SingleOrDefaultAsync(encoder => encoder.Id == request.EncoderId, cancellationToken);
        if (encoder is null)
            return Results.Problem("Encoder does not exist.", statusCode: StatusCodes.Status409Conflict);

        if (!encoder.Enabled)
            return Results.Problem("Encoder is disabled.", statusCode: StatusCodes.Status409Conflict);

        if (request.TransformationId is not null && !encoder.SupportsEncoding)
            return Results.Problem("Selected encoder does not support encoding.", statusCode: StatusCodes.Status409Conflict);

        if (request.PrintDesignId is not null)
        {
            PrintDesign? printDesign = await printingDb.PrintDesigns.AsNoTracking().SingleOrDefaultAsync(design => design.Id == request.PrintDesignId, cancellationToken);
            if (printDesign is null)
                return Results.Problem("Print design does not exist.", statusCode: StatusCodes.Status409Conflict);

            if (printDesign.SurfaceKind != PrintSurfaceKind.Card)
                return Results.Problem("Print design must target card surface.", statusCode: StatusCodes.Status409Conflict);

            if (!encoder.SupportsPrinting)
                return Results.Problem("Selected encoder does not support card printing.", statusCode: StatusCodes.Status409Conflict);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        BadgeBatch batch = BadgeBatch.Create(
            request.Name,
            encoder.Id,
            request.TransformationId,
            request.PrintDesignId,
            JsonSerializer.Serialize(request.OriginalInput, DesfireJson.Options),
            JsonSerializer.Serialize(request.NormalizedRows, DesfireJson.Options),
            now);

        db.BadgeBatches.Add(batch);
        if (request.NormalizedRows.ValueKind != JsonValueKind.Array)
            return Results.Problem("Normalized rows must be a JSON array.", statusCode: StatusCodes.Status400BadRequest);

        foreach (JsonElement row in request.NormalizedRows.EnumerateArray())
        {
            db.BadgeJobs.Add(BadgeJob.Create(
                request.TransformationId,
                batch.Id,
                encoder.Id,
                request.PrintDesignId,
                null,
                BadgeJobKind.Batch,
                DesfireEncodingSources.BadgeBatch,
                JsonSerializer.Serialize(row, DesfireJson.Options),
                transformation?.VariableConfigsJson ?? "[]",
                encoder.AgentId,
                encoder.DeviceId,
                request.Priority,
                now));
        }

        await db.SaveChangesAsync(cancellationToken);
        wakeChannel.Signal();
        return Results.Created($"/api/desfire/badge-batches/{batch.Id}", batch.ToResponse());
    }

    private static async Task<IResult> GetBatch(Guid id, DesfireDbContext db, CancellationToken cancellationToken = default)
    {
        BadgeBatch? batch = await db.BadgeBatches.AsNoTracking().SingleOrDefaultAsync(batch => batch.Id == id, cancellationToken);
        if (batch is null)
            return Results.NotFound();

        Dictionary<Guid, BadgeBatchJobSummary> summaries = await GetBatchRunSummariesAsync(db, [batch.Id], cancellationToken);
        return Results.Ok(batch.ToResponse(summaries.GetValueOrDefault(batch.Id)));
    }

    private static async Task<IResult> ListRuns([AsParameters] BaseListRequest request, [FromQuery] Guid? transformationId, [FromQuery] Guid? batchId, [FromQuery] string? cardUid, [FromQuery] string? source, DesfireDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<BadgeJob> query = db.BadgeJobs.AsNoTracking();
        if (transformationId is not null)
            query = query.Where(run => run.TransformationId == transformationId);

        if (batchId is not null)
            query = query.Where(run => run.BatchId == batchId);

        if (!string.IsNullOrWhiteSpace(cardUid))
            query = query.Where(run => run.CardUid == cardUid);

        if (!string.IsNullOrWhiteSpace(source))
        {
            string normalizedSource = source.Trim().ToLowerInvariant();
            query = query.Where(run => run.Source == normalizedSource);
        }

        IPaged<BadgeJob> result = await query.OrderByDescending(run => run.RequestedAt).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(run => run.ToResponse()));
    }

    private static async Task<IResult> GetRun(Guid id, DesfireDbContext db, CancellationToken cancellationToken = default)
    {
        BadgeJob? run = await db.BadgeJobs.AsNoTracking().SingleOrDefaultAsync(run => run.Id == id, cancellationToken);
        return run is null ? Results.NotFound() : Results.Ok(run.ToResponse());
    }

    private static async Task<IResult> CreateBadgeJob([FromBody] CreateBadgeJobRequest request, DesfireEncodingService encodingService, CancellationToken cancellationToken = default)
    {
        DesfireEncodingResult result = await encodingService.CreateAdHocAsync(request, cancellationToken);
        if (result.Failure is not null)
            return result.Run is null ? result.Failure : Results.Json(result.Run.ToResponse(), statusCode: GetFailureStatusCode(result.Failure));

        return Results.Ok(result.Run!.ToResponse());
    }

    private static int GetFailureStatusCode(IResult failure) => failure switch
    {
        IStatusCodeHttpResult statusCodeResult when statusCodeResult.StatusCode is not null => statusCodeResult.StatusCode.Value,
        _ => StatusCodes.Status409Conflict
    };

    private static async Task<Dictionary<Guid, BadgeBatchJobSummary>> GetBatchRunSummariesAsync(DesfireDbContext db, IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken)
    {
        if (batchIds.Count == 0)
            return [];

        return await db.BadgeJobs
            .AsNoTracking()
            .Where(run => run.BatchId != null && batchIds.Contains(run.BatchId.Value))
            .GroupBy(run => run.BatchId!.Value)
            .Select(group => new BadgeBatchJobSummary(
                group.Key,
                group.Count(),
                group.Count(run => run.Status == BadgeJobStatus.Pending || run.Status == BadgeJobStatus.Claimed),
                group.Count(run => run.Status == BadgeJobStatus.Running),
                group.Count(run => run.Status == BadgeJobStatus.Succeeded),
                group.Count(run => run.Status == BadgeJobStatus.Failed || run.Status == BadgeJobStatus.Timeout || run.Status == BadgeJobStatus.DeviceUnavailable),
                group.Count(run => run.Status == BadgeJobStatus.Cancelled)))
            .ToDictionaryAsync(summary => summary.BatchId, cancellationToken);
    }
}
