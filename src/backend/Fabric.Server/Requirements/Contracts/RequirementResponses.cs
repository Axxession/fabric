using Fabric.Server.Requirements.Domain;

namespace Fabric.Server.Requirements.Contracts;

public sealed record EnforcementZoneResponse(Guid Id, string Code, string Name, string? Description, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record RequirementDefinitionResponse(Guid Id, string Code, string Name, string? Description, RequirementEvaluatorKind EvaluatorKind, bool IsSensitive, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record EnforcementZoneLocationResponse(Guid Id, Guid EnforcementZoneId, Guid LocationId, DateTimeOffset CreatedAt);
public sealed record ZoneRequirementPolicyResponse(Guid Id, Guid EnforcementZoneId, Guid RequirementDefinitionId, RequirementSubjectKind SubjectKind, bool IsBlocking, bool IsEnabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record ContractorJobRequirementPolicyResponse(Guid Id, Guid EnforcementZoneId, Guid JobTypeId, Guid RequirementDefinitionId, bool IsBlocking, bool IsEnabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record EnforcementZoneAccessPolicyResponse(Guid Id, Guid EnforcementZoneId, Guid AccessItemId, bool IsEnabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record RequirementEvidenceResponse(Guid Id, Guid IdentityId, Guid RequirementDefinitionId, RequirementEvidenceKind EvidenceKind, RequirementEvidenceStatus Status, DateTimeOffset? ValidFrom, DateTimeOffset? ValidUntil, string? SourceReference, string Summary, bool IsSensitive, DateTimeOffset VerifiedAt, string? FileName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record ZoneComplianceRequirementResultResponse(Guid Id, Guid RequirementDefinitionId, RequirementResultStatus Status, RequirementEvidenceKind? EvidenceKind, string? EvidenceReference, string Reason, DateTimeOffset? ValidUntil);
public sealed record ZoneComplianceResponse(Guid Id, Guid EnforcementZoneId, Guid IdentityId, RequirementSubjectKind SubjectKind, ZoneComplianceStatus CalculatedStatus, DateTimeOffset ValidFrom, DateTimeOffset? ValidUntil, DateTimeOffset LastEvaluatedAt, string ReasonSummary, IReadOnlyList<ZoneComplianceRequirementResultResponse> RequirementResults);

public static class RequirementsMapper
{
    public static EnforcementZoneResponse ToResponse(this EnforcementZone zone) =>
        new(zone.Id, zone.Code, zone.Name, zone.Description, zone.IsActive, zone.CreatedAt, zone.UpdatedAt);

    public static RequirementDefinitionResponse ToResponse(this RequirementDefinition definition) =>
        new(definition.Id, definition.Code, definition.Name, definition.Description, definition.EvaluatorKind, definition.IsSensitive, definition.IsActive, definition.CreatedAt, definition.UpdatedAt);

    public static EnforcementZoneLocationResponse ToResponse(this EnforcementZoneLocation link) =>
        new(link.Id, link.EnforcementZoneId, link.LocationId, link.CreatedAt);

    public static ZoneRequirementPolicyResponse ToResponse(this ZoneRequirementPolicy policy) =>
        new(policy.Id, policy.EnforcementZoneId, policy.RequirementDefinitionId, policy.SubjectKind, policy.IsBlocking, policy.IsEnabled, policy.CreatedAt, policy.UpdatedAt);

    public static ContractorJobRequirementPolicyResponse ToResponse(this ContractorJobRequirementPolicy policy) =>
        new(policy.Id, policy.EnforcementZoneId, policy.JobTypeId, policy.RequirementDefinitionId, policy.IsBlocking, policy.IsEnabled, policy.CreatedAt, policy.UpdatedAt);

    public static EnforcementZoneAccessPolicyResponse ToResponse(this EnforcementZoneAccessPolicy policy) =>
        new(policy.Id, policy.EnforcementZoneId, policy.AccessItemId, policy.IsEnabled, policy.CreatedAt, policy.UpdatedAt);

    public static RequirementEvidenceResponse ToResponse(this RequirementEvidence evidence) =>
        new(evidence.Id, evidence.IdentityId, evidence.RequirementDefinitionId, evidence.EvidenceKind, evidence.Status, evidence.ValidFrom, evidence.ValidUntil, evidence.SourceReference, evidence.Summary, evidence.IsSensitive, evidence.VerifiedAt, evidence.FileName, evidence.CreatedAt, evidence.UpdatedAt);

    public static ZoneComplianceResponse ToResponse(this ZoneCompliance compliance) =>
        new(
            compliance.Id,
            compliance.EnforcementZoneId,
            compliance.IdentityId,
            compliance.SubjectKind,
            compliance.CalculatedStatus,
            compliance.ValidFrom,
            compliance.ValidUntil,
            compliance.LastEvaluatedAt,
            compliance.ReasonSummary,
            compliance.RequirementResults.Select(item => new ZoneComplianceRequirementResultResponse(item.Id, item.RequirementDefinitionId, item.Status, item.EvidenceKind, item.EvidenceReference, item.Reason, item.ValidUntil)).ToArray());
}
