using Fabric.Server.Core;

namespace Fabric.Server.Requirements.Domain;

public sealed class LocationJobRequirementPolicy
{
    private LocationJobRequirementPolicy() { }

    public Guid Id { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid JobTypeId { get; private set; }
    public Guid RequirementDefinitionId { get; private set; }
    public bool IsBlocking { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static LocationJobRequirementPolicy Create(
        Guid locationId,
        Guid jobTypeId,
        Guid requirementDefinitionId,
        bool isBlocking,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            JobTypeId = jobTypeId,
            RequirementDefinitionId = requirementDefinitionId,
            IsBlocking = isBlocking,
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    public Result<RequirementPolicyErrors> SetEnabled(bool isEnabled, DateTimeOffset now)
    {
        if (IsEnabled == isEnabled)
            return Result.Failure(isEnabled ? RequirementPolicyErrors.PolicyAlreadyEnabled : RequirementPolicyErrors.PolicyAlreadyDisabled);

        IsEnabled = isEnabled;
        UpdatedAt = now;
        return Result.Success<RequirementPolicyErrors>();
    }
}
