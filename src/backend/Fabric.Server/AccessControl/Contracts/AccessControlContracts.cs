using System.Text.Json.Serialization;
using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Core;

namespace Fabric.Server.AccessControl.Contracts;

public sealed record ListAccessControlSystemsRequest : BaseListRequest
{
    public string? Name { get; set; }
}

public sealed record ListAccessItemsRequest : BaseListRequest
{
    public string? Name { get; set; }
}

public sealed record CreateUnipassAccessControlSystemRequest(
    string Name,
    string Endpoint,
    bool SslValidation,
    string Username,
    string Password);

public sealed record UpdateUnipassAccessControlSystemRequest(
    string Name,
    string Endpoint,
    bool SslValidation,
    string Username,
    string? Password,
    AccessControlSystemStatus Status);

public sealed record LinkAccessControlSystemLocationRequest(Guid LocationId);

public sealed record CreateAccessItemRequest(string Name, string? Description);

public sealed record UpdateAccessItemRequest(string Name, string? Description, AccessItemStatus Status);

public sealed record CreateUnipassAccessLevelTargetRequest(
    Guid AccessControlSystemId,
    string Name,
    int SiteId,
    int AccessRuleId,
    ProvisioningTiming ProvisioningTiming);

public sealed record UpdateUnipassAccessLevelTargetRequest(
    string Name,
    int SiteId,
    int AccessRuleId,
    bool IsEnabled,
    ProvisioningTiming ProvisioningTiming);

public sealed record ListCredentialTypeTargetsRequest : BaseListRequest
{
    public Guid? CredentialTypeId { get; set; }
    public Guid? AccessControlSystemId { get; set; }
}

public sealed record CreateUnipassCredentialTypeTargetRequest(
    Guid CredentialTypeId,
    Guid AccessControlSystemId,
    ProvisioningTiming ProvisioningTiming);

public sealed record UpdateUnipassCredentialTypeTargetRequest(
    ProvisioningTiming ProvisioningTiming,
    bool IsEnabled);

public sealed record ListPACSAssignmentsRequest : BaseListRequest
{
    public Guid? SourceAssignmentId { get; set; }
    public Guid? IdentityId { get; set; }
    public Guid? AccessControlSystemId { get; set; }
    public PACSAssignmentStatus? Status { get; set; }
}

public sealed record CreatePACSAssignmentsRequest(
    Guid SourceAssignmentId,
    Guid IdentityId,
    Guid AccessItemId,
    Guid LocationId,
    PACSAssignmentDurationKind DurationKind,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil);

public sealed record ListPACSSubjectProvisioningsRequest : BaseListRequest
{
    public Guid? PACSSubjectId { get; set; }
    public PACSSubjectProvisioningStatus? Status { get; set; }
}

public sealed record ListPACSSubjectsRequest : BaseListRequest
{
    public Guid? IdentityId { get; set; }
    public Guid? AccessControlSystemId { get; set; }
}

public sealed record UpsertPACSSubjectProvisioningRequest(
    Guid IdentityId,
    Guid AccessControlSystemId,
    PACSSubjectState DesiredState,
    string DesiredFirstName,
    string DesiredLastName,
    string? DesiredEmail,
    PACSSubjectProvisioningReason Reason,
    PACSSubjectProvisioningSourceKind SourceKind,
    Guid SourceId);

public sealed record ResolveAccessControlSystemResponse(Guid MatchedLocationId, Guid AccessControlSystemId);

public sealed record PACSSubjectResponse(
    Guid Id,
    Guid IdentityId,
    Guid AccessControlSystemId,
    string NativeSubjectId,
    PACSSubjectState State,
    string FirstName,
    string LastName,
    string? Email,
    DateTimeOffset LastSynchronizedAt);

public sealed record PACSSubjectProvisioningResponse(
    Guid Id,
    Guid PACSSubjectId,
    PACSSubjectState DesiredState,
    string DesiredFirstName,
    string DesiredLastName,
    string? DesiredEmail,
    PACSSubjectProvisioningReason Reason,
    PACSSubjectProvisioningSourceKind SourceKind,
    Guid SourceId,
    PACSSubjectProvisioningStatus Status,
    DateTimeOffset ScheduledFor,
    DateTimeOffset? LastRetryAt,
    string? LastKnownError,
    int AttemptCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PACSSubjectProvisioningResultResponse(
    PACSSubjectResponse Subject,
    PACSSubjectProvisioningResponse? Provisioning);

public sealed record PACSAssignmentResponse(
    Guid Id,
    Guid SourceAssignmentId,
    Guid AccessLevelTargetId,
    Guid AccessControlSystemId,
    Guid IdentityId,
    PACSAssignmentDurationKind DurationKind,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    PACSAssignmentStatus Status,
    DateTimeOffset ScheduledFor,
    string? NativeAssignmentId,
    string? FailureReason,
    DateTimeOffset? ProvisionedAt,
    DateTimeOffset? CompletedAt);

public sealed record ListPACSProvisioningsRequest : BaseListRequest
{
    public Guid? IdentityId { get; set; }
    public Guid? AccessControlSystemId { get; set; }
    public PACSProvisioningStatus? Status { get; set; }
}

public sealed record ListCredentialPACSAssignmentsRequest : BaseListRequest
{
    public Guid? CredentialId { get; set; }
    public Guid[] CredentialIds { get; set; } = [];
    public Guid? AccessControlSystemId { get; set; }
    public CredentialPACSAssignmentStatus? Status { get; set; }
}

public sealed record PACSProvisioningResponse(
    Guid Id,
    Guid AccessLevelTargetId,
    Guid AccessControlSystemId,
    Guid IdentityId,
    PACSAssignmentDurationKind DurationKind,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    ProvisioningTiming ProvisioningTiming,
    PACSProvisioningStatus Status,
    DateTimeOffset ScheduledFor,
    string? NativeAssignmentId,
    string? FailureReason,
    DateTimeOffset? ProvisionedAt,
    DateTimeOffset? CompletedAt,
    Guid[] SourceAssignmentIds);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UnipassCredentialTypeTargetResponse), "unipass")]
public abstract record CredentialTypeTargetResponse
{
    public required Guid Id { get; init; }
    public required Guid CredentialTypeId { get; init; }
    public required Guid AccessControlSystemId { get; init; }
    public required ProvisioningTiming ProvisioningTiming { get; init; }
    public required bool IsEnabled { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record UnipassCredentialTypeTargetResponse : CredentialTypeTargetResponse;

public sealed record CredentialPACSAssignmentResponse(
    Guid Id,
    Guid CredentialId,
    Guid CredentialTypeTargetId,
    Guid AccessControlSystemId,
    CredentialPACSAssignmentStatus Status,
    DateTimeOffset ScheduledFor,
    int AttemptCount,
    DateTimeOffset? LastAttemptAt,
    string? NativeAssignmentId,
    DateTimeOffset? ProvisionedAt,
    DateTimeOffset? RevokedAt,
    string? FailureReasonCode,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AccessControlSystemLocationResponse(Guid Id, Guid AccessControlSystemId, Guid LocationId);

public sealed record AccessControlSystemResponse(
    Guid Id,
    string Name,
    AccessControlProviderKind ProviderKind,
    AccessControlSystemStatus Status,
    string Endpoint,
    bool SslValidation,
    bool HasSecret);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UnipassAccessControlSystemConfigurationResponse), "unipass")]
public abstract record AccessControlSystemConfigurationResponse;

public sealed record UnipassAccessControlSystemConfigurationResponse(
    string Username) : AccessControlSystemConfigurationResponse;

public sealed record AccessControlSystemDetailsResponse(
    AccessControlSystemResponse System,
    AccessControlSystemConfigurationResponse Configuration);

public sealed record AccessItemResponse(
    Guid Id,
    string Name,
    string? Description,
    AccessItemStatus Status);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UnipassAccessLevelTargetResponse), "unipass")]
public abstract record AccessLevelTargetResponse
{
    public required Guid Id { get; init; }
    public required Guid AccessItemId { get; init; }
    public required Guid AccessControlSystemId { get; init; }
    public required string Name { get; init; }
    public required bool IsEnabled { get; init; }
    public required ProvisioningTiming ProvisioningTiming { get; init; }
}

public sealed record UnipassAccessLevelTargetResponse : AccessLevelTargetResponse
{
    public required int SiteId { get; init; }
    public required string SiteName { get; init; }
    public required int AccessRuleId { get; init; }
    public required string AccessRuleName { get; init; }
}

public static class AccessControlMapper
{
    public static AccessControlSystemResponse ToResponse(this AccessControlSystem system) =>
        new(
            system.Id,
            system.Name,
            system.ProviderKind,
            system.Status,
            system.UnipassConfig?.Endpoint ?? string.Empty,
            system.UnipassConfig?.SslValidation ?? false,
            !string.IsNullOrWhiteSpace(system.UnipassConfig?.Password));

    public static AccessControlSystemDetailsResponse ToDetailsResponse(this AccessControlSystem system) =>
        new(
            system.ToResponse(),
            system.ToConfigurationResponse());

    public static AccessControlSystemConfigurationResponse ToConfigurationResponse(this AccessControlSystem system) =>
        system.ProviderKind switch
        {
            AccessControlProviderKind.Unipass => new UnipassAccessControlSystemConfigurationResponse(
                system.UnipassConfig?.Username ?? string.Empty),
            _ => throw new InvalidOperationException("Unknown access control system type.")
        };

    public static AccessControlSystemLocationResponse ToResponse(this AccessControlSystemLocation link) =>
        new(link.Id, link.AccessControlSystemId, link.LocationId);

    public static AccessItemResponse ToResponse(this AccessItem item) =>
        new(item.Id, item.Name, item.Description, item.Status);

    public static AccessLevelTargetResponse ToResponse(this AccessLevelTarget target) =>
        target switch
        {
            UnipassAccessLevelTarget unipass => new UnipassAccessLevelTargetResponse
            {
                Id = unipass.Id,
                AccessItemId = unipass.AccessItemId,
                AccessControlSystemId = unipass.AccessControlSystemId,
                Name = unipass.Name,
                IsEnabled = unipass.IsEnabled,
                ProvisioningTiming = unipass.ProvisioningTiming,
                SiteId = unipass.SiteId,
                SiteName = unipass.SiteName,
                AccessRuleId = unipass.AccessRuleId,
                AccessRuleName = unipass.AccessRuleName
            },
            _ => throw new InvalidOperationException("Unknown access level target type.")
        };

    public static ResolveAccessControlSystemResponse ToResponse(this ResolvedAccessControlSystem resolved) =>
        new(resolved.LocationId, resolved.AccessControlSystemId);

    public static PACSSubjectResponse ToResponse(this PACSSubject subject) =>
        new(
            subject.Id,
            subject.IdentityId,
            subject.AccessControlSystemId,
            subject.NativeSubjectId,
            subject.State,
            subject.FirstName,
            subject.LastName,
            subject.Email,
            subject.LastSynchronizedAt);

    public static PACSSubjectProvisioningResponse ToResponse(this PACSSubjectProvisioning provisioning) =>
        new(
            provisioning.Id,
            provisioning.PACSSubjectId,
            provisioning.DesiredState,
            provisioning.DesiredFirstName,
            provisioning.DesiredLastName,
            provisioning.DesiredEmail,
            provisioning.Reason,
            provisioning.SourceKind,
            provisioning.SourceId,
            provisioning.Status,
            provisioning.ScheduledFor,
            provisioning.LastRetryAt,
            provisioning.LastKnownError,
            provisioning.AttemptCount,
            provisioning.CreatedAt,
            provisioning.UpdatedAt);

    public static PACSSubjectProvisioningResultResponse ToResponse(this PACSSubjectProvisioningResult result) =>
        new(result.Subject.ToResponse(), result.Provisioning?.ToResponse());

    public static PACSAssignmentResponse ToResponse(this PACSAssignment assignment) =>
        new(
            assignment.Id,
            assignment.SourceAssignmentId,
            assignment.AccessLevelTargetId,
            assignment.AccessControlSystemId,
            assignment.IdentityId,
            assignment.DurationKind,
            assignment.ValidFrom,
            assignment.ValidUntil,
            assignment.Status,
            assignment.ScheduledFor,
            assignment.NativeAssignmentId,
            assignment.FailureReason,
            assignment.ProvisionedAt,
            assignment.CompletedAt);

    public static PACSProvisioningResponse ToResponse(this PACSProvisioning provisioning, Guid[] sourceAssignmentIds) =>
        new(
            provisioning.Id,
            provisioning.AccessLevelTargetId,
            provisioning.AccessControlSystemId,
            provisioning.IdentityId,
            provisioning.DurationKind,
            provisioning.ValidFrom,
            provisioning.ValidUntil,
            provisioning.ProvisioningTiming,
            provisioning.Status,
            provisioning.ScheduledFor,
            provisioning.NativeAssignmentId,
            provisioning.FailureReason,
            provisioning.ProvisionedAt,
            provisioning.CompletedAt,
            sourceAssignmentIds);

    public static CredentialTypeTargetResponse ToResponse(this CredentialTypeTarget target) =>
        target switch
        {
            UnipassCredentialTypeTarget unipass => new UnipassCredentialTypeTargetResponse
            {
                Id = unipass.Id,
                CredentialTypeId = unipass.CredentialTypeId,
                AccessControlSystemId = unipass.AccessControlSystemId,
                ProvisioningTiming = unipass.ProvisioningTiming,
                IsEnabled = unipass.IsEnabled,
                CreatedAt = unipass.CreatedAt,
                UpdatedAt = unipass.UpdatedAt
            },
            _ => throw new InvalidOperationException("Unknown credential type target type.")
        };

    public static CredentialPACSAssignmentResponse ToResponse(this CredentialPACSAssignment assignment) =>
        new(assignment.Id, assignment.CredentialId, assignment.CredentialTypeTargetId, assignment.AccessControlSystemId, assignment.Status, assignment.ScheduledFor, assignment.AttemptCount, assignment.LastAttemptAt, assignment.NativeAssignmentId, assignment.ProvisionedAt, assignment.RevokedAt, assignment.FailureReasonCode, assignment.ErrorMessage, assignment.CreatedAt, assignment.UpdatedAt);
}
