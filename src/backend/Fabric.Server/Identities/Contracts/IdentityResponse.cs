using Fabric.Server.Identities.Domain;

namespace Fabric.Server.Identities.Contracts;

public record IdentityAffiliationSummaryResponse(Guid Id, Guid SourceId, AffiliationStatus Status, DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveUntil);

public record IdentityResponse(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? PreferredName,
    string DisplayName,
    string? Email,
    string? Phone,
    IdentityStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<IdentityAffiliationSummaryResponse> EmployeeAffiliations,
    IReadOnlyList<IdentityAffiliationSummaryResponse> ContractorAffiliations,
    IReadOnlyList<IdentityAffiliationSummaryResponse> VisitorAffiliations);

public static class IdentityMapper
{
    public static IdentityResponse ToResponse(this Identity identity) =>
        new(
            identity.Id,
            identity.FirstName,
            identity.MiddleName,
            identity.LastName,
            identity.PreferredName,
            identity.DisplayName,
            identity.Email,
            identity.Phone,
            identity.Status,
            identity.CreatedAt,
            identity.UpdatedAt,
            identity.EmployeeAffiliations.Select(ToResponse).ToArray(),
            identity.ContractorAffiliations.Select(ToResponse).ToArray(),
            identity.VisitorAffiliations.Select(ToResponse).ToArray());

    private static IdentityAffiliationSummaryResponse ToResponse(EmployeeAffiliation affiliation) =>
        new(affiliation.Id, affiliation.EmployeeId, affiliation.Status, affiliation.EffectiveFrom, affiliation.EffectiveUntil);

    private static IdentityAffiliationSummaryResponse ToResponse(ContractorAffiliation affiliation) =>
        new(affiliation.Id, affiliation.ContractorId, affiliation.Status, affiliation.EffectiveFrom, affiliation.EffectiveUntil);

    private static IdentityAffiliationSummaryResponse ToResponse(VisitorAffiliation affiliation) =>
        new(affiliation.Id, affiliation.VisitorId, affiliation.Status, affiliation.EffectiveFrom, affiliation.EffectiveUntil);
}
