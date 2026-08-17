using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Contracts;

public sealed record ListContractorsRequest : BaseListRequest
{
    public string? Query { get; set; }
    public Guid? CompanyId { get; set; }
    public bool? IsArchived { get; set; }
    public Guid? IdentityId { get; set; }
}

public sealed record CreateContractorRequest(string FirstName, string LastName, string? Email, Guid CompanyId);

public sealed record UpdateContractorRequest(string FirstName, string LastName, string? Email, Guid CompanyId);
