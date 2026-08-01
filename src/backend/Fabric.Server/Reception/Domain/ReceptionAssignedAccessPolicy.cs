namespace Fabric.Server.Reception.Domain;

public sealed class ReceptionAssignedAccessPolicy
{
    private ReceptionAssignedAccessPolicy() { }

    public Guid Id { get; private set; }
    public Guid ArrivalId { get; private set; }
    public Guid RuleAssignmentId { get; private set; }
    public Guid AccessGrantId { get; private set; }
    public Guid PackageId { get; private set; }

    public static ReceptionAssignedAccessPolicy Create(
        Guid arrivalId,
        Guid ruleAssignmentId,
        Guid accessGrantId,
        Guid packageId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ArrivalId = arrivalId,
            RuleAssignmentId = ruleAssignmentId,
            AccessGrantId = accessGrantId,
            PackageId = packageId
        };
}
