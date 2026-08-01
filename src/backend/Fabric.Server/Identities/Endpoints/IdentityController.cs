using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Identities.Contracts;
using Fabric.Server.Identities.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Identities.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder identities = app.MapGroup("/api/identities");

        identities.MapGet("", ListIdentities)
            .WithSummary("List identities")
            .WithDescription("List canonical identities with optional search and affiliation filters.")
            .Produces<Page<IdentityResponse>>();

        identities.MapGet("/{id:guid}", GetIdentity)
            .WithSummary("Get identity")
            .WithDescription("Get one canonical identity including thin affiliations.")
            .Produces<IdentityResponse>()
            .Produces(StatusCodes.Status404NotFound);

        identities.MapPost("", CreateIdentity)
            .WithSummary("Create identity")
            .WithDescription("Create a canonical identity record.")
            .Produces<IdentityResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        identities.MapPut("/{id:guid}/profile", UpdateIdentityProfile)
            .WithSummary("Update identity profile")
            .WithDescription("Update canonical identity profile fields.")
            .Produces<IdentityResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListIdentities(
        [FromQuery] string? query,
        [FromQuery] IdentityStatus? status,
        [FromQuery] IdentityAffiliationType? affiliationType,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IdentityService identityService,
        CancellationToken cancellationToken = default)
    {
        var request = new ListIdentitiesRequest(query, status, affiliationType, page ?? 0, pageSize ?? 25);
        IPaged<Identity> result = await identityService.SearchIdentitiesAsync(request, cancellationToken);
        return Results.Ok(result.Map(identity => identity.ToResponse()));
    }

    private static async Task<IResult> GetIdentity(
        Guid id,
        IdentityService identityService,
        CancellationToken cancellationToken = default)
    {
        Identity? identity = await identityService.GetIdentityAsync(id, cancellationToken);
        return identity is null ? Results.NotFound() : Results.Ok(identity.ToResponse());
    }

    private static async Task<IResult> CreateIdentity(
        [FromBody] CreateIdentityRequest request,
        IdentityService identityService,
        CancellationToken cancellationToken = default)
    {
        Result<Identity, IdentityErrors> result = await identityService.CreateIdentityAsync(
            request.FirstName,
            request.MiddleName,
            request.LastName,
            request.PreferredName,
            request.Email,
            request.Phone,
            cancellationToken);

        return result.Map(identity => identity.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> UpdateIdentityProfile(
        Guid id,
        [FromBody] UpdateIdentityProfileRequest request,
        IdentityService identityService,
        CancellationToken cancellationToken = default)
    {
        Result<Identity, IdentityErrors> result = await identityService.UpdateProfileAsync(
            id,
            request.FirstName,
            request.MiddleName,
            request.LastName,
            request.PreferredName,
            request.Email,
            request.Phone,
            cancellationToken);

        return result.Map(identity => identity.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(IdentityErrors error) =>
        error switch
        {
            IdentityErrors.IdentityNotFound => Problem(StatusCodes.Status404NotFound, "Identity not found."),
            IdentityErrors.FirstNameRequired => Problem(StatusCodes.Status400BadRequest, "First name is required."),
            IdentityErrors.LastNameRequired => Problem(StatusCodes.Status400BadRequest, "Last name is required."),
            IdentityErrors.AffiliationEffectiveUntilMustBeAfterEffectiveFrom => Problem(StatusCodes.Status400BadRequest, "Effective until must be after effective from."),
            IdentityErrors.VisitorAffiliationAlreadyExists => Problem(StatusCodes.Status409Conflict, "Visitor affiliation already exists."),
            IdentityErrors.EmployeeAffiliationAlreadyExists => Problem(StatusCodes.Status409Conflict, "Employee affiliation already exists."),
            IdentityErrors.ContractorAffiliationAlreadyExists => Problem(StatusCodes.Status409Conflict, "Contractor affiliation already exists."),
            IdentityErrors.VisitorAlreadyLinkedToDifferentIdentity => Problem(StatusCodes.Status409Conflict, "Visitor is already linked to a different identity."),
            IdentityErrors.EmployeeAlreadyLinkedToDifferentIdentity => Problem(StatusCodes.Status409Conflict, "Employee is already linked to a different identity."),
            IdentityErrors.ContractorAlreadyLinkedToDifferentIdentity => Problem(StatusCodes.Status409Conflict, "Contractor is already linked to a different identity."),
            IdentityErrors.AffiliationAlreadyEnded => Problem(StatusCodes.Status409Conflict, "Affiliation already ended."),
            _ => Problem(StatusCodes.Status400BadRequest, "Identity request is invalid."),
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
