namespace Fabric.Server.Tenants.Domain;

public enum HostAssignmentMode
{
    AllEmployees,
    AllowList
}

public sealed record HostSettings
{
    public HostAssignmentMode AssignmentMode { get; init; } = HostAssignmentMode.AllEmployees;
}
