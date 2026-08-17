namespace Fabric.Server.AccessCatalog.Domain;

public sealed class GrantRequirement
{
    private GrantRequirement() { }

    public Guid Id { get; private set; }
    public Guid AccessGrantId { get; private set; }
    public Guid RequirementDefinitionId { get; private set; }
    public string SourcePolicyKind { get; private set; } = null!;
    public Guid SourcePolicyId { get; private set; }
    public bool IsBlocking { get; private set; }
    public DateTimeOffset DerivedAt { get; private set; }

    public static GrantRequirement Create(
        Guid accessGrantId,
        Guid requirementDefinitionId,
        string sourcePolicyKind,
        Guid sourcePolicyId,
        bool isBlocking,
        DateTimeOffset derivedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            AccessGrantId = accessGrantId,
            RequirementDefinitionId = requirementDefinitionId,
            SourcePolicyKind = sourcePolicyKind,
            SourcePolicyId = sourcePolicyId,
            IsBlocking = isBlocking,
            DerivedAt = derivedAt
        };
}
