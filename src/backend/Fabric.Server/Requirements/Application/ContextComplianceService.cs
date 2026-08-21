using Fabric.Server.Core;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.ContractorJobs;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Application;

public sealed class ContextComplianceService(
    RequirementsDbContext requirementsDb,
    LocationsDbContext locationsDb,
    SagasDbContext sagasDb,
    GrantRequirementsService grantRequirementsService)
{
    public async Task<Result<ContextComplianceResponse, RequirementsEvaluationErrors>> EvaluateAsync(
        Guid? identityId,
        RequirementSubjectKind subjectKind,
        Guid? locationId,
        DateTimeOffset? validUntil,
        IReadOnlyCollection<Guid>? contractorJobTypeIds = null,
        string? unavailableReason = null,
        CancellationToken cancellationToken = default)
    {
        if (!locationId.HasValue)
            return Result.Success<ContextComplianceResponse, RequirementsEvaluationErrors>(new ContextComplianceResponse(ContextComplianceStatus.Compliant, null, unavailableReason, []));

        Result<IReadOnlyList<DerivedGrantRequirement>, RequirementsEvaluationErrors> derivation = await grantRequirementsService.DeriveForGrantAsync(
            identityId ?? Guid.Empty,
            subjectKind,
            locationId.Value,
            contractorJobTypeIds,
            cancellationToken);
        if (derivation.IsFailure(out RequirementsEvaluationErrors error))
            return Result.Failure<ContextComplianceResponse, RequirementsEvaluationErrors>(error);

        derivation.IsSuccess(out IReadOnlyList<DerivedGrantRequirement> derivedRequirements);
        if (derivedRequirements.Count == 0)
            return Result.Success<ContextComplianceResponse, RequirementsEvaluationErrors>(new ContextComplianceResponse(ContextComplianceStatus.Compliant, null, unavailableReason, []));

        Guid[] requirementDefinitionIds = derivedRequirements.Select(item => item.RequirementDefinitionId).Distinct().ToArray();
        Dictionary<Guid, RequirementDefinition> definitionsById = await requirementsDb.RequirementDefinitions
            .AsNoTracking()
            .Where(item => requirementDefinitionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        Dictionary<Guid, EvaluatedGrantRequirement> evaluationsById = identityId.HasValue
            ? (await grantRequirementsService.EvaluateGrantRequirementsAsync(identityId.Value, requirementDefinitionIds, cancellationToken)).ToDictionary(item => item.RequirementDefinitionId)
            : [];

        RequirementComplianceResponse[] requirements = derivedRequirements
            .Where(item => definitionsById.ContainsKey(item.RequirementDefinitionId))
            .Select(item => BuildRequirementResponse(item, definitionsById[item.RequirementDefinitionId], evaluationsById.GetValueOrDefault(item.RequirementDefinitionId), identityId.HasValue))
            .OrderBy(item => item.Name)
            .ToArray();

        (ContextComplianceStatus status, DateTimeOffset? compliantUntil) = Aggregate(requirements, validUntil);
        return Result.Success<ContextComplianceResponse, RequirementsEvaluationErrors>(new ContextComplianceResponse(status, compliantUntil, unavailableReason, requirements));
    }

    public async Task<Guid[]> ResolveContractorPackageIdsAsync(Guid jobTypeId, Guid locationId, CancellationToken cancellationToken = default)
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

    private static RequirementComplianceResponse BuildRequirementResponse(DerivedGrantRequirement requirement, RequirementDefinition definition, EvaluatedGrantRequirement? evaluation, bool hasIdentity)
    {
        RequirementResultStatus status = evaluation?.Status ?? RequirementResultStatus.Missing;
        string reason = hasIdentity
            ? evaluation?.Reason ?? "Requirement evidence is missing."
            : "No linked identity is available for compliance evaluation.";

        return new RequirementComplianceResponse(
            requirement.RequirementDefinitionId,
            definition.Code,
            definition.Name,
            requirement.IsBlocking,
            status,
            reason,
            evaluation?.ValidUntil,
            definition.AllowedEvidenceKinds);
    }

    public static (ContextComplianceStatus status, DateTimeOffset? compliantUntil) Aggregate(IReadOnlyList<RequirementComplianceResponse> requirements, DateTimeOffset? validUntil)
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
            ? (ContextComplianceStatus.NonCompliant, null)
            : temporary
                ? (ContextComplianceStatus.TemporarilyCompliant, compliantUntil)
                : (ContextComplianceStatus.Compliant, null);
    }

    private static bool IsInLocationScope(LocationLookup target, LocationLookup scope) =>
        scope.Type switch
        {
            LocationType.Site => target.SiteId == scope.SiteId,
            LocationType.Building when scope.BuildingId.HasValue => target.BuildingId == scope.BuildingId,
            LocationType.Room when scope.RoomId.HasValue => target.RoomId == scope.RoomId,
            _ => false,
        };
}
