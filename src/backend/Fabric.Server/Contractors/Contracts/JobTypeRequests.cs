using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Contracts;

public sealed record ListJobTypesRequest : BaseListRequest
{
    public string? Query { get; set; }
    public bool? IsActive { get; set; }
}

public sealed record CreateJobTypeRequest(string Code, string Name, string? Description);

public sealed record UpdateJobTypeRequest(string Code, string Name, string? Description);
