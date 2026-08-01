using Fabric.Server.Core;
using Fabric.Server.Tenants.Domain;

namespace Fabric.Server.Visitors.Contracts;

public sealed record ListHostsRequest : BaseListRequest
{
    public string? Query { get; set; }
}

public sealed record HostResponse(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string? Email,
    bool IsAllowListed);

public sealed record HostSettingsResponse(HostAssignmentMode AssignmentMode);

public sealed record UpdateHostSettingsRequest(HostAssignmentMode AssignmentMode);
