using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Contracts;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Endpoints;

public static class AccessGrantEndpoints
{
    public static IEndpointRouteBuilder MapAccessGrantEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder grants = app.MapGroup("/api/access-catalog/access-grants");

        grants.MapGet("", ListAccessGrants).Produces<Page<AccessGrantResponse>>();
        grants.MapPost("", CreateAccessGrant).Produces<AccessGrantResponse>(StatusCodes.Status201Created);
        grants.MapGet("/{accessGrantId:guid}", GetAccessGrant).Produces<AccessGrantResponse>().Produces(StatusCodes.Status404NotFound);
        grants.MapPost("/{accessGrantId:guid}/reconcile", ReconcileAccessGrant).Produces(StatusCodes.Status202Accepted).Produces(StatusCodes.Status404NotFound);
        grants.MapPost("/{accessGrantId:guid}/revoke", RevokeAccessGrant).Produces<AccessGrantResponse>().Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAccessGrants([AsParameters] ListAccessGrantsRequest request, AccessCatalogDbContext db, SagasDbContext sagasDb, CancellationToken cancellationToken = default)
    {
        IQueryable<AccessGrant> query = db.AccessGrants.AsNoTracking();
        if (request.IdentityId.HasValue)
            query = query.Where(item => item.IdentityId == request.IdentityId.Value);
        if (request.PackageId.HasValue)
            query = query.Where(item => item.PackageId == request.PackageId.Value);
        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);

        IPaged<AccessGrant> result = await query.OrderBy(item => item.ValidFrom).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        AccessGrant[] items = result.Items.ToArray();
        Dictionary<Guid, Guid[]> locations = await LoadLocations(db, items.Select(item => item.Id).ToArray(), cancellationToken);
        Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, items.Select(item => item.Id).ToArray(), cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse(locations.GetValueOrDefault(item.Id, []), outcomes.GetValueOrDefault(item.Id, []))));
    }

    private static async Task<IResult> CreateAccessGrant([FromBody] CreateAccessGrantRequest request, AccessGrantService service, AccessCatalogDbContext db, SagasDbContext sagasDb, CancellationToken cancellationToken = default)
    {
        Result<AccessGrant, AccessCatalogErrors> result = await service.CreateAsync(
            request.PackageId,
            request.IdentityId,
            request.LocationIds,
            request.AssignmentChannel,
            request.SourceKind,
            request.SourceId,
            request.DurationKind,
            request.ValidFrom,
            request.ValidUntil,
            request.ReasonText,
            cancellationToken);

        return await result.Match<Task<IResult>>(
            async item =>
            {
                Guid[] locationIds = await db.AccessGrantLocations.AsNoTracking().Where(link => link.AccessGrantId == item.Id).Select(link => link.LocationId).ToArrayAsync(cancellationToken);
                Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, [item.Id], cancellationToken);
                return Results.Created($"/api/access-catalog/access-grants/{item.Id}", item.ToResponse(locationIds, outcomes.GetValueOrDefault(item.Id, [])));
            },
            error => Task.FromResult(MapError(error).ToResult()));
    }

    private static async Task<IResult> GetAccessGrant(Guid accessGrantId, AccessCatalogDbContext db, SagasDbContext sagasDb, CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.AsNoTracking().SingleOrDefaultAsync(item => item.Id == accessGrantId, cancellationToken);
        if (grant is null)
            return Results.NotFound();

        Guid[] locationIds = await db.AccessGrantLocations.AsNoTracking().Where(link => link.AccessGrantId == accessGrantId).Select(link => link.LocationId).ToArrayAsync(cancellationToken);
        Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, [accessGrantId], cancellationToken);
        return Results.Ok(grant.ToResponse(locationIds, outcomes.GetValueOrDefault(accessGrantId, [])));
    }

    private static async Task<IResult> RevokeAccessGrant(Guid accessGrantId, AccessGrantService service, AccessCatalogDbContext db, SagasDbContext sagasDb, CancellationToken cancellationToken = default)
    {
        Result<AccessGrant, AccessCatalogErrors> result = await service.RevokeAsync(accessGrantId, cancellationToken);

        return await result.Match<Task<IResult>>(
            async item =>
            {
                Guid[] locationIds = await db.AccessGrantLocations.AsNoTracking().Where(link => link.AccessGrantId == item.Id).Select(link => link.LocationId).ToArrayAsync(cancellationToken);
                Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, [item.Id], cancellationToken);
                return Results.Ok(item.ToResponse(locationIds, outcomes.GetValueOrDefault(item.Id, [])));
            },
            error => Task.FromResult(MapError(error).ToResult()));
    }

    private static async Task<IResult> ReconcileAccessGrant(
        Guid accessGrantId,
        AccessCatalogDbContext db,
        AccessGrantProvisioningSagaService sagaService,
        CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.AsNoTracking().SingleOrDefaultAsync(item => item.Id == accessGrantId, cancellationToken);
        if (grant is null)
            return Results.NotFound();

        await sagaService.EnqueueAccessGrantCreatedAsync(accessGrantId, cancellationToken);
        return Results.Accepted($"/api/access-catalog/access-grants/{accessGrantId}");
    }

    private static async Task<Dictionary<Guid, Guid[]>> LoadLocations(AccessCatalogDbContext db, Guid[] accessGrantIds, CancellationToken cancellationToken)
    {
        return await db.AccessGrantLocations.AsNoTracking()
            .Where(item => accessGrantIds.Contains(item.AccessGrantId))
            .GroupBy(item => item.AccessGrantId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.LocationId).ToArray(), cancellationToken);
    }

    private static async Task<Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]>> LoadMaterializationOutcomes(SagasDbContext db, Guid[] accessGrantIds, CancellationToken cancellationToken)
    {
        return await db.AccessGrantMaterializationOutcomes.AsNoTracking()
            .Where(item => accessGrantIds.Contains(item.AccessGrantId))
            .GroupBy(item => item.AccessGrantId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.ToResponse()).ToArray(), cancellationToken);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(AccessCatalogErrors error) =>
        error switch
        {
            AccessCatalogErrors.PackageNotFound => Problem(StatusCodes.Status404NotFound, "Package not found."),
            AccessCatalogErrors.AccessGrantNotFound => Problem(StatusCodes.Status404NotFound, "Access grant not found."),
            AccessCatalogErrors.ReasonRequired => Problem(StatusCodes.Status400BadRequest, "Reason is required."),
            AccessCatalogErrors.InvalidValidityRange => Problem(StatusCodes.Status400BadRequest, "Valid until must be after valid from."),
            AccessCatalogErrors.PackageMustContainAccessItems => Problem(StatusCodes.Status409Conflict, "Package must contain at least one access item."),
            AccessCatalogErrors.LocationRequired => Problem(StatusCodes.Status400BadRequest, "At least one valid location is required."),
            AccessCatalogErrors.AccessGrantAlreadyRevoked => Problem(StatusCodes.Status409Conflict, "Access grant already revoked."),
            AccessCatalogErrors.AccessProvisioningFailed => Problem(StatusCodes.Status409Conflict, "Failed to provision one or more PACS assignments for this access grant."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected access catalog error.")
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });

    private static IResult ToResult(this (int statusCode, ProblemDetails? problemDetails) error) =>
        error.problemDetails is null ? Results.StatusCode(error.statusCode) : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
