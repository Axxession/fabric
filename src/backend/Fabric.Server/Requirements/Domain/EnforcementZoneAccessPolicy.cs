using Fabric.Server.Core;

namespace Fabric.Server.Requirements.Domain;

public sealed class EnforcementZoneAccessPolicy
{
    private EnforcementZoneAccessPolicy() { }

    public Guid Id { get; private set; }
    public Guid EnforcementZoneId { get; private set; }
    public Guid AccessItemId { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static EnforcementZoneAccessPolicy Create(Guid enforcementZoneId, Guid accessItemId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            EnforcementZoneId = enforcementZoneId,
            AccessItemId = accessItemId,
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(Guid accessItemId, DateTimeOffset now)
    {
        AccessItemId = accessItemId;
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
