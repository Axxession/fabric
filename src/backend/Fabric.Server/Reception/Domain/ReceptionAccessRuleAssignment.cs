using Fabric.Server.Core;

namespace Fabric.Server.Reception.Domain;

public sealed class ReceptionAccessRuleAssignment
{
    private ReceptionAccessRuleAssignment() { }

    public Guid Id { get; private set; }
    public Guid PackageId { get; private set; }
    public int GracePeriodMinutes { get; private set; }
    public ReceptionAccessPolicyTrigger Trigger { get; private set; }

    public static Result<ReceptionAccessRuleAssignment, ReceptionErrors> Create(
        Guid packageId,
        int gracePeriodMinutes,
        ReceptionAccessPolicyTrigger trigger)
    {
        if (gracePeriodMinutes < 0)
            return Result.Failure<ReceptionAccessRuleAssignment, ReceptionErrors>(ReceptionErrors.GracePeriodMustNotBeNegative);

        return Result.Success<ReceptionAccessRuleAssignment, ReceptionErrors>(new ReceptionAccessRuleAssignment
        {
            Id = Guid.NewGuid(),
            PackageId = packageId,
            GracePeriodMinutes = gracePeriodMinutes,
            Trigger = trigger
        });
    }

    public Result<ReceptionErrors> Update(
        Guid packageId,
        int gracePeriodMinutes,
        ReceptionAccessPolicyTrigger trigger)
    {
        if (gracePeriodMinutes < 0)
            return Result.Failure(ReceptionErrors.GracePeriodMustNotBeNegative);

        PackageId = packageId;
        GracePeriodMinutes = gracePeriodMinutes;
        Trigger = trigger;
        return Result.Success<ReceptionErrors>();
    }
}
