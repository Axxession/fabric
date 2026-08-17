using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Contracts;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Fabric.Server.AccessCatalog.Endpoints;

public static class AccessGrantEndpoints
{
    public static IEndpointRouteBuilder MapAccessGrantEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder grants = app.MapGroup("/api/access-catalog/access-grants");

        grants.MapGet("", ListAccessGrants).Produces<Page<AccessGrantResponse>>();
        grants.MapPost("", CreateAccessGrant).Produces<AccessGrantResponse>(StatusCodes.Status201Created);
        grants.MapPost("/recalculate-requirements", RecalculateGrantRequirements).Produces<RecalculateGrantRequirementsResponse>();
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
        Guid[] grantIds = items.Select(item => item.Id).ToArray();
        Dictionary<Guid, GrantRequirementResponse[]> requirements = await LoadRequirements(db, grantIds, cancellationToken);
        Dictionary<Guid, GrantRequirementResultResponse[]> requirementResults = await LoadRequirementResults(db, grantIds, cancellationToken);
        Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, grantIds, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse(requirements.GetValueOrDefault(item.Id, []), requirementResults.GetValueOrDefault(item.Id, []), outcomes.GetValueOrDefault(item.Id, []))));
    }

    private static async Task<IResult> CreateAccessGrant([FromBody] CreateAccessGrantRequest request, AccessGrantService service, AccessCatalogDbContext db, SagasDbContext sagasDb, CancellationToken cancellationToken = default)
    {
        Result<AccessGrant, AccessCatalogErrors> result = await service.CreateAsync(
            request.PackageId,
            request.IdentityId,
            request.LocationId,
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
                Dictionary<Guid, GrantRequirementResponse[]> requirements = await LoadRequirements(db, [item.Id], cancellationToken);
                Dictionary<Guid, GrantRequirementResultResponse[]> requirementResults = await LoadRequirementResults(db, [item.Id], cancellationToken);
                Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, [item.Id], cancellationToken);
                return Results.Created($"/api/access-catalog/access-grants/{item.Id}", item.ToResponse(requirements.GetValueOrDefault(item.Id, []), requirementResults.GetValueOrDefault(item.Id, []), outcomes.GetValueOrDefault(item.Id, [])));
            },
            error => Task.FromResult(MapError(error).ToResult()));
    }

    private static async Task<IResult> GetAccessGrant(Guid accessGrantId, AccessCatalogDbContext db, SagasDbContext sagasDb, CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.AsNoTracking().SingleOrDefaultAsync(item => item.Id == accessGrantId, cancellationToken);
        if (grant is null)
            return Results.NotFound();

        Dictionary<Guid, GrantRequirementResponse[]> requirements = await LoadRequirements(db, [grant.Id], cancellationToken);
        Dictionary<Guid, GrantRequirementResultResponse[]> requirementResults = await LoadRequirementResults(db, [grant.Id], cancellationToken);
        Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, [accessGrantId], cancellationToken);
        return Results.Ok(grant.ToResponse(requirements.GetValueOrDefault(grant.Id, []), requirementResults.GetValueOrDefault(grant.Id, []), outcomes.GetValueOrDefault(accessGrantId, [])));
    }

    private static async Task<IResult> RecalculateGrantRequirements([FromQuery] bool futureOnly, AccessGrantService service, CancellationToken cancellationToken = default)
    {
        int processed = await service.RecalculateRequirementsAsync(futureOnly, cancellationToken);
        return Results.Ok(new RecalculateGrantRequirementsResponse(processed, futureOnly));
    }

    private static async Task<IResult> RevokeAccessGrant(Guid accessGrantId, AccessGrantService service, AccessCatalogDbContext db, SagasDbContext sagasDb, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<AccessGrant, AccessCatalogErrors> result = await service.RevokeAsync(accessGrantId, AccessGrantRevokeCause.Manual, GetRevokedBy(httpContext.User), cancellationToken);

        return await result.Match<Task<IResult>>(
            async item =>
            {
                Dictionary<Guid, GrantRequirementResponse[]> requirements = await LoadRequirements(db, [item.Id], cancellationToken);
                Dictionary<Guid, GrantRequirementResultResponse[]> requirementResults = await LoadRequirementResults(db, [item.Id], cancellationToken);
                Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, [item.Id], cancellationToken);
                return Results.Ok(item.ToResponse(requirements.GetValueOrDefault(item.Id, []), requirementResults.GetValueOrDefault(item.Id, []), outcomes.GetValueOrDefault(item.Id, [])));
            },
            error => Task.FromResult(MapError(error).ToResult()));
    }

    private static async Task<IResult> ReconcileAccessGrant(
        Guid accessGrantId,
        AccessCatalogDbContext db,
        AccessGrantComplianceService complianceService,
        CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.AsNoTracking().SingleOrDefaultAsync(item => item.Id == accessGrantId, cancellationToken);
        if (grant is null)
            return Results.NotFound();

        await complianceService.EvaluateGrantAsync(accessGrantId, cancellationToken);
        return Results.Accepted($"/api/access-catalog/access-grants/{accessGrantId}");
    }

    private static async Task<Dictionary<Guid, GrantRequirementResponse[]>> LoadRequirements(AccessCatalogDbContext db, Guid[] accessGrantIds, CancellationToken cancellationToken)
    {
        return await db.GrantRequirements.AsNoTracking()
            .Where(item => accessGrantIds.Contains(item.AccessGrantId))
            .GroupBy(item => item.AccessGrantId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.ToResponse()).ToArray(), cancellationToken);
    }

    private static async Task<Dictionary<Guid, GrantRequirementResultResponse[]>> LoadRequirementResults(AccessCatalogDbContext db, Guid[] accessGrantIds, CancellationToken cancellationToken)
    {
        return await db.GrantRequirementResults.AsNoTracking()
            .Where(item => accessGrantIds.Contains(item.AccessGrantId))
            .GroupBy(item => item.AccessGrantId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.ToResponse()).ToArray(), cancellationToken);
    }

    private static async Task<Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]>> LoadMaterializationOutcomes(SagasDbContext db, Guid[] accessGrantIds, CancellationToken cancellationToken)
    {
        return await db.AccessGrantMaterializationOutcomes.AsNoTracking()
            .Where(item => accessGrantIds.Contains(item.AccessGrantId))
            .GroupBy(item => item.AccessGrantId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.ToResponse()).ToArray(), cancellationToken);
    }

    private static string? GetRevokedBy(ClaimsPrincipal user)
    {
        string? email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("email")
            ?? user.FindFirstValue("preferred_username");
        string? displayName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue("name");

        return !string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(email)
            ? $"{displayName} ({email})"
            : !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : email;
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(AccessCatalogErrors error) =>
        error switch
        {
            AccessCatalogErrors.PackageNotFound => Problem(StatusCodes.Status404NotFound, "Package not found."),
            AccessCatalogErrors.AccessGrantNotFound => Problem(StatusCodes.Status404NotFound, "Access grant not found."),
            AccessCatalogErrors.ReasonRequired => Problem(StatusCodes.Status400BadRequest, "Reason is required."),
            AccessCatalogErrors.InvalidValidityRange => Problem(StatusCodes.Status400BadRequest, "Valid until must be after valid from."),
            AccessCatalogErrors.PackageMustContainAccessItems => Problem(StatusCodes.Status409Conflict, "Package must contain at least one access item."),
            AccessCatalogErrors.LocationRequired => Problem(StatusCodes.Status400BadRequest, "A valid location is required."),
            AccessCatalogErrors.AccessGrantAlreadyRevoked => Problem(StatusCodes.Status409Conflict, "Access grant already revoked."),
            AccessCatalogErrors.AccessGrantAlreadyReplaced => Problem(StatusCodes.Status409Conflict, "Access grant already replaced."),
            AccessCatalogErrors.AccessGrantNotActive => Problem(StatusCodes.Status409Conflict, "Access grant is not active."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected access catalog error.")
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });

    private static IResult ToResult(this (int statusCode, ProblemDetails? problemDetails) error) =>
        error.problemDetails is null ? Results.StatusCode(error.statusCode) : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
