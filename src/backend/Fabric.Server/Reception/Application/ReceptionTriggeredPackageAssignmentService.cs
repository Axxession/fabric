using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Application;

public sealed class ReceptionTriggeredPackageAssignmentService(
    ReceptionDbContext db,
    AccessGrantService accessGrantService,
    IdentityService identityService)
{
    public async Task ApplyTrigger(ExpectedArrival arrival, ReceptionAccessPolicyTrigger trigger, CancellationToken cancellationToken = default)
    {
        if (!AppliesToArrival(arrival, trigger) || !arrival.LocationId.HasValue)
            return;

        List<ReceptionAccessRuleAssignment> assignments = await GetMatchingAssignments(trigger, cancellationToken);
        foreach (ReceptionAccessRuleAssignment assignment in assignments)
            await CreateGrantIfMissing(arrival, assignment, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecreateAssignedPolicies(
        ExpectedArrival arrival,
        AccessGrantRevokeCause revokeCause,
        string revokedBy,
        CancellationToken cancellationToken = default)
    {
        await RetractAssignedPolicies(arrival.Id, revokeCause, revokedBy, cancellationToken);

        foreach (ReceptionAccessPolicyTrigger trigger in GetTriggeredStates(arrival))
            await ApplyTrigger(arrival, trigger, cancellationToken);
    }

    public async Task RetractAssignedPolicies(
        Guid arrivalId,
        AccessGrantRevokeCause revokeCause,
        string revokedBy,
        CancellationToken cancellationToken = default)
    {
        List<ReceptionAssignedAccessPolicy> assignedPolicies = await db.AssignedAccessPolicies
            .Where(policy => policy.ArrivalId == arrivalId)
            .ToListAsync(cancellationToken);

        foreach (ReceptionAssignedAccessPolicy assignedPolicy in assignedPolicies)
        {
            Result<AccessGrant, AccessCatalogErrors> revoke = await accessGrantService.RevokeAsync(assignedPolicy.AccessGrantId, revokeCause, revokedBy, cancellationToken);
            if (revoke.IsFailure(out AccessCatalogErrors error) && error != AccessCatalogErrors.AccessGrantAlreadyRevoked && error != AccessCatalogErrors.AccessGrantNotFound)
                throw new InvalidOperationException($"Failed to revoke reception access grant {assignedPolicy.AccessGrantId}: {error}.");
        }

        db.AssignedAccessPolicies.RemoveRange(assignedPolicies);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<ReceptionAccessRuleAssignment>> GetMatchingAssignments(
        ReceptionAccessPolicyTrigger trigger,
        CancellationToken cancellationToken) =>
        await db.AccessRuleAssignments
            .Where(assignment => assignment.Trigger == trigger)
            .ToListAsync(cancellationToken);

    private async Task CreateGrantIfMissing(
        ExpectedArrival arrival,
        ReceptionAccessRuleAssignment assignment,
        CancellationToken cancellationToken)
    {
        bool exists = await db.AssignedAccessPolicies
            .AnyAsync(policy => policy.ArrivalId == arrival.Id && policy.RuleAssignmentId == assignment.Id, cancellationToken);

        if (exists)
            return;

        Guid? identityId = await ResolveIdentityIdAsync(arrival, cancellationToken);
        if (!identityId.HasValue || !arrival.LocationId.HasValue)
            return;

        DateTimeOffset validFrom = arrival.ExpectedArrivalTime.AddMinutes(-assignment.GracePeriodMinutes);
        DateTimeOffset validUntil = arrival.ExpectedOffboardTime.AddMinutes(assignment.GracePeriodMinutes);

        Result<AccessGrant, AccessCatalogErrors> create = await accessGrantService.CreateAsync(
            assignment.PackageId,
            identityId.Value,
            [arrival.LocationId.Value],
            AssignmentChannel.AutomaticConfiguration,
            AssignmentSourceKind.ReceptionArrival,
            arrival.Id,
            AccessDurationKind.Temporary,
            validFrom,
            validUntil,
            $"Automatic reception access from trigger {assignment.Trigger}.",
            cancellationToken);

        if (create.IsFailure(out AccessCatalogErrors error))
            throw new InvalidOperationException($"Failed to create reception access grant for arrival {arrival.Id}: {error}.");

        create.IsSuccess(out AccessGrant accessGrant);
        db.AssignedAccessPolicies.Add(ReceptionAssignedAccessPolicy.Create(arrival.Id, assignment.Id, accessGrant.Id, assignment.PackageId));
    }

    private async Task<Guid?> ResolveIdentityIdAsync(ExpectedArrival arrival, CancellationToken cancellationToken)
    {
        if (arrival.IdentityId.HasValue)
            return arrival.IdentityId.Value;

        if (arrival.Type == ArrivalType.Visitor && arrival.VisitorId.HasValue)
            return await identityService.GetIdentityIdForVisitorAsync(arrival.VisitorId.Value, cancellationToken);

        if (arrival.Type == ArrivalType.Contractor && arrival.ContractorId.HasValue)
            return await identityService.GetIdentityIdForContractorAsync(arrival.ContractorId.Value, cancellationToken);

        return null;
    }

    private static bool AppliesToArrival(ExpectedArrival arrival, ReceptionAccessPolicyTrigger trigger) =>
        trigger switch
        {
            ReceptionAccessPolicyTrigger.ExpectedVisitorAdded => arrival.Type == ArrivalType.Visitor,
            ReceptionAccessPolicyTrigger.VisitorConfirmed => arrival.Type == ArrivalType.Visitor,
            ReceptionAccessPolicyTrigger.VisitorOnboarded => arrival.Type == ArrivalType.Visitor,
            ReceptionAccessPolicyTrigger.ContractorExpectedAdded => arrival.Type == ArrivalType.Contractor,
            ReceptionAccessPolicyTrigger.ContractorOnboarded => arrival.Type == ArrivalType.Contractor,
            _ => false
        };

    private static List<ReceptionAccessPolicyTrigger> GetTriggeredStates(ExpectedArrival arrival)
    {
        if (arrival.Type == ArrivalType.Visitor)
        {
            List<ReceptionAccessPolicyTrigger> triggers = [ReceptionAccessPolicyTrigger.ExpectedVisitorAdded];
            if (arrival.Confirmed == true)
                triggers.Add(ReceptionAccessPolicyTrigger.VisitorConfirmed);
            if (arrival.Status == OnboardingStatus.Onboarded)
                triggers.Add(ReceptionAccessPolicyTrigger.VisitorOnboarded);
            return triggers;
        }

        if (arrival.Type == ArrivalType.Contractor)
        {
            List<ReceptionAccessPolicyTrigger> triggers = [ReceptionAccessPolicyTrigger.ContractorExpectedAdded];
            if (arrival.Status == OnboardingStatus.Onboarded)
                triggers.Add(ReceptionAccessPolicyTrigger.ContractorOnboarded);
            return triggers;
        }

        return [];
    }
}
