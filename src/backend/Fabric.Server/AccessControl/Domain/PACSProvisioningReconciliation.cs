namespace Fabric.Server.AccessControl.Domain;

public sealed class PACSProvisioningReconciliation
{
    private PACSProvisioningReconciliation() { }

    public Guid Id { get; private set; }
    public Guid IdentityId { get; private set; }
    public Guid AccessControlSystemId { get; private set; }
    public DateTimeOffset ScheduledFor { get; private set; }
    public DateTimeOffset? LastRetryAt { get; private set; }
    public string? LastKnownError { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static PACSProvisioningReconciliation Create(Guid identityId, Guid accessControlSystemId, DateTimeOffset scheduledFor, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            AccessControlSystemId = accessControlSystemId,
            ScheduledFor = scheduledFor,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void RescheduleNow(DateTimeOffset now)
    {
        ScheduledFor = now;
        LastRetryAt = null;
        LastKnownError = null;
        AttemptCount = 0;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset retryAt, DateTimeOffset now)
    {
        LastRetryAt = now;
        LastKnownError = error;
        AttemptCount++;
        ScheduledFor = retryAt;
        UpdatedAt = now;
    }
}
