using Fabric.Server.Requirements.Domain;

namespace Fabric.Server.Requirements.Contracts;

public sealed record RequirementComplianceResponse(Guid RequirementDefinitionId, string Code, string Name, bool IsBlocking, RequirementResultStatus Status, string Reason, DateTimeOffset? ValidUntil, RequirementEvidenceKind[] AllowedEvidenceKinds);
public sealed record ContextComplianceResponse(ContextComplianceStatus Status, DateTimeOffset? CompliantUntil, string? UnavailableReason, RequirementComplianceResponse[] Requirements);
public sealed record ContractorAssignmentContextCompliancePackageResponse(Guid PackageId, string PackageName, ContextComplianceStatus Status, DateTimeOffset? CompliantUntil, RequirementComplianceResponse[] Requirements);
public sealed record ContractorAssignmentContextComplianceResponse(Guid ContractorId, Guid ContractorJobId, Guid LocationId, Guid JobTypeId, string? UnavailableReason, ContractorAssignmentContextCompliancePackageResponse[] Packages);

public sealed record RequirementDefinitionResponse(Guid Id, string Code, string Name, string? Description, RequirementEvidenceKind[] AllowedEvidenceKinds, bool IsSensitive, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record LocationRequirementPolicyResponse(Guid Id, Guid LocationId, Guid RequirementDefinitionId, RequirementSubjectKind SubjectKind, bool IsBlocking, bool IsEnabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record LocationJobRequirementPolicyResponse(Guid Id, Guid LocationId, Guid JobTypeId, Guid RequirementDefinitionId, bool IsBlocking, bool IsEnabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record LocationAttachedRequirementResponse(Guid PolicyId, Guid LocationId, Guid RequirementDefinitionId, string RequirementCode, string RequirementName, RequirementEvidenceKind[] AllowedEvidenceKinds, bool IsSensitive, RequirementSubjectKind SubjectKind, bool IsBlocking, bool IsEnabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record LocationJobAttachedRequirementResponse(Guid PolicyId, Guid LocationId, Guid JobTypeId, Guid RequirementDefinitionId, string RequirementCode, string RequirementName, RequirementEvidenceKind[] AllowedEvidenceKinds, bool IsSensitive, bool IsBlocking, bool IsEnabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record RequirementEvidenceResponse(Guid Id, Guid IdentityId, Guid RequirementDefinitionId, RequirementEvidenceKind EvidenceKind, RequirementEvidenceStatus Status, DateTimeOffset? ValidFrom, DateTimeOffset? ValidUntil, string? SourceReference, string Summary, bool IsSensitive, DateTimeOffset VerifiedAt, string? FileName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public static class RequirementsMapper
{
    public static RequirementDefinitionResponse ToResponse(this RequirementDefinition definition) =>
        new(definition.Id, definition.Code, definition.Name, definition.Description, definition.AllowedEvidenceKinds, definition.IsSensitive, definition.IsActive, definition.CreatedAt, definition.UpdatedAt);

    public static LocationRequirementPolicyResponse ToResponse(this LocationRequirementPolicy policy) =>
        new(policy.Id, policy.LocationId, policy.RequirementDefinitionId, policy.SubjectKind, policy.IsBlocking, policy.IsEnabled, policy.CreatedAt, policy.UpdatedAt);

    public static LocationJobRequirementPolicyResponse ToResponse(this LocationJobRequirementPolicy policy) =>
        new(policy.Id, policy.LocationId, policy.JobTypeId, policy.RequirementDefinitionId, policy.IsBlocking, policy.IsEnabled, policy.CreatedAt, policy.UpdatedAt);

    public static LocationAttachedRequirementResponse ToAttachedRequirementResponse(this LocationRequirementPolicy policy, RequirementDefinition definition) =>
        new(policy.Id, policy.LocationId, policy.RequirementDefinitionId, definition.Code, definition.Name, definition.AllowedEvidenceKinds, definition.IsSensitive, policy.SubjectKind, policy.IsBlocking, policy.IsEnabled, policy.CreatedAt, policy.UpdatedAt);

    public static LocationJobAttachedRequirementResponse ToAttachedRequirementResponse(this LocationJobRequirementPolicy policy, RequirementDefinition definition) =>
        new(policy.Id, policy.LocationId, policy.JobTypeId, policy.RequirementDefinitionId, definition.Code, definition.Name, definition.AllowedEvidenceKinds, definition.IsSensitive, policy.IsBlocking, policy.IsEnabled, policy.CreatedAt, policy.UpdatedAt);

    public static RequirementEvidenceResponse ToResponse(this RequirementEvidence evidence) =>
        new(evidence.Id, evidence.IdentityId, evidence.RequirementDefinitionId, evidence.EvidenceKind, evidence.Status, evidence.ValidFrom, evidence.ValidUntil, evidence.SourceReference, evidence.Summary, evidence.IsSensitive, evidence.VerifiedAt, evidence.FileName, evidence.CreatedAt, evidence.UpdatedAt);
}
