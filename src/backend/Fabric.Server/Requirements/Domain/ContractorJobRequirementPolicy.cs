using Fabric.Server.Core;

namespace Fabric.Server.Requirements.Domain;

public sealed class ContractorJobRequirementPolicy
{
    private ContractorJobRequirementPolicy() { }

    public Guid Id { get; private set; }
    public Guid EnforcementZoneId { get; private set; }
    public Guid JobTypeId { get; private set; }
    public Guid RequirementDefinitionId { get; private set; }
    public bool IsBlocking { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ContractorJobRequirementPolicy Create(
        Guid enforcementZoneId,
        Guid jobTypeId,
        Guid requirementDefinitionId,
        bool isBlocking,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            EnforcementZoneId = enforcementZoneId,
            JobTypeId = jobTypeId,
            RequirementDefinitionId = requirementDefinitionId,
            IsBlocking = isBlocking,
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(Guid jobTypeId, bool isBlocking, DateTimeOffset now)
    {
        JobTypeId = jobTypeId;
        IsBlocking = isBlocking;
        UpdatedAt = now;
    }

    public Result<RequirementPolicyErrors> Enable(DateTimeOffset now)
    {
        if (IsEnabled)
            return Result.Failure(RequirementPolicyErrors.PolicyAlreadyEnabled);

        IsEnabled = true;
        UpdatedAt = now;
        return Result.Success<RequirementPolicyErrors>();
    }

    public Result<RequirementPolicyErrors> Disable(DateTimeOffset now)
    {
        if (!IsEnabled)
            return Result.Failure(RequirementPolicyErrors.PolicyAlreadyDisabled);

        IsEnabled = false;
        UpdatedAt = now;
        return Result.Success<RequirementPolicyErrors>();
    }
}
