using Fabric.Server.Core;

namespace Fabric.Server.Identities.Domain;

public sealed class VisitorAffiliation
{
    private VisitorAffiliation() { }

    public Guid Id { get; internal set; }
    public Guid IdentityId { get; internal set; }
    public Guid VisitorId { get; internal set; }
    public AffiliationStatus Status { get; internal set; }
    public DateTimeOffset EffectiveFrom { get; internal set; }
    public DateTimeOffset? EffectiveUntil { get; internal set; }

    internal static Result<VisitorAffiliation, IdentityErrors> Create(Guid visitorId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveUntil)
    {
        if (effectiveUntil.HasValue && effectiveUntil.Value <= effectiveFrom)
            return Result.Failure<VisitorAffiliation, IdentityErrors>(IdentityErrors.AffiliationEffectiveUntilMustBeAfterEffectiveFrom);

        return Result.Success<VisitorAffiliation, IdentityErrors>(new VisitorAffiliation
        {
            Id = Guid.NewGuid(),
            VisitorId = visitorId,
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
