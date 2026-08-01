namespace Fabric.Server.AccessControl.Domain;

public abstract class CredentialTypeTarget
{
    private protected CredentialTypeTarget() { }

    public Guid Id { get; protected set; }
    public Guid CredentialTypeId { get; protected set; }
    public Guid AccessControlSystemId { get; protected set; }
    public ProvisioningTiming ProvisioningTiming { get; protected set; }
    public bool IsEnabled { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }

    public void Update(ProvisioningTiming provisioningTiming, bool isEnabled, DateTimeOffset now)
    {
        ProvisioningTiming = provisioningTiming;
        IsEnabled = isEnabled;
        UpdatedAt = now;
    }
}

public sealed class UnipassCredentialTypeTarget : CredentialTypeTarget
{
    private UnipassCredentialTypeTarget() { }

    public static UnipassCredentialTypeTarget Create(
        Guid credentialTypeId,
        Guid accessControlSystemId,
        ProvisioningTiming provisioningTiming,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            CredentialTypeId = credentialTypeId,
            AccessControlSystemId = accessControlSystemId,
            ProvisioningTiming = provisioningTiming,
            IsEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };
}
