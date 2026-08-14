using Fabric.Server.Contractors.Domain;

namespace Fabric.Server.Contractors.Contracts;

public sealed record CompanyResponse(
    Guid Id,
    string Code,
    string Name,
    string? CompanyNumber,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class CompanyMapper
{
    public static CompanyResponse ToResponse(this Company company) =>
        new(company.Id, company.Code, company.Name, company.CompanyNumber, company.IsActive, company.CreatedAt, company.UpdatedAt);
}
