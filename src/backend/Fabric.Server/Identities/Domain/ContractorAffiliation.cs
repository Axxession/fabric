using Fabric.Server.Core;

namespace Fabric.Server.Identities.Domain;

public sealed class ContractorAffiliation
{
    private ContractorAffiliation() { }

    public Guid Id { get; internal set; }
    public Guid IdentityId { get; internal set; }
    public Guid ContractorId { get; internal set; }
    public AffiliationStatus Status { get; internal set; }
    public DateTimeOffset EffectiveFrom { get; internal set; }
    public DateTimeOffset? EffectiveUntil { get; internal set; }

    internal static Result<ContractorAffiliation, IdentityErrors> Create(Guid contractorId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveUntil)
    {
        if (effectiveUntil.HasValue && effectiveUntil.Value <= effectiveFrom)
            return Result.Failure<ContractorAffiliation, IdentityErrors>(IdentityErrors.AffiliationEffectiveUntilMustBeAfterEffectiveFrom);

        return Result.Success<ContractorAffiliation, IdentityErrors>(new ContractorAffiliation
        {
            Id = Guid.NewGuid(),
            ContractorId = contractorId,
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
