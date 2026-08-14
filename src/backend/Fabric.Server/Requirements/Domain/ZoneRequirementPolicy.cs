using Fabric.Server.Core;

namespace Fabric.Server.Requirements.Domain;

public sealed class ZoneRequirementPolicy
{
    private ZoneRequirementPolicy() { }

    public Guid Id { get; private set; }
    public Guid EnforcementZoneId { get; private set; }
    public Guid RequirementDefinitionId { get; private set; }
    public RequirementSubjectKind SubjectKind { get; private set; }
    public bool IsBlocking { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ZoneRequirementPolicy Create(
        Guid enforcementZoneId,
        Guid requirementDefinitionId,
        RequirementSubjectKind subjectKind,
        bool isBlocking,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            EnforcementZoneId = enforcementZoneId,
            RequirementDefinitionId = requirementDefinitionId,
            SubjectKind = subjectKind,
            IsBlocking = isBlocking,
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(RequirementSubjectKind subjectKind, bool isBlocking, DateTimeOffset now)
    {
        SubjectKind = subjectKind;
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
