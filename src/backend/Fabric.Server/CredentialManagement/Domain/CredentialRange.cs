using Fabric.Server.Core;

namespace Fabric.Server.CredentialManagement.Domain;

public sealed class CredentialRange
{
    private CredentialRange() { }

    public Guid Id { get; private set; }
    public Guid CredentialTypeId { get; private set; }
    public long RangeStart { get; private set; }
    public long RangeStop { get; private set; }
    public long NextCandidateNumber { get; private set; }
    public bool IsActive { get; private set; }

    public static Result<CredentialRange, CredentialManagementErrors> Create(Guid credentialTypeId, long rangeStart, long rangeStop, bool isActive)
    {
        if (rangeStop < rangeStart)
            return Result.Failure<CredentialRange, CredentialManagementErrors>(CredentialManagementErrors.CredentialRangeInvalid);

        return Result.Success<CredentialRange, CredentialManagementErrors>(new CredentialRange
        {
            Id = Guid.NewGuid(),
            CredentialTypeId = credentialTypeId,
            RangeStart = rangeStart,
            RangeStop = rangeStop,
            NextCandidateNumber = rangeStart,
            IsActive = isActive
        });
    }

    public Result<CredentialManagementErrors> Update(long rangeStart, long rangeStop, bool isActive)
    {
        if (rangeStop < rangeStart)
            return Result.Failure(CredentialManagementErrors.CredentialRangeInvalid);

        RangeStart = rangeStart;
        RangeStop = rangeStop;
        if (NextCandidateNumber < RangeStart || NextCandidateNumber > RangeStop)
            NextCandidateNumber = RangeStart;
        IsActive = isActive;
        return Result.Success<CredentialManagementErrors>();
    }

    public bool Contains(string identifier) =>
        long.TryParse(identifier, out long parsed) && parsed >= RangeStart && parsed <= RangeStop;

    public long NormalizeCandidate(long candidate)
    {
        if (candidate < RangeStart || candidate > RangeStop)
            return RangeStart;

        return candidate;
    }

    public void AdvanceNextCandidate(long issuedNumber)
    {
        NextCandidateNumber = issuedNumber >= RangeStop ? RangeStart : issuedNumber + 1;
    }
}
