using Fabric.Server.Contractors.Domain;

namespace Fabric.Server.Contractors.Contracts;

public sealed record ContractorResponse(
    Guid Id,
    Guid CompanyId,
    Guid? IdentityId,
    string FirstName,
    string LastName,
    string? Email,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class ContractorMapper
{
    public static ContractorResponse ToResponse(this Contractor contractor, Guid? identityId) =>
        new(contractor.Id, contractor.CompanyId, identityId, contractor.FirstName, contractor.LastName, contractor.Email, contractor.ArchivedAt, contractor.CreatedAt, contractor.UpdatedAt);
}
