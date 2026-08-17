using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Application;

public sealed class GrantRequirementsService(
    RequirementsDbContext db,
    LocationsDbContext locationsDb,
    ContractorsDbContext contractorsDb,
    IdentitiesDbContext identitiesDb,
    TimeProvider timeProvider)
{
    public async Task<Result<IReadOnlyList<DerivedGrantRequirement>, RequirementsEvaluationErrors>> DeriveForGrantAsync(
        Guid identityId,
        RequirementSubjectKind subjectKind,
        Guid locationId,
        IReadOnlyCollection<Guid>? previewContractorJobTypeIds = null,
        CancellationToken cancellationToken = default)
    {
        if (!await locationsDb.LocationLookups.AnyAsync(item => item.Id == locationId, cancellationToken))
            return Result.Failure<IReadOnlyList<DerivedGrantRequirement>, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.LocationNotFound);

        Guid[] locationPath = await ResolveLocationPathAsync(locationId, cancellationToken);
        RequirementDefinition[] definitions = await db.RequirementDefinitions.Where(item => item.IsActive).ToArrayAsync(cancellationToken);
        Guid[] activeDefinitionIds = definitions.Select(item => item.Id).ToArray();

        List<DerivedGrantRequirement> requirements = await db.LocationRequirementPolicies
            .Where(item => locationPath.Contains(item.LocationId) && item.IsEnabled)
            .Where(item => item.SubjectKind == subjectKind || item.SubjectKind == RequirementSubjectKind.Any)
            .Where(item => activeDefinitionIds.Contains(item.RequirementDefinitionId))
            .Select(item => new DerivedGrantRequirement(item.RequirementDefinitionId, item.Id, nameof(LocationRequirementPolicy), item.IsBlocking))
            .ToListAsync(cancellationToken);

        if (subjectKind == RequirementSubjectKind.Contractor)
        {
            Guid[] jobTypeIds = await ResolveContractorJobTypeIdsAsync(identityId, locationPath, previewContractorJobTypeIds, cancellationToken);
            if (jobTypeIds.Length > 0)
            {
                List<DerivedGrantRequirement> contractorRequirements = await db.LocationJobRequirementPolicies
                    .Where(item => locationPath.Contains(item.LocationId) && item.IsEnabled)
                    .Where(item => jobTypeIds.Contains(item.JobTypeId))
                    .Where(item => activeDefinitionIds.Contains(item.RequirementDefinitionId))
                    .Select(item => new DerivedGrantRequirement(item.RequirementDefinitionId, item.Id, nameof(LocationJobRequirementPolicy), item.IsBlocking))
                    .ToListAsync(cancellationToken);
                requirements.AddRange(contractorRequirements);
            }
        }

        return Result.Success<IReadOnlyList<DerivedGrantRequirement>, RequirementsEvaluationErrors>(
            requirements
                .GroupBy(item => item.RequirementDefinitionId)
                .Select(group =>
                {
                    DerivedGrantRequirement first = group.First();
                    return first with { IsBlocking = group.Any(item => item.IsBlocking) };
                })
                .ToArray());
    }

    public async Task<IReadOnlyList<EvaluatedGrantRequirement>> EvaluateGrantRequirementsAsync(
        Guid identityId,
        Guid[] requirementDefinitionIds,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        RequirementDefinition[] definitions = await db.RequirementDefinitions
            .Where(item => requirementDefinitionIds.Contains(item.Id))
            .ToArrayAsync(cancellationToken);

        List<EvaluatedGrantRequirement> results = [];
        foreach (RequirementDefinition definition in definitions)
        {
            RequirementEvaluation evaluation = await EvaluateRequirementAsync(identityId, definition, now, cancellationToken);
            results.Add(new EvaluatedGrantRequirement(
                definition.Id,
                evaluation.Status,
                evaluation.EvidenceKind,
                evaluation.EvidenceReference,
                evaluation.Reason,
                evaluation.ValidUntil,
                now));
        }

        return results;
    }

    private async Task<Guid[]> ResolveLocationPathAsync(Guid locationId, CancellationToken cancellationToken)
    {
        LocationLookup lookup = await locationsDb.LocationLookups.SingleAsync(item => item.Id == locationId, cancellationToken);
        return lookup.Type switch
        {
            LocationType.Site => [lookup.SiteId],
            LocationType.Building when lookup.BuildingId.HasValue => [lookup.SiteId, lookup.BuildingId.Value],
            LocationType.Room when lookup.BuildingId.HasValue && lookup.RoomId.HasValue => [lookup.SiteId, lookup.BuildingId.Value, lookup.RoomId.Value],
            _ => [lookup.SiteId]
        };
    }

    private async Task<Guid[]> ResolveContractorJobTypeIdsAsync(Guid identityId, Guid[] locationPath, IReadOnlyCollection<Guid>? previewContractorJobTypeIds, CancellationToken cancellationToken)
    {
        Guid[] contractorIds = await identitiesDb.ContractorAffiliations
            .Where(item => item.IdentityId == identityId)
            .Select(item => item.ContractorId)
            .ToArrayAsync(cancellationToken);

        Guid[] persistedJobTypeIds = contractorIds.Length == 0
            ? []
            : await contractorsDb.ContractorJobAssignments
                .Where(item => contractorIds.Contains(item.ContractorId))
                .Where(item => item.Status == ContractorJobAssignmentStatus.Planned || item.Status == ContractorJobAssignmentStatus.Active)
                .Where(item => item.AssignedUntil > timeProvider.GetUtcNow())
                .Join(contractorsDb.ContractorJobs.Where(item => item.Status == ContractorJobStatus.Planned || item.Status == ContractorJobStatus.Active),
                    assignment => assignment.ContractorJobId,
                    job => job.Id,
                    (_, job) => job)
                .Where(job => locationPath.Contains(job.LocationId))
                .Select(job => job.JobTypeId)
                .Distinct()
                .ToArrayAsync(cancellationToken);

        Guid[] previewJobTypeIds = previewContractorJobTypeIds?.Distinct().ToArray() ?? [];
        if (persistedJobTypeIds.Length == 0)
            return previewJobTypeIds;
        if (previewJobTypeIds.Length == 0)
            return persistedJobTypeIds;

        return persistedJobTypeIds
            .Concat(previewJobTypeIds)
            .Distinct()
            .ToArray();
    }

    private async Task<RequirementEvaluation> EvaluateRequirementAsync(
        Guid identityId,
        RequirementDefinition definition,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        RequirementEvidence[] evidence = await db.RequirementEvidence
            .Where(item => item.IdentityId == identityId && item.RequirementDefinitionId == definition.Id)
            .OrderByDescending(item => item.VerifiedAt)
            .ToArrayAsync(cancellationToken);

        RequirementEvidence[] validEvidence = evidence
            .Where(item => item.Status == RequirementEvidenceStatus.Valid)
            .Where(item => !item.ValidFrom.HasValue || item.ValidFrom.Value <= now)
            .Where(item => !item.ValidUntil.HasValue || item.ValidUntil.Value > now)
            .ToArray();

        if (validEvidence.Length > 0)
        {
            RequirementEvidence selectedEvidence = validEvidence
                .OrderBy(item => item.ValidUntil ?? DateTimeOffset.MaxValue)
                .First();

            return new RequirementEvaluation(
                RequirementResultStatus.Fulfilled,
                selectedEvidence.EvidenceKind,
                selectedEvidence.SourceReference ?? selectedEvidence.Id.ToString(),
                GetRequirementValidUntil(validEvidence),
                "Requirement fulfilled.");
        }

        if (evidence.Any(item => item.Status == RequirementEvidenceStatus.Valid && item.ValidUntil.HasValue && item.ValidUntil.Value <= now))
            return new RequirementEvaluation(RequirementResultStatus.Expired, null, null, null, "Requirement evidence expired.");

        if (evidence.Any(item => item.Status == RequirementEvidenceStatus.Invalid))
            return new RequirementEvaluation(RequirementResultStatus.Failed, null, null, null, "Requirement evidence is invalid.");

        return new RequirementEvaluation(RequirementResultStatus.Missing, null, null, null, "Requirement evidence is missing.");
    }

    private static DateTimeOffset? GetRequirementValidUntil(IEnumerable<RequirementEvidence> evidence)
    {
        DateTimeOffset? result = null;

        foreach (RequirementEvidence item in evidence)
        {
            if (!item.ValidUntil.HasValue)
                return null;

            if (!result.HasValue || item.ValidUntil.Value < result.Value)
                result = item.ValidUntil.Value;
        }

        return result;
    }

    private sealed record RequirementEvaluation(RequirementResultStatus Status, RequirementEvidenceKind? EvidenceKind, string? EvidenceReference, DateTimeOffset? ValidUntil, string Reason);
}

public sealed record DerivedGrantRequirement(Guid RequirementDefinitionId, Guid SourcePolicyId, string SourcePolicyKind, bool IsBlocking);
public sealed record EvaluatedGrantRequirement(Guid RequirementDefinitionId, RequirementResultStatus Status, RequirementEvidenceKind? EvidenceKind, string? EvidenceReference, string Reason, DateTimeOffset? ValidUntil, DateTimeOffset LastEvaluatedAt);
