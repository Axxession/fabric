namespace Fabric.Server.AccessControl.Domain;

public sealed class PACSSubject
{
    private PACSSubject() { }

    public Guid Id { get; private set; }
    public Guid IdentityId { get; private set; }
    public Guid AccessControlSystemId { get; private set; }
    public string NativeSubjectId { get; private set; } = null!;
    public PACSSubjectState State { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? Email { get; private set; }
    public DateTimeOffset LastSynchronizedAt { get; private set; }

    public static PACSSubject Create(
        Guid identityId,
        Guid accessControlSystemId,
        string nativeSubjectId,
        PACSSubjectState state,
        string firstName,
        string lastName,
        string? email,
        DateTimeOffset lastSynchronizedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            AccessControlSystemId = accessControlSystemId,
            NativeSubjectId = nativeSubjectId,
            State = state,
            FirstName = firstName,
            LastName = lastName,
            Email = NormalizeOptional(email),
            LastSynchronizedAt = lastSynchronizedAt
        };

    public void ApplySynchronizedRepresentation(
        PACSSubjectState state,
        string firstName,
        string lastName,
        string? email,
        DateTimeOffset synchronizedAt)
    {
        State = state;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = NormalizeOptional(email);
        LastSynchronizedAt = synchronizedAt;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
