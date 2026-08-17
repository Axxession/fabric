using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Contracts;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Persistence;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.ContractorJobs;
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
        grants.MapPost("", CreateAccessGrant).Produces<CreateAccessGrantResponse>(StatusCodes.Status201Created);
        grants.MapPost("/recalculate-requirements", RecalculateGrantRequirements).Produces<RecalculateGrantRequirementsResponse>();
        grants.MapPost("/contractor-assignment-preview", PreviewContractorAssignmentCompliance).Produces<ContractorAssignmentCompliancePreviewResponse>();
        grants.MapPost("/compliance-summaries/by-source", ListAssignmentComplianceSummariesBySource).Produces<AssignmentComplianceSummaryResponse[]>();
        grants.MapPost("/compliance-details/by-source", ListAssignmentComplianceDetailsBySource).Produces<AssignmentComplianceDetailResponse[]>();
        grants.MapGet("/{accessGrantId:guid}", GetAccessGrant).Produces<AccessGrantResponse>().Produces(StatusCodes.Status404NotFound);
        grants.MapPost("/{accessGrantId:guid}/reconcile", ReconcileAccessGrant).Produces(StatusCodes.Status202Accepted).Produces(StatusCodes.Status404NotFound);
        grants.MapPost("/{accessGrantId:guid}/revoke", RevokeAccessGrant).Produces<AccessGrantResponse>().Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAccessGrants([AsParameters] ListAccessGrantsRequest request, AccessCatalogDbContext db, AccessControlDbContext accessControlDb, SagasDbContext sagasDb, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        IQueryable<AccessGrant> query = db.AccessGrants.AsNoTracking();
        if (request.IdentityId.HasValue)
            query = query.Where(item => item.IdentityId == request.IdentityId.Value);
        if (request.PackageId.HasValue)
            query = query.Where(item => item.PackageId == request.PackageId.Value);
        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);
        if (request.SourceKind.HasValue)
            query = query.Where(item => item.SourceKind == request.SourceKind.Value);
        if (request.SourceId.HasValue)
            query = query.Where(item => item.SourceId == request.SourceId.Value);

        IPaged<AccessGrant> result = await query.OrderBy(item => item.ValidFrom).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        AccessGrant[] items = result.Items.ToArray();
        Guid[] grantIds = items.Select(item => item.Id).ToArray();
        Dictionary<Guid, GrantRequirementResponse[]> requirements = await LoadRequirements(db, grantIds, cancellationToken);
        Dictionary<Guid, GrantRequirementResultResponse[]> requirementResults = await LoadRequirementResults(db, grantIds, cancellationToken);
        Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, grantIds, cancellationToken);
        Dictionary<Guid, bool> complianceRequiredByAccessItemId = await LoadComplianceRequiredByAccessItemIdAsync(accessControlDb, items, cancellationToken);
        Dictionary<Guid, AccessGrantProvisioningSagaState> sagaStates = await LoadSagaStatesAsync(sagasDb, grantIds, cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();
        return Results.Ok(result.Map(item => item.ToResponse(
            GrantProvisioningStatusResolver.Resolve(
                item,
                complianceRequiredByAccessItemId.GetValueOrDefault(item.AccessItemId ?? Guid.Empty, true),
                sagaStates.GetValueOrDefault(item.Id),
                outcomes.GetValueOrDefault(item.Id, []).Select(response => response.ToDomain(item.Id)).ToArray(),
                now),
            requirements.GetValueOrDefault(item.Id, []),
            requirementResults.GetValueOrDefault(item.Id, []),
            outcomes.GetValueOrDefault(item.Id, []))));
    }

    private static async Task<IResult> CreateAccessGrant([FromBody] CreateAccessGrantRequest request, AccessGrantService service, AccessCatalogDbContext db, AccessControlDbContext accessControlDb, SagasDbContext sagasDb, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<AccessGrant>, AccessCatalogErrors> result = await service.CreateAsync(
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
            async grants =>
            {
                AccessGrantResponse[] responses = await BuildGrantResponsesAsync(grants, db, accessControlDb, sagasDb, timeProvider, cancellationToken);
                return Results.Created($"/api/access-catalog/access-grants", new CreateAccessGrantResponse(responses));
            },
            error => Task.FromResult(MapError(error).ToResult()));
    }

    private static async Task<IResult> GetAccessGrant(Guid accessGrantId, AccessCatalogDbContext db, AccessControlDbContext accessControlDb, SagasDbContext sagasDb, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.AsNoTracking().SingleOrDefaultAsync(item => item.Id == accessGrantId, cancellationToken);
        if (grant is null)
            return Results.NotFound();

        AccessGrantResponse[] responses = await BuildGrantResponsesAsync([grant], db, accessControlDb, sagasDb, timeProvider, cancellationToken);
        return Results.Ok(responses.Single());
    }

    private static async Task<IResult> RecalculateGrantRequirements([FromQuery] bool futureOnly, AccessGrantService service, CancellationToken cancellationToken = default)
    {
        int processed = await service.RecalculateRequirementsAsync(futureOnly, cancellationToken);
        return Results.Ok(new RecalculateGrantRequirementsResponse(processed, futureOnly));
    }

    private static async Task<IResult> PreviewContractorAssignmentCompliance(
        [FromBody] ContractorAssignmentCompliancePreviewRequest request,
        ContractorsDbContext contractorsDb,
        IdentitiesDbContext identitiesDb,
        RequirementsDbContext requirementsDb,
        AccessCatalogDbContext accessCatalogDb,
        SagasDbContext sagasDb,
        LocationsDbContext locationsDb,
        GrantRequirementsService grantRequirementsService,
        CancellationToken cancellationToken = default)
    {
        ContractorJob? job = await contractorsDb.ContractorJobs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.ContractorJobId, cancellationToken);
        if (job is null)
            return Results.NotFound();

        Contractor? contractor = await contractorsDb.Contractors.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.ContractorId, cancellationToken);
        if (contractor is null)
            return Results.NotFound();

        if (contractor.CompanyId != job.CompanyId)
            return Results.Problem("Contractor does not belong to the same company as the job.", statusCode: StatusCodes.Status400BadRequest);

        if (request.AssignedUntil <= request.AssignedFrom)
            return Results.Problem("Assigned until must be after assigned from.", statusCode: StatusCodes.Status400BadRequest);

        if (request.AssignedUntil > job.PlannedEnd)
            return Results.Problem("Assignment must fit inside the job window.", statusCode: StatusCodes.Status400BadRequest);

        Guid? identityId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => item.ContractorId == request.ContractorId)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);

        Guid[] packageIds = await ResolveContractorRulePackageIdsAsync(sagasDb, locationsDb, job.JobTypeId, job.LocationId, cancellationToken);
        Dictionary<Guid, string> packageNamesById = packageIds.Length == 0
            ? []
            : await accessCatalogDb.Packages.AsNoTracking()
                .Where(item => packageIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        if (!identityId.HasValue)
        {
            return Results.Ok(new ContractorAssignmentCompliancePreviewResponse(
                request.ContractorId,
                request.ContractorJobId,
                job.LocationId,
                job.JobTypeId,
                "No compliance preview available because this contractor has no linked identity.",
                []));
        }

        Result<IReadOnlyList<DerivedGrantRequirement>, RequirementsEvaluationErrors> derivation = await grantRequirementsService.DeriveForGrantAsync(
            identityId.Value,
            RequirementSubjectKind.Contractor,
            job.LocationId,
            [job.JobTypeId],
            cancellationToken);

        if (derivation.IsFailure(out RequirementsEvaluationErrors derivationError))
            return Results.Problem($"Could not build contractor assignment compliance preview: {derivationError}.", statusCode: StatusCodes.Status400BadRequest);

        derivation.IsSuccess(out IReadOnlyList<DerivedGrantRequirement> derivedRequirements);
        Guid[] requirementDefinitionIds = derivedRequirements.Select(item => item.RequirementDefinitionId).Distinct().ToArray();
        IReadOnlyList<EvaluatedGrantRequirement> evaluations = requirementDefinitionIds.Length == 0
            ? []
            : await grantRequirementsService.EvaluateGrantRequirementsAsync(identityId.Value, requirementDefinitionIds, cancellationToken);
        Dictionary<Guid, RequirementDefinition> definitionsById = requirementDefinitionIds.Length == 0
            ? []
            : await requirementsDb.RequirementDefinitions.AsNoTracking()
                .Where(item => requirementDefinitionIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);

        AssignmentRequirementComplianceResponse[] requirements = derivedRequirements
            .Join(evaluations, requirement => requirement.RequirementDefinitionId, evaluation => evaluation.RequirementDefinitionId, (requirement, evaluation) => new { requirement, evaluation })
            .Where(item => definitionsById.ContainsKey(item.requirement.RequirementDefinitionId))
            .Select(item => new AssignmentRequirementComplianceResponse(
                item.requirement.RequirementDefinitionId,
                definitionsById[item.requirement.RequirementDefinitionId].Code,
                definitionsById[item.requirement.RequirementDefinitionId].Name,
                item.requirement.IsBlocking,
                item.evaluation.Status,
                item.evaluation.Reason,
                item.evaluation.ValidUntil))
            .OrderBy(item => item.Name)
            .ToArray();

        (GrantComplianceStatus status, DateTimeOffset? compliantUntil) = AggregateCompliance(requirements, request.AssignedUntil);
        ContractorAssignmentCompliancePreviewPackageResponse[] packages = packageIds
            .Select(packageId => new ContractorAssignmentCompliancePreviewPackageResponse(
                packageId,
                packageNamesById.GetValueOrDefault(packageId, packageId.ToString()),
                status,
                compliantUntil,
                requirements))
            .ToArray();

        return Results.Ok(new ContractorAssignmentCompliancePreviewResponse(
            request.ContractorId,
            request.ContractorJobId,
            job.LocationId,
            job.JobTypeId,
            null,
            packages));
    }

    private static async Task<IResult> ListAssignmentComplianceSummariesBySource(
        [FromBody] AssignmentContextRequest[] request,
        AccessCatalogDbContext db,
        CancellationToken cancellationToken = default)
    {
        AssignmentContextRequest[] contexts = request
            .DistinctBy(item => (item.SourceKind, item.SourceId))
            .ToArray();
        if (contexts.Length == 0)
            return Results.Ok(Array.Empty<AssignmentComplianceSummaryResponse>());

        AccessGrant[] grants = await LoadGrantsForContextsAsync(db, contexts, cancellationToken);
        AssignmentComplianceSummaryResponse[] response = contexts
            .Select(context => BuildComplianceSummary(context, grants.Where(grant => grant.SourceKind == context.SourceKind && grant.SourceId == context.SourceId).ToArray()))
            .ToArray();
        return Results.Ok(response);
    }

    private static async Task<IResult> ListAssignmentComplianceDetailsBySource(
        [FromBody] AssignmentContextRequest[] request,
        AccessCatalogDbContext db,
        RequirementsDbContext requirementsDb,
        CancellationToken cancellationToken = default)
    {
        AssignmentContextRequest[] contexts = request
            .DistinctBy(item => (item.SourceKind, item.SourceId))
            .ToArray();
        if (contexts.Length == 0)
            return Results.Ok(Array.Empty<AssignmentComplianceDetailResponse>());

        AccessGrant[] grants = await LoadGrantsForContextsAsync(db, contexts, cancellationToken);
        Guid[] grantIds = grants.Select(item => item.Id).Distinct().ToArray();
        GrantRequirement[] requirements = grantIds.Length == 0
            ? []
            : await db.GrantRequirements.AsNoTracking().Where(item => grantIds.Contains(item.AccessGrantId)).ToArrayAsync(cancellationToken);
        GrantRequirementResult[] requirementResults = grantIds.Length == 0
            ? []
            : await db.GrantRequirementResults.AsNoTracking().Where(item => grantIds.Contains(item.AccessGrantId)).ToArrayAsync(cancellationToken);
        Guid[] requirementDefinitionIds = requirements.Select(item => item.RequirementDefinitionId).Distinct().ToArray();
        Dictionary<Guid, RequirementDefinition> definitionsById = requirementDefinitionIds.Length == 0
            ? []
            : await requirementsDb.RequirementDefinitions.AsNoTracking().Where(item => requirementDefinitionIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);

        AssignmentComplianceDetailResponse[] response = contexts
            .Select(context => BuildComplianceDetail(
                context,
                grants.Where(grant => grant.SourceKind == context.SourceKind && grant.SourceId == context.SourceId).ToArray(),
                requirements,
                requirementResults,
                definitionsById))
            .ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> RevokeAccessGrant(Guid accessGrantId, AccessGrantService service, AccessCatalogDbContext db, AccessControlDbContext accessControlDb, SagasDbContext sagasDb, TimeProvider timeProvider, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<AccessGrant, AccessCatalogErrors> result = await service.RevokeAsync(accessGrantId, AccessGrantRevokeCause.Manual, GetRevokedBy(httpContext.User), cancellationToken);

        return await result.Match<Task<IResult>>(
            async item =>
            {
                AccessGrantResponse[] responses = await BuildGrantResponsesAsync([item], db, accessControlDb, sagasDb, timeProvider, cancellationToken);
                return Results.Ok(responses.Single());
            },
            error => Task.FromResult(MapError(error).ToResult()));
    }

    private static async Task<AccessGrantResponse[]> BuildGrantResponsesAsync(
        IReadOnlyList<AccessGrant> grants,
        AccessCatalogDbContext db,
        AccessControlDbContext accessControlDb,
        SagasDbContext sagasDb,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        Guid[] grantIds = grants.Select(item => item.Id).ToArray();
        Dictionary<Guid, GrantRequirementResponse[]> requirements = await LoadRequirements(db, grantIds, cancellationToken);
        Dictionary<Guid, GrantRequirementResultResponse[]> requirementResults = await LoadRequirementResults(db, grantIds, cancellationToken);
        Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]> outcomes = await LoadMaterializationOutcomes(sagasDb, grantIds, cancellationToken);
        Dictionary<Guid, bool> complianceRequiredByAccessItemId = await LoadComplianceRequiredByAccessItemIdAsync(accessControlDb, grants, cancellationToken);
        Dictionary<Guid, AccessGrantProvisioningSagaState> sagaStates = await LoadSagaStatesAsync(sagasDb, grantIds, cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();

        return grants
            .Select(grant => grant.ToResponse(
                GrantProvisioningStatusResolver.Resolve(
                    grant,
                    complianceRequiredByAccessItemId.GetValueOrDefault(grant.AccessItemId ?? Guid.Empty, true),
                    sagaStates.GetValueOrDefault(grant.Id),
                    outcomes.GetValueOrDefault(grant.Id, []).Select(response => response.ToDomain(grant.Id)).ToArray(),
                    now),
                requirements.GetValueOrDefault(grant.Id, []),
                requirementResults.GetValueOrDefault(grant.Id, []),
                outcomes.GetValueOrDefault(grant.Id, [])))
            .ToArray();
    }

    private static async Task<Dictionary<Guid, bool>> LoadComplianceRequiredByAccessItemIdAsync(AccessControlDbContext accessControlDb, IReadOnlyList<AccessGrant> grants, CancellationToken cancellationToken)
    {
        Guid[] accessItemIds = grants.Where(item => item.AccessItemId.HasValue).Select(item => item.AccessItemId!.Value).Distinct().ToArray();
        if (accessItemIds.Length == 0)
            return [];

        return await accessControlDb.AccessItems.AsNoTracking()
            .Where(item => accessItemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.IsComplianceRequired, cancellationToken);
    }

    private static async Task<Dictionary<Guid, AccessGrantProvisioningSagaState>> LoadSagaStatesAsync(SagasDbContext db, Guid[] accessGrantIds, CancellationToken cancellationToken)
    {
        return await db.AccessGrantProvisioningSagas.AsNoTracking()
            .Where(item => accessGrantIds.Contains(item.AccessGrantId))
            .ToDictionaryAsync(item => item.AccessGrantId, item => item.State, cancellationToken);
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

    private static async Task<AccessGrant[]> LoadGrantsForContextsAsync(
        AccessCatalogDbContext db,
        AssignmentContextRequest[] contexts,
        CancellationToken cancellationToken)
    {
        Guid[] sourceIds = contexts.Select(item => item.SourceId).Distinct().ToArray();
        AssignmentSourceKind[] sourceKinds = contexts.Select(item => item.SourceKind).Distinct().ToArray();

        return await db.AccessGrants.AsNoTracking()
            .Where(item => sourceIds.Contains(item.SourceId) && sourceKinds.Contains(item.SourceKind))
            .ToArrayAsync(cancellationToken);
    }

    private static AssignmentComplianceSummaryResponse BuildComplianceSummary(
        AssignmentContextRequest context,
        AccessGrant[] grants)
    {
        if (grants.Length == 0)
            return new(context.SourceKind, context.SourceId, null, null, 0);

        (GrantComplianceStatus status, DateTimeOffset? compliantUntil) = AggregateCompliance(grants);
        return new(context.SourceKind, context.SourceId, status, compliantUntil, grants.Length);
    }

    private static AssignmentComplianceDetailResponse BuildComplianceDetail(
        AssignmentContextRequest context,
        AccessGrant[] grants,
        GrantRequirement[] requirements,
        GrantRequirementResult[] requirementResults,
        Dictionary<Guid, RequirementDefinition> definitionsById)
    {
        if (grants.Length == 0)
            return new(context.SourceKind, context.SourceId, null, null, []);

        HashSet<Guid> grantIds = grants.Select(item => item.Id).ToHashSet();
        (GrantComplianceStatus status, DateTimeOffset? compliantUntil) = AggregateCompliance(grants);
        AssignmentRequirementComplianceResponse[] requirementDetails = requirements
            .Where(item => grantIds.Contains(item.AccessGrantId))
            .GroupBy(item => item.RequirementDefinitionId)
            .Select(group => BuildRequirementDetail(group.Key, group.ToArray(), requirementResults, definitionsById))
            .OrderBy(item => item.Name)
            .ToArray();

        return new(context.SourceKind, context.SourceId, status, compliantUntil, requirementDetails);
    }

    private static AssignmentRequirementComplianceResponse BuildRequirementDetail(
        Guid requirementDefinitionId,
        GrantRequirement[] requirements,
        GrantRequirementResult[] requirementResults,
        Dictionary<Guid, RequirementDefinition> definitionsById)
    {
        RequirementDefinition? definition = definitionsById.GetValueOrDefault(requirementDefinitionId);
        GrantRequirementResult[] matchingResults = requirementResults
            .Where(item => requirements.Select(requirement => requirement.AccessGrantId).Contains(item.AccessGrantId) && item.RequirementDefinitionId == requirementDefinitionId)
            .ToArray();

        RequirementResultStatus status = AggregateRequirementStatus(matchingResults);
        GrantRequirementResult? selectedResult = SelectRequirementResultForDisplay(matchingResults);
        string reason = selectedResult?.Reason ?? "Requirement has not been evaluated.";
        DateTimeOffset? validUntil = matchingResults
            .Where(item => item.Status == RequirementResultStatus.Fulfilled && item.ValidUntil.HasValue)
            .Select(item => item.ValidUntil)
            .OrderBy(item => item)
            .FirstOrDefault();

        return new(
            requirementDefinitionId,
            definition?.Code ?? requirementDefinitionId.ToString(),
            definition?.Name ?? requirementDefinitionId.ToString(),
            requirements.Any(item => item.IsBlocking),
            status,
            reason,
            validUntil);
    }

    private static (GrantComplianceStatus status, DateTimeOffset? compliantUntil) AggregateCompliance(
        IReadOnlyList<AssignmentRequirementComplianceResponse> requirements,
        DateTimeOffset? validUntil)
    {
        bool anyBlockingFailure = requirements.Any(item => item.IsBlocking && item.Status != RequirementResultStatus.Fulfilled);
        DateTimeOffset? compliantUntil = requirements
            .Where(item => item.Status == RequirementResultStatus.Fulfilled)
            .Select(item => item.ValidUntil)
            .Where(item => item.HasValue)
            .OrderBy(item => item)
            .FirstOrDefault();
        bool temporary = compliantUntil.HasValue && (!validUntil.HasValue || compliantUntil.Value < validUntil.Value);
        return anyBlockingFailure
            ? (GrantComplianceStatus.NonCompliant, null)
            : temporary
                ? (GrantComplianceStatus.TemporarilyCompliant, compliantUntil)
                : (GrantComplianceStatus.Compliant, null);
    }

    private static GrantRequirementResult? SelectRequirementResultForDisplay(GrantRequirementResult[] results)
    {
        if (results.Length == 0)
            return null;

        return results
            .OrderBy(item => GetRequirementStatusSortOrder(item.Status))
            .ThenBy(item => item.ValidUntil ?? DateTimeOffset.MaxValue)
            .First();
    }

    private static RequirementResultStatus AggregateRequirementStatus(GrantRequirementResult[] results)
    {
        if (results.Length == 0)
            return RequirementResultStatus.Missing;
        if (results.Any(item => item.Status == RequirementResultStatus.Missing))
            return RequirementResultStatus.Missing;
        if (results.Any(item => item.Status == RequirementResultStatus.Failed))
            return RequirementResultStatus.Failed;
        if (results.Any(item => item.Status == RequirementResultStatus.Expired))
            return RequirementResultStatus.Expired;
        return RequirementResultStatus.Fulfilled;
    }

    private static int GetRequirementStatusSortOrder(RequirementResultStatus status) =>
        status switch
        {
            RequirementResultStatus.Missing => 0,
            RequirementResultStatus.Failed => 1,
            RequirementResultStatus.Expired => 2,
            _ => 3
        };

    private static (GrantComplianceStatus status, DateTimeOffset? compliantUntil) AggregateCompliance(IReadOnlyList<AccessGrant> grants)
    {
        if (grants.Any(item => item.ComplianceStatus == GrantComplianceStatus.NonCompliant))
            return (GrantComplianceStatus.NonCompliant, null);

        DateTimeOffset? compliantUntil = grants
            .Where(item => item.ComplianceStatus == GrantComplianceStatus.TemporarilyCompliant && item.CompliantUntil.HasValue)
            .Select(item => item.CompliantUntil)
            .OrderBy(item => item)
            .FirstOrDefault();
        if (grants.Any(item => item.ComplianceStatus == GrantComplianceStatus.TemporarilyCompliant))
            return (GrantComplianceStatus.TemporarilyCompliant, compliantUntil);

        return (GrantComplianceStatus.Compliant, null);
    }

    private static async Task<Guid[]> ResolveContractorRulePackageIdsAsync(
        SagasDbContext sagasDb,
        LocationsDbContext locationsDb,
        Guid jobTypeId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        ContractorJobPackageRule[] rules = await sagasDb.ContractorJobPackageRules
            .AsNoTracking()
            .Where(item => item.JobTypeId == jobTypeId && item.IsEnabled)
            .ToArrayAsync(cancellationToken);
        if (rules.Length == 0)
            return [];

        LocationLookup jobLocation = await locationsDb.LocationLookups.AsNoTracking().SingleAsync(item => item.Id == locationId, cancellationToken);
        Guid[] scopedLocationIds = rules.Where(item => item.LocationId.HasValue).Select(item => item.LocationId!.Value).Distinct().ToArray();
        Dictionary<Guid, LocationLookup> scopedLocations = scopedLocationIds.Length == 0
            ? []
            : await locationsDb.LocationLookups.AsNoTracking().Where(item => scopedLocationIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);

        return rules
            .Where(rule => !rule.LocationId.HasValue || (scopedLocations.TryGetValue(rule.LocationId.Value, out LocationLookup? scopedLocation) && IsInLocationScope(jobLocation, scopedLocation)))
            .Select(rule => rule.PackageId)
            .Distinct()
            .ToArray();
    }

    private static bool IsInLocationScope(LocationLookup target, LocationLookup scope) =>
        scope.Type switch
        {
            LocationType.Site => target.SiteId == scope.SiteId,
            LocationType.Building when scope.BuildingId.HasValue => target.BuildingId == scope.BuildingId,
            LocationType.Room when scope.RoomId.HasValue => target.RoomId == scope.RoomId,
            _ => false
        };

    private static async Task<Dictionary<Guid, AccessGrantMaterializationOutcomeResponse[]>> LoadMaterializationOutcomes(SagasDbContext db, Guid[] accessGrantIds, CancellationToken cancellationToken)
    {
        return await db.AccessGrantMaterializationOutcomes.AsNoTracking()
            .Where(item => accessGrantIds.Contains(item.AccessGrantId))
            .GroupBy(item => item.AccessGrantId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.ToResponse()).ToArray(), cancellationToken);
    }

    private static AccessGrantMaterializationOutcome ToDomain(this AccessGrantMaterializationOutcomeResponse outcome, Guid accessGrantId) =>
        new()
        {
            Id = outcome.Id,
            AccessGrantId = accessGrantId,
            AccessItemId = outcome.AccessItemId,
            LocationId = outcome.LocationId,
            Status = outcome.Status,
            FailureReason = outcome.FailureReason
        };

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
