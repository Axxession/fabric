namespace Fabric.Server.Employees.Domain;

public sealed class EmployeeLifecycleEvent
{
    private EmployeeLifecycleEvent() { }

    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid IdentityId { get; set; }
    public EmployeeStatus FromStatus { get; set; }
    public EmployeeStatus ToStatus { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
    public EmployeeLifecycleSource Source { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static EmployeeLifecycleEvent Create(
        Guid employeeId,
        Guid identityId,
        EmployeeStatus fromStatus,
        EmployeeStatus toStatus,
        DateTimeOffset effectiveAt,
        EmployeeLifecycleSource source,
        string? reason,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            IdentityId = identityId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            EffectiveAt = effectiveAt,
            Source = source,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAt = createdAt,
        };
}
