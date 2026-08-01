namespace Fabric.Server.Sagas.AccessGrantProvisioning;

public enum AccessGrantProvisioningSagaState
{
    PendingProvision,
    Provisioned,
    PendingRevocation,
    Revoked,
    Failed
}

public enum AccessGrantMaterializationOutcomeStatus
{
    Created,
    SkippedNoTarget,
    Failed
}

public sealed class AccessGrantProvisioningSaga
{
    public Guid Id { get; set; }
    public Guid AccessGrantId { get; set; }
    public AccessGrantProvisioningSagaState State { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AccessGrantMaterializationOutcome
{
    public Guid Id { get; set; }
    public Guid AccessGrantId { get; set; }
    public Guid AccessItemId { get; set; }
    public Guid LocationId { get; set; }
    public AccessGrantMaterializationOutcomeStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum AccessGrantProvisioningSagaEventType
{
    AccessGrantCreated,
    AccessGrantRevoked
}

public sealed class AccessGrantProvisioningSagaEvent
{
    public Guid Id { get; set; }
    public Guid SagaId { get; set; }
    public Guid AccessGrantId { get; set; }
    public AccessGrantProvisioningSagaEventType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }

    public static AccessGrantProvisioningSagaEvent Create(
        Guid sagaId,
        Guid accessGrantId,
        AccessGrantProvisioningSagaEventType type,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            SagaId = sagaId,
            AccessGrantId = accessGrantId,
            Type = type,
            CreatedAt = createdAt,
            RetryCount = 0
        };
}
