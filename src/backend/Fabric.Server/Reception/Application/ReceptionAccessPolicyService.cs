using Fabric.Server.Core;
using Fabric.Server.Locations.Application;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Application;

public class ReceptionAccessPolicyService(
    ReceptionDbContext db,
    LocationService locationService)
{
    public async Task ApplyTrigger(ExpectedArrival arrival, ReceptionAccessPolicyTrigger trigger, CancellationToken cancellationToken = default)
    {
        if (!AppliesToArrival(arrival, trigger) || !arrival.LocationId.HasValue)
            return;

        List<ReceptionAccessRuleAssignment> assignments = await GetMatchingAssignments(arrival.LocationId.Value, trigger, cancellationToken);
        foreach (ReceptionAccessRuleAssignment assignment in assignments)
            await CreatePolicyIfMissing(arrival, assignment, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecreateAssignedPolicies(ExpectedArrival arrival, CancellationToken cancellationToken = default)
    {
        await RetractAssignedPolicies(arrival.Id, cancellationToken);

        foreach (ReceptionAccessPolicyTrigger trigger in GetTriggeredStates(arrival))
            await ApplyTrigger(arrival, trigger, cancellationToken);
    }

    public async Task RetractAssignedPolicies(Guid arrivalId, CancellationToken cancellationToken = default)
    {
        List<ReceptionAssignedAccessPolicy> assignedPolicies = await db.AssignedAccessPolicies
            .Where(policy => policy.ArrivalId == arrivalId)
            .ToListAsync(cancellationToken);

        if (assignedPolicies.Count > 0)
            throw new NotImplementedException("Reception PACS policy retraction has not been migrated from AccessPolicies yet.");

        db.AssignedAccessPolicies.RemoveRange(assignedPolicies);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<ReceptionAccessRuleAssignment>> GetMatchingAssignments(
        Guid arrivalLocationId,
        ReceptionAccessPolicyTrigger trigger,
        CancellationToken cancellationToken)
    {
        List<ReceptionAccessRuleAssignment> candidates = await db.AccessRuleAssignments
            .Where(assignment => assignment.Trigger == trigger)
            .ToListAsync(cancellationToken);

        List<ReceptionAccessRuleAssignment> matches = [];
        foreach (ReceptionAccessRuleAssignment assignment in candidates)
        {
            if (await locationService.IsPartOfLocationTree(arrivalLocationId, assignment.LocationId, cancellationToken))
                matches.Add(assignment);
        }

        return matches;
    }

    private async Task CreatePolicyIfMissing(
        ExpectedArrival arrival,
        ReceptionAccessRuleAssignment assignment,
        CancellationToken cancellationToken)
    {
        bool exists = await db.AssignedAccessPolicies
            .AnyAsync(policy => policy.ArrivalId == arrival.Id && policy.RuleAssignmentId == assignment.Id, cancellationToken);

        if (exists)
            return;

        Guid subjectId = GetSubjectId(arrival);
        if (subjectId == Guid.Empty)
            return;

        _ = assignment;
        _ = subjectId;
        throw new NotImplementedException("Reception PACS policy creation has not been migrated from AccessPolicies yet.");
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

    private static Guid GetSubjectId(ExpectedArrival arrival) =>
        arrival.Type switch
        {
            ArrivalType.Visitor => arrival.VisitorId ?? Guid.Empty,
            ArrivalType.Contractor => arrival.ContractorId ?? Guid.Empty,
            _ => Guid.Empty
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
