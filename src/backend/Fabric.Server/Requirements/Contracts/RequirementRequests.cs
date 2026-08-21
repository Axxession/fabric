using Fabric.Server.Core;
using Fabric.Server.Requirements.Domain;
using Microsoft.AspNetCore.Http;

namespace Fabric.Server.Requirements.Contracts;

public sealed record ListRequirementsRequest : BaseListRequest
{
    public Guid[]? Ids { get; set; }
    public string? Query { get; set; }
    public bool? IsActive { get; set; }
    public Guid? LocationId { get; set; }
}

public sealed record CreateRequirementDefinitionRequest(string Code, string Name, string? Description, RequirementEvidenceKind[] AllowedEvidenceKinds, bool IsSensitive);
public sealed record UpdateRequirementDefinitionRequest(string Code, string Name, string? Description, RequirementEvidenceKind[] AllowedEvidenceKinds, bool IsSensitive);
public sealed record CreateLocationRequirementPolicyRequest(Guid LocationId, Guid RequirementDefinitionId, RequirementSubjectKind SubjectKind, bool IsBlocking);
public sealed record ContractorAssignmentContextComplianceRequest(Guid ContractorId, Guid ContractorJobId, DateTimeOffset AssignedFrom, DateTimeOffset AssignedUntil);
public sealed record CreateContextComplianceWaiverRequest(Guid RequirementDefinitionId, DateTimeOffset ValidUntil, string Reason, string? SourceReference);
public sealed record ListLocationJobRequirementPoliciesRequest : BaseListRequest
{
    public Guid? LocationId { get; set; }
    public Guid? JobTypeId { get; set; }
    public Guid? RequirementDefinitionId { get; set; }
    public bool? IsEnabled { get; set; }
}

public sealed record CreateLocationJobRequirementPolicyRequest(Guid LocationId, Guid JobTypeId, Guid RequirementDefinitionId, bool IsBlocking);
public sealed record UpdateLocationJobRequirementPolicyRequest(bool IsBlocking);
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

public sealed class CreateRequirementEvidenceFormRequest
{
    public Guid IdentityId { get; set; }
    public Guid RequirementDefinitionId { get; set; }
    public RequirementEvidenceKind EvidenceKind { get; set; }
    public RequirementEvidenceStatus Status { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
    public string? SourceReference { get; set; }
    public string Summary { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
    public IFormFile? File { get; set; }
}

public sealed class UpdateRequirementEvidenceFormRequest
{
    public RequirementEvidenceStatus Status { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
    public string? SourceReference { get; set; }
    public string Summary { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
    public IFormFile? File { get; set; }
}
