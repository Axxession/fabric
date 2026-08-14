using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Contracts;

public sealed record ListCompaniesRequest : BaseListRequest
{
    public string? Query { get; set; }
    public bool? IsActive { get; set; }
}

public sealed record CreateCompanyRequest(string Code, string Name, string? CompanyNumber);

public sealed record UpdateCompanyRequest(string Code, string Name, string? CompanyNumber);
