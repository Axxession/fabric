using Fabric.Server.Identities.Domain;

namespace Fabric.Server.Identities.Contracts;

public record ListIdentitiesRequest(
    string? Query,
    IdentityStatus? Status,
    IdentityAffiliationType? AffiliationType,
    int Page = 0,
    int PageSize = 25);

public record CreateIdentityRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? PreferredName,
    string? Email,
    string? Phone);

public record UpdateIdentityProfileRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? PreferredName,
    string? Email,
    string? Phone);
