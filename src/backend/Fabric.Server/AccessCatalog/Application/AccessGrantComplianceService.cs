using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Application;

public sealed class AccessGrantComplianceService(
    AccessCatalogDbContext db,
    AccessControlDbContext accessControlDb,
    GrantRequirementsService grantRequirementsService,
    AccessGrantProvisioningSagaService provisioningSagaService,
    TimeProvider timeProvider)
{
    public async Task EvaluateGrantAsync(Guid accessGrantId, CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.SingleOrDefaultAsync(item => item.Id == accessGrantId, cancellationToken);
        if (grant is null || grant.Status != AccessGrantStatus.Active)
            return;

        GrantRequirement[] requirements = await db.GrantRequirements
            .Where(item => item.AccessGrantId == accessGrantId)
            .ToArrayAsync(cancellationToken);

        if (requirements.Length == 0)
        {
            _ = grant.UpdateCompliance(GrantComplianceStatus.Compliant, null, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await ReconcileProvisioningAsync(grant, cancellationToken);
            return;
        }

        IReadOnlyList<EvaluatedGrantRequirement> evaluations = await grantRequirementsService.EvaluateGrantRequirementsAsync(
            grant.IdentityId,
            requirements.Select(item => item.RequirementDefinitionId).Distinct().ToArray(),
            cancellationToken);

        Dictionary<Guid, GrantRequirementResult> existingResults = await db.GrantRequirementResults
            .Where(item => item.AccessGrantId == accessGrantId)
            .ToDictionaryAsync(item => item.RequirementDefinitionId, cancellationToken);

        foreach (EvaluatedGrantRequirement evaluation in evaluations)
        {
            if (existingResults.TryGetValue(evaluation.RequirementDefinitionId, out GrantRequirementResult? existing))
            {
                existing.Update(evaluation.Status, evaluation.EvidenceKind, evaluation.EvidenceReference, evaluation.Reason, evaluation.ValidUntil, evaluation.LastEvaluatedAt);
                continue;
            }

            db.GrantRequirementResults.Add(GrantRequirementResult.Create(
                grant.Id,
                evaluation.RequirementDefinitionId,
                evaluation.Status,
                evaluation.EvidenceKind,
                evaluation.EvidenceReference,
                evaluation.Reason,
                evaluation.ValidUntil,
                evaluation.LastEvaluatedAt));
        }

        bool anyBlockingFailure = requirements
            .Where(item => item.IsBlocking)
            .Join(evaluations, item => item.RequirementDefinitionId, item => item.RequirementDefinitionId, (item, evaluation) => evaluation)
            .Any(item => item.Status != RequirementResultStatus.Fulfilled);

        GrantComplianceStatus complianceStatus;
        DateTimeOffset? compliantUntil;
        if (anyBlockingFailure)
        {
            complianceStatus = GrantComplianceStatus.NonCompliant;
            compliantUntil = null;
        }
        else
        {
            compliantUntil = evaluations
                .Where(item => item.Status == RequirementResultStatus.Fulfilled)
                .Select(item => item.ValidUntil)
                .Where(item => item.HasValue)
                .OrderBy(item => item)
                .FirstOrDefault();

            bool temporary = compliantUntil.HasValue && (!grant.ValidUntil.HasValue || compliantUntil.Value < grant.ValidUntil.Value);
            complianceStatus = temporary ? GrantComplianceStatus.TemporarilyCompliant : GrantComplianceStatus.Compliant;
            if (!temporary)
                compliantUntil = null;
        }

        _ = grant.UpdateCompliance(complianceStatus, compliantUntil, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        await ReconcileProvisioningAsync(grant, cancellationToken);
    }

    public async Task ReevaluateIdentityRequirementAsync(Guid identityId, Guid requirementDefinitionId, CancellationToken cancellationToken = default)
    {
        Guid[] grantIds = await db.GrantRequirements
            .Where(item => item.RequirementDefinitionId == requirementDefinitionId)
            .Join(db.AccessGrants.Where(item => item.IdentityId == identityId && item.Status == AccessGrantStatus.Active),
                requirement => requirement.AccessGrantId,
                grant => grant.Id,
                (requirement, grant) => grant.Id)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        foreach (Guid grantId in grantIds)
            await EvaluateGrantAsync(grantId, cancellationToken);
    }

    private async Task ReconcileProvisioningAsync(AccessGrant grant, CancellationToken cancellationToken)
    {
        bool isComplianceRequired = await IsComplianceRequiredAsync(grant, cancellationToken);
        if (IsProvisionable(grant, isComplianceRequired, timeProvider.GetUtcNow()))
        {
            await provisioningSagaService.EnqueueAccessGrantCreatedAsync(grant.Id, cancellationToken);
            return;
        }

        await provisioningSagaService.EnqueueAccessGrantRevokedAsync(grant.Id, cancellationToken);
    }

    public static bool IsComplianceSatisfied(AccessGrant grant, bool isComplianceRequired) =>
        !isComplianceRequired
        || grant.ComplianceStatus == GrantComplianceStatus.Compliant
        || (grant.ComplianceStatus == GrantComplianceStatus.TemporarilyCompliant && grant.CompliantUntil.HasValue && grant.CompliantUntil.Value > grant.ValidFrom);

    public static bool IsProvisionable(AccessGrant grant, bool isComplianceRequired, DateTimeOffset now) =>
        grant.Status == AccessGrantStatus.Active
        && (!grant.ValidUntil.HasValue || grant.ValidUntil.Value > now)
        && (grant.ApprovalStatus == GrantApprovalStatus.Approved || grant.ApprovalStatus == GrantApprovalStatus.NotRequired)
        && IsComplianceSatisfied(grant, isComplianceRequired);

    public static DateTimeOffset? GetProvisionedUntil(AccessGrant grant)
    {
        if (grant.ComplianceStatus == GrantComplianceStatus.TemporarilyCompliant)
            return Min(grant.ValidUntil, grant.CompliantUntil);

        return grant.ValidUntil;
    }

    private static DateTimeOffset? Min(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (!left.HasValue)
            return right;
        if (!right.HasValue)
            return left;
        return left.Value <= right.Value ? left : right;
    }

    private async Task<bool> IsComplianceRequiredAsync(AccessGrant grant, CancellationToken cancellationToken)
    {
        if (!grant.AccessItemId.HasValue)
            return true;

        bool? isComplianceRequired = await accessControlDb.AccessItems
            .Where(item => item.Id == grant.AccessItemId.Value)
            .Select(item => (bool?)item.IsComplianceRequired)
            .SingleOrDefaultAsync(cancellationToken);

        return isComplianceRequired ?? true;
    }
}
