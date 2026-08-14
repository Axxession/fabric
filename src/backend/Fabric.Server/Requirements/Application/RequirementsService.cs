using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Application;

public sealed class RequirementsService(
    RequirementsDbContext db,
    LocationsDbContext locationsDb,
    ContractorsDbContext contractorsDb,
    IdentitiesDbContext identitiesDb,
    AccessControlDbContext accessControlDb,
    PACSAssignmentService pacsAssignmentService,
    RequirementsLocationResolver locationResolver,
    TimeProvider timeProvider)
{
    public async Task<Result<EnforcementZone, EnforcementZoneErrors>> CreateEnforcementZoneAsync(CreateEnforcementZoneRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.EnforcementZones.AnyAsync(item => item.Code == request.Code, cancellationToken))
            return Result.Failure<EnforcementZone, EnforcementZoneErrors>(EnforcementZoneErrors.CodeRequired);

        Result<EnforcementZone, EnforcementZoneErrors> create = EnforcementZone.Create(request.Code, request.Name, request.Description, timeProvider.GetUtcNow());
        if (create.IsFailure(out EnforcementZoneErrors error))
            return Result.Failure<EnforcementZone, EnforcementZoneErrors>(error);

        create.IsSuccess(out EnforcementZone zone);
        db.EnforcementZones.Add(zone);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<EnforcementZone, EnforcementZoneErrors>(zone);
    }

    public async Task<Result<EnforcementZone, EnforcementZoneErrors>> UpdateEnforcementZoneAsync(Guid id, UpdateEnforcementZoneRequest request, CancellationToken cancellationToken = default)
    {
        EnforcementZone? zone = await db.EnforcementZones.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (zone is null)
            return Result.Failure<EnforcementZone, EnforcementZoneErrors>(EnforcementZoneErrors.EnforcementZoneNotFound);

        Result<EnforcementZoneErrors> update = zone.Update(request.Code, request.Name, request.Description, timeProvider.GetUtcNow());
        if (update.IsFailure(out EnforcementZoneErrors error))
            return Result.Failure<EnforcementZone, EnforcementZoneErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await RecalculateZoneCompliancesAsync(zone.Id, cancellationToken);
        return Result.Success<EnforcementZone, EnforcementZoneErrors>(zone);
    }

    public async Task<Result<RequirementDefinition, RequirementDefinitionErrors>> CreateRequirementDefinitionAsync(CreateRequirementDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.RequirementDefinitions.AnyAsync(item => item.Code == request.Code, cancellationToken))
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(RequirementDefinitionErrors.CodeRequired);

        Result<RequirementDefinition, RequirementDefinitionErrors> create = RequirementDefinition.Create(request.Code, request.Name, request.Description, request.EvaluatorKind, request.IsSensitive, timeProvider.GetUtcNow());
        if (create.IsFailure(out RequirementDefinitionErrors error))
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(error);

        create.IsSuccess(out RequirementDefinition definition);
        db.RequirementDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<RequirementDefinition, RequirementDefinitionErrors>(definition);
    }

    public async Task<Result<RequirementDefinition, RequirementDefinitionErrors>> UpdateRequirementDefinitionAsync(Guid id, UpdateRequirementDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        RequirementDefinition? definition = await db.RequirementDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (definition is null)
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(RequirementDefinitionErrors.RequirementDefinitionNotFound);

        Result<RequirementDefinitionErrors> update = definition.Update(request.Code, request.Name, request.Description, request.EvaluatorKind, request.IsSensitive, timeProvider.GetUtcNow());
        if (update.IsFailure(out RequirementDefinitionErrors error))
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await RecalculateByRequirementDefinitionAsync(definition.Id, cancellationToken);
        return Result.Success<RequirementDefinition, RequirementDefinitionErrors>(definition);
    }

    public async Task<Result<RequirementEvidence, RequirementEvidenceErrors>> CreateRequirementEvidenceAsync(CreateRequirementEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.RequirementDefinitions.AnyAsync(item => item.Id == request.RequirementDefinitionId, cancellationToken))
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(RequirementEvidenceErrors.SummaryRequired);

        Result<RequirementEvidence, RequirementEvidenceErrors> create = RequirementEvidence.Create(
            request.IdentityId,
            request.RequirementDefinitionId,
            request.EvidenceKind,
            request.Status,
            request.ValidFrom,
            request.ValidUntil,
            request.SourceReference,
            request.Summary,
            request.IsSensitive,
            request.VerifiedAt,
            request.FileName,
            request.Content,
            timeProvider.GetUtcNow());

        if (create.IsFailure(out RequirementEvidenceErrors error))
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(error);

        create.IsSuccess(out RequirementEvidence evidence);
        db.RequirementEvidence.Add(evidence);
        await db.SaveChangesAsync(cancellationToken);
        await RecalculateIdentityCompliancesAsync(evidence.IdentityId, cancellationToken);
        return Result.Success<RequirementEvidence, RequirementEvidenceErrors>(evidence);
    }

    public async Task<Result<RequirementEvidence, RequirementEvidenceErrors>> UpdateRequirementEvidenceAsync(Guid id, UpdateRequirementEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        RequirementEvidence? evidence = await db.RequirementEvidence.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (evidence is null)
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(RequirementEvidenceErrors.RequirementEvidenceNotFound);

        Result<RequirementEvidenceErrors> update = evidence.Update(
            request.Status,
            request.ValidFrom,
            request.ValidUntil,
            request.SourceReference,
            request.Summary,
            request.IsSensitive,
            request.VerifiedAt,
            request.FileName,
            request.Content,
            timeProvider.GetUtcNow());

        if (update.IsFailure(out RequirementEvidenceErrors error))
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await RecalculateIdentityCompliancesAsync(evidence.IdentityId, cancellationToken);
        return Result.Success<RequirementEvidence, RequirementEvidenceErrors>(evidence);
    }

    public async Task<Result<EnforcementZoneLocation, RequirementsEvaluationErrors>> AddZoneLocationAsync(CreateEnforcementZoneLocationRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.EnforcementZones.AnyAsync(item => item.Id == request.EnforcementZoneId, cancellationToken))
            return Result.Failure<EnforcementZoneLocation, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.EnforcementZoneNotFound);

        if (!await locationsDb.LocationLookups.AnyAsync(item => item.Id == request.LocationId, cancellationToken))
            return Result.Failure<EnforcementZoneLocation, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.LocationNotFound);

        bool existingLocation = await db.EnforcementZoneLocations.AnyAsync(item => item.LocationId == request.LocationId, cancellationToken);
        if (existingLocation)
            return Result.Failure<EnforcementZoneLocation, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.LocationNotFound);

        EnforcementZoneLocation link = EnforcementZoneLocation.Create(request.EnforcementZoneId, request.LocationId, timeProvider.GetUtcNow());
        db.EnforcementZoneLocations.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        await RecalculateZoneCompliancesAsync(request.EnforcementZoneId, cancellationToken);
        return Result.Success<EnforcementZoneLocation, RequirementsEvaluationErrors>(link);
    }

    public async Task<bool> DeleteZoneLocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnforcementZoneLocation? link = await db.EnforcementZoneLocations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (link is null)
            return false;

        Guid zoneId = link.EnforcementZoneId;
        db.EnforcementZoneLocations.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        await RecalculateZoneCompliancesAsync(zoneId, cancellationToken);
        return true;
    }

    public async Task<Result<ZoneRequirementPolicy, RequirementsEvaluationErrors>> CreateZoneRequirementPolicyAsync(CreateZoneRequirementPolicyRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.EnforcementZones.AnyAsync(item => item.Id == request.EnforcementZoneId, cancellationToken))
            return Result.Failure<ZoneRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.EnforcementZoneNotFound);

        if (!await db.RequirementDefinitions.AnyAsync(item => item.Id == request.RequirementDefinitionId, cancellationToken))
            return Result.Failure<ZoneRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.RequirementDefinitionNotFound);

        ZoneRequirementPolicy policy = ZoneRequirementPolicy.Create(request.EnforcementZoneId, request.RequirementDefinitionId, request.SubjectKind, request.IsBlocking, timeProvider.GetUtcNow());
        db.ZoneRequirementPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        await RecalculateZoneCompliancesAsync(policy.EnforcementZoneId, cancellationToken);
        return Result.Success<ZoneRequirementPolicy, RequirementsEvaluationErrors>(policy);
    }

    public async Task<Result<ContractorJobRequirementPolicy, RequirementsEvaluationErrors>> CreateContractorJobRequirementPolicyAsync(CreateContractorJobRequirementPolicyRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.EnforcementZones.AnyAsync(item => item.Id == request.EnforcementZoneId, cancellationToken))
            return Result.Failure<ContractorJobRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.EnforcementZoneNotFound);

        if (!await db.RequirementDefinitions.AnyAsync(item => item.Id == request.RequirementDefinitionId, cancellationToken))
            return Result.Failure<ContractorJobRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.RequirementDefinitionNotFound);

        if (!await contractorsDb.JobTypes.AnyAsync(item => item.Id == request.JobTypeId, cancellationToken))
            return Result.Failure<ContractorJobRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.JobTypeNotFound);

        ContractorJobRequirementPolicy policy = ContractorJobRequirementPolicy.Create(request.EnforcementZoneId, request.JobTypeId, request.RequirementDefinitionId, request.IsBlocking, timeProvider.GetUtcNow());
        db.ContractorJobRequirementPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        await RecalculateZoneCompliancesAsync(policy.EnforcementZoneId, cancellationToken);
        return Result.Success<ContractorJobRequirementPolicy, RequirementsEvaluationErrors>(policy);
    }

    public async Task<Result<EnforcementZoneAccessPolicy, RequirementsEvaluationErrors>> CreateEnforcementZoneAccessPolicyAsync(CreateEnforcementZoneAccessPolicyRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.EnforcementZones.AnyAsync(item => item.Id == request.EnforcementZoneId, cancellationToken))
            return Result.Failure<EnforcementZoneAccessPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.EnforcementZoneNotFound);

        if (!await accessControlDb.AccessItems.AnyAsync(item => item.Id == request.AccessItemId, cancellationToken))
            return Result.Failure<EnforcementZoneAccessPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.AccessItemNotFound);

        EnforcementZoneAccessPolicy policy = EnforcementZoneAccessPolicy.Create(request.EnforcementZoneId, request.AccessItemId, timeProvider.GetUtcNow());
        db.EnforcementZoneAccessPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        await RecalculateZoneCompliancesAsync(policy.EnforcementZoneId, cancellationToken);
        return Result.Success<EnforcementZoneAccessPolicy, RequirementsEvaluationErrors>(policy);
    }

    public async Task<Result<IReadOnlyList<ZoneCompliance>, RequirementsEvaluationErrors>> EvaluateForLocationAsync(EvaluateZoneComplianceRequest request, CancellationToken cancellationToken = default)
    {
        if (!await identitiesDb.Identities.AnyAsync(item => item.Id == request.IdentityId, cancellationToken))
            return Result.Failure<IReadOnlyList<ZoneCompliance>, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.IdentityNotFound);

        Guid[]? zoneIds = await locationResolver.ResolveApplicableZoneIdsAsync(request.LocationId, cancellationToken);
        if (zoneIds is null)
            return Result.Failure<IReadOnlyList<ZoneCompliance>, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.LocationNotFound);

        List<ZoneCompliance> compliances = [];
        foreach (Guid zoneId in zoneIds)
        {
            ZoneCompliance compliance = await EvaluateZoneAsync(request.IdentityId, request.SubjectKind, zoneId, cancellationToken);
            compliances.Add(compliance);
        }

        return Result.Success<IReadOnlyList<ZoneCompliance>, RequirementsEvaluationErrors>(compliances);
    }

    public async Task RecalculateIdentityCompliancesAsync(Guid identityId, CancellationToken cancellationToken = default)
    {
        (Guid EnforcementZoneId, RequirementSubjectKind SubjectKind)[] items = await db.ZoneCompliances
            .Where(item => item.IdentityId == identityId)
            .Select(item => new ValueTuple<Guid, RequirementSubjectKind>(item.EnforcementZoneId, item.SubjectKind))
            .ToArrayAsync(cancellationToken);

        foreach ((Guid zoneId, RequirementSubjectKind subjectKind) in items)
            await EvaluateZoneAsync(identityId, subjectKind, zoneId, cancellationToken);
    }

    public async Task RecalculateZoneCompliancesAsync(Guid zoneId, CancellationToken cancellationToken = default)
    {
        (Guid IdentityId, RequirementSubjectKind SubjectKind)[] items = await db.ZoneCompliances
            .Where(item => item.EnforcementZoneId == zoneId)
            .Select(item => new ValueTuple<Guid, RequirementSubjectKind>(item.IdentityId, item.SubjectKind))
            .ToArrayAsync(cancellationToken);

        foreach ((Guid identityId, RequirementSubjectKind subjectKind) in items)
            await EvaluateZoneAsync(identityId, subjectKind, zoneId, cancellationToken);
    }

    public async Task RecalculateByRequirementDefinitionAsync(Guid requirementDefinitionId, CancellationToken cancellationToken = default)
    {
        (Guid IdentityId, RequirementSubjectKind SubjectKind, Guid EnforcementZoneId)[] items = await db.Set<ZoneComplianceRequirementResult>()
            .Where(item => item.RequirementDefinitionId == requirementDefinitionId)
            .Join(db.ZoneCompliances, result => result.ZoneComplianceId, compliance => compliance.Id, (result, compliance) => new ValueTuple<Guid, RequirementSubjectKind, Guid>(compliance.IdentityId, compliance.SubjectKind, compliance.EnforcementZoneId))
            .Distinct()
            .ToArrayAsync(cancellationToken);

        foreach ((Guid identityId, RequirementSubjectKind subjectKind, Guid zoneId) in items)
            await EvaluateZoneAsync(identityId, subjectKind, zoneId, cancellationToken);
    }

    private async Task<ZoneCompliance> EvaluateZoneAsync(Guid identityId, RequirementSubjectKind subjectKind, Guid zoneId, CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        EnforcementZone zone = await db.EnforcementZones.SingleAsync(item => item.Id == zoneId, cancellationToken);
        List<EffectiveRequirement> effectiveRequirements = await ResolveEffectiveRequirementsAsync(identityId, subjectKind, zone.Id, now, cancellationToken);

        List<ZoneComplianceRequirementResult> results = [];
        List<DateTimeOffset?> fulfilledRequirementExpiries = [];
        List<string> failures = [];

        foreach (EffectiveRequirement effectiveRequirement in effectiveRequirements)
        {
            RequirementEvaluation evaluation = await EvaluateRequirementAsync(identityId, effectiveRequirement.RequirementDefinition, now, cancellationToken);
            results.Add(ZoneComplianceRequirementResult.Create(
                effectiveRequirement.RequirementDefinition.Id,
                evaluation.Status,
                evaluation.EvidenceKind,
                evaluation.EvidenceReference,
                evaluation.Reason,
                evaluation.ValidUntil));

            if (evaluation.Status == RequirementResultStatus.Fulfilled)
                fulfilledRequirementExpiries.Add(evaluation.ValidUntil);
            else if (effectiveRequirement.IsBlocking)
                failures.Add(evaluation.Reason);
        }

        ZoneComplianceStatus calculatedStatus = failures.Count == 0 ? ZoneComplianceStatus.Compliant : ZoneComplianceStatus.NonCompliant;
        DateTimeOffset? validUntil = calculatedStatus == ZoneComplianceStatus.Compliant
            ? GetComplianceValidUntil(fulfilledRequirementExpiries)
            : null;
        string reasonSummary = calculatedStatus == ZoneComplianceStatus.Compliant
            ? "All blocking requirements fulfilled."
            : failures[0];

        ZoneCompliance? existing = await db.ZoneCompliances
            .Include(item => item.RequirementResults)
            .SingleOrDefaultAsync(item => item.IdentityId == identityId && item.EnforcementZoneId == zone.Id, cancellationToken);

        ZoneCompliance compliance = existing is null
            ? ZoneCompliance.Create(zone.Id, identityId, subjectKind, calculatedStatus, now, validUntil, now, reasonSummary, results)
            : existing;

        if (existing is null)
            db.ZoneCompliances.Add(compliance);
        else
            compliance.Update(subjectKind, calculatedStatus, now, validUntil, now, reasonSummary, results);

        await db.SaveChangesAsync(cancellationToken);
        await ReprojectZoneAccessAsync(compliance, cancellationToken);
        return compliance;
    }

    private async Task<List<EffectiveRequirement>> ResolveEffectiveRequirementsAsync(
        Guid identityId,
        RequirementSubjectKind subjectKind,
        Guid zoneId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        RequirementDefinition[] definitions = await db.RequirementDefinitions
            .Where(item => item.IsActive)
            .ToArrayAsync(cancellationToken);
        Dictionary<Guid, RequirementDefinition> definitionsById = definitions.ToDictionary(item => item.Id);

        ZoneRequirementPolicy[] zonePolicies = await db.ZoneRequirementPolicies
            .Where(item => item.EnforcementZoneId == zoneId && item.IsEnabled)
            .Where(item => item.SubjectKind == subjectKind || item.SubjectKind == RequirementSubjectKind.Any)
            .ToArrayAsync(cancellationToken);

        List<EffectiveRequirement> requirements = zonePolicies
            .Where(policy => definitionsById.ContainsKey(policy.RequirementDefinitionId))
            .Select(policy => new EffectiveRequirement(definitionsById[policy.RequirementDefinitionId], policy.IsBlocking))
            .ToList();

        if (subjectKind != RequirementSubjectKind.Contractor)
            return requirements
                .GroupBy(item => item.RequirementDefinition.Id)
                .Select(group => new EffectiveRequirement(group.First().RequirementDefinition, group.Any(item => item.IsBlocking)))
                .ToList();

        Guid[] contractorIds = await identitiesDb.ContractorAffiliations
            .Where(item => item.IdentityId == identityId)
            .Select(item => item.ContractorId)
            .ToArrayAsync(cancellationToken);

        if (contractorIds.Length == 0)
            return requirements;

        ContractorAssignmentContext[] activeAssignments = await contractorsDb.ContractorJobAssignments
            .Where(item => contractorIds.Contains(item.ContractorId))
            .Where(item => item.Status == ContractorJobAssignmentStatus.Planned || item.Status == ContractorJobAssignmentStatus.Active)
            .Where(item => item.AssignedUntil > now)
            .Join(contractorsDb.ContractorJobs.Where(job => job.Status == ContractorJobStatus.Planned || job.Status == ContractorJobStatus.Active),
                assignment => assignment.ContractorJobId,
                job => job.Id,
                (assignment, job) => new ContractorAssignmentContext(job.LocationId, job.JobTypeId))
            .ToArrayAsync(cancellationToken);

        if (activeAssignments.Length == 0)
            return requirements;

        Guid[] locationIds = activeAssignments.Select(item => item.LocationId).Distinct().ToArray();
        Dictionary<Guid, Guid[]> zonesByLocation = [];
        foreach (Guid locationId in locationIds)
            zonesByLocation[locationId] = await locationResolver.ResolveApplicableZoneIdsAsync(locationId, cancellationToken) ?? [];

        Guid[] jobTypeIds = activeAssignments
            .Where(item => zonesByLocation.GetValueOrDefault(item.LocationId, []).Contains(zoneId))
            .Select(item => item.JobTypeId)
            .Distinct()
            .ToArray();

        if (jobTypeIds.Length == 0)
            return requirements;

        ContractorJobRequirementPolicy[] contractorPolicies = await db.ContractorJobRequirementPolicies
            .Where(item => item.EnforcementZoneId == zoneId && item.IsEnabled)
            .Where(item => jobTypeIds.Contains(item.JobTypeId))
            .ToArrayAsync(cancellationToken);

        List<EffectiveRequirement> contractorRequirements = contractorPolicies
            .Where(policy => definitionsById.ContainsKey(policy.RequirementDefinitionId))
            .Select(policy => new EffectiveRequirement(definitionsById[policy.RequirementDefinitionId], policy.IsBlocking))
            .ToList();

        return [..
            requirements
                .Concat(contractorRequirements)
                .GroupBy(item => item.RequirementDefinition.Id)
                .Select(group => new EffectiveRequirement(group.First().RequirementDefinition, group.Any(item => item.IsBlocking)))];
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
                .OrderByDescending(item => item.ValidUntil.HasValue)
                .ThenByDescending(item => item.ValidUntil)
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

    private async Task ReprojectZoneAccessAsync(ZoneCompliance compliance, CancellationToken cancellationToken)
    {
        ProjectedZoneAccessAssignment[] existingAssignments = await db.ProjectedZoneAccessAssignments
            .Where(item => item.ZoneComplianceId == compliance.Id)
            .ToArrayAsync(cancellationToken);

        foreach (ProjectedZoneAccessAssignment assignment in existingAssignments)
            await pacsAssignmentService.RevokeBySourceAssignmentIdAsync(assignment.Id, cancellationToken);

        if (existingAssignments.Length > 0)
        {
            db.ProjectedZoneAccessAssignments.RemoveRange(existingAssignments);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!IsUsable(compliance, timeProvider.GetUtcNow()))
            return;

        EnforcementZoneAccessPolicy[] accessPolicies = await db.EnforcementZoneAccessPolicies
            .Where(item => item.EnforcementZoneId == compliance.EnforcementZoneId && item.IsEnabled)
            .ToArrayAsync(cancellationToken);

        EnforcementZoneLocation[] zoneLocations = await db.EnforcementZoneLocations
            .Where(item => item.EnforcementZoneId == compliance.EnforcementZoneId)
            .ToArrayAsync(cancellationToken);

        PACSAssignmentDurationKind durationKind = compliance.ValidUntil.HasValue
            ? PACSAssignmentDurationKind.Temporary
            : PACSAssignmentDurationKind.Permanent;

        foreach (EnforcementZoneAccessPolicy policy in accessPolicies)
        {
            foreach (EnforcementZoneLocation location in zoneLocations)
            {
                ProjectedZoneAccessAssignment assignment = ProjectedZoneAccessAssignment.Create(compliance.Id, policy.Id, policy.AccessItemId, location.LocationId, timeProvider.GetUtcNow());
                db.ProjectedZoneAccessAssignments.Add(assignment);
                await db.SaveChangesAsync(cancellationToken);
                _ = await pacsAssignmentService.CreateAssignmentsForGrantAsync(
                    assignment.Id,
                    compliance.IdentityId,
                    policy.AccessItemId,
                    location.LocationId,
                    durationKind,
                    compliance.ValidFrom,
                    compliance.ValidUntil,
                    cancellationToken);
            }
        }
    }

    private static bool IsUsable(ZoneCompliance compliance, DateTimeOffset now) =>
        compliance.CalculatedStatus == ZoneComplianceStatus.Compliant
        && (!compliance.ValidUntil.HasValue || compliance.ValidUntil.Value > now);

    private static DateTimeOffset? GetComplianceValidUntil(IEnumerable<DateTimeOffset?> requirementExpiries)
    {
        DateTimeOffset? result = null;

        foreach (DateTimeOffset? expiry in requirementExpiries)
        {
            if (!expiry.HasValue)
                continue;

            if (!result.HasValue || expiry.Value < result.Value)
                result = expiry.Value;
        }

        return result;
    }

    private static DateTimeOffset? GetRequirementValidUntil(IEnumerable<RequirementEvidence> evidence)
    {
        DateTimeOffset? result = null;

        foreach (RequirementEvidence item in evidence)
        {
            if (!item.ValidUntil.HasValue)
                return null;

            if (!result.HasValue || item.ValidUntil.Value > result.Value)
                result = item.ValidUntil.Value;
        }

        return result;
    }

    private sealed record EffectiveRequirement(RequirementDefinition RequirementDefinition, bool IsBlocking);
    private sealed record RequirementEvaluation(RequirementResultStatus Status, RequirementEvidenceKind? EvidenceKind, string? EvidenceReference, DateTimeOffset? ValidUntil, string Reason);
    private sealed record ContractorAssignmentContext(Guid LocationId, Guid JobTypeId);
}
