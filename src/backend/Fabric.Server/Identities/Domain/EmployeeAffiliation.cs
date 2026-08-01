using Fabric.Server.Core;

namespace Fabric.Server.Identities.Domain;

public sealed class EmployeeAffiliation
{
    private EmployeeAffiliation() { }

    public Guid Id { get; internal set; }
    public Guid IdentityId { get; internal set; }
    public Guid EmployeeId { get; internal set; }
    public AffiliationStatus Status { get; internal set; }
    public DateTimeOffset EffectiveFrom { get; internal set; }
    public DateTimeOffset? EffectiveUntil { get; internal set; }

    internal static Result<EmployeeAffiliation, IdentityErrors> Create(Guid employeeId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveUntil)
    {
        if (effectiveUntil.HasValue && effectiveUntil.Value <= effectiveFrom)
            return Result.Failure<EmployeeAffiliation, IdentityErrors>(IdentityErrors.AffiliationEffectiveUntilMustBeAfterEffectiveFrom);

        return Result.Success<EmployeeAffiliation, IdentityErrors>(new EmployeeAffiliation
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Status = effectiveUntil.HasValue ? AffiliationStatus.Ended : AffiliationStatus.Active,
            EffectiveFrom = effectiveFrom,
            EffectiveUntil = effectiveUntil,
        });
    }

    internal Result<IdentityErrors> End(DateTimeOffset effectiveUntil)
    {
        if (Status == AffiliationStatus.Ended)
            return Result.Failure(IdentityErrors.AffiliationAlreadyEnded);

        if (effectiveUntil <= EffectiveFrom)
            return Result.Failure(IdentityErrors.AffiliationEffectiveUntilMustBeAfterEffectiveFrom);

        Status = AffiliationStatus.Ended;
        EffectiveUntil = effectiveUntil;
        return Result.Success<IdentityErrors>();
    }
}
