using Fabric.Server.Core;
using Fabric.Server.Requirements.Domain;

namespace Fabric.Server.Requirements.Contracts;

public sealed record ListRequirementsRequest : BaseListRequest
{
    public string? Query { get; set; }
    public bool? IsActive { get; set; }
}

public sealed record CreateEnforcementZoneRequest(string Code, string Name, string? Description);
public sealed record UpdateEnforcementZoneRequest(string Code, string Name, string? Description);
public sealed record CreateRequirementDefinitionRequest(string Code, string Name, string? Description, RequirementEvaluatorKind EvaluatorKind, bool IsSensitive);
public sealed record UpdateRequirementDefinitionRequest(string Code, string Name, string? Description, RequirementEvaluatorKind EvaluatorKind, bool IsSensitive);
public sealed record CreateEnforcementZoneLocationRequest(Guid EnforcementZoneId, Guid LocationId);
public sealed record CreateZoneRequirementPolicyRequest(Guid EnforcementZoneId, Guid RequirementDefinitionId, RequirementSubjectKind SubjectKind, bool IsBlocking);
public sealed record CreateContractorJobRequirementPolicyRequest(Guid EnforcementZoneId, Guid JobTypeId, Guid RequirementDefinitionId, bool IsBlocking);
public sealed record CreateEnforcementZoneAccessPolicyRequest(Guid EnforcementZoneId, Guid AccessItemId);
public sealed record CreateRequirementEvidenceRequest(
    Guid IdentityId,
    Guid RequirementDefinitionId,
    RequirementEvidenceKind EvidenceKind,
    RequirementEvidenceStatus Status,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    string? SourceReference,
    string Summary,
    bool IsSensitive,
    DateTimeOffset VerifiedAt,
    string? FileName,
    byte[]? Content);

public sealed record UpdateRequirementEvidenceRequest(
    RequirementEvidenceStatus Status,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    string? SourceReference,
    string Summary,
    bool IsSensitive,
    DateTimeOffset VerifiedAt,
    string? FileName,
    byte[]? Content);

public sealed record EvaluateZoneComplianceRequest(Guid IdentityId, RequirementSubjectKind SubjectKind, Guid LocationId);
