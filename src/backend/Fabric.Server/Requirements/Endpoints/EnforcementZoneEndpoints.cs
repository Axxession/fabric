using Fabric.Server.Core;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Endpoints;

public static class EnforcementZoneEndpoints
{
    public static IEndpointRouteBuilder MapEnforcementZoneEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder zones = app.MapGroup("/api/requirements/enforcement-zones");

        zones.MapGet("", ListEnforcementZones).Produces<Page<EnforcementZoneResponse>>();
        zones.MapGet("/{id:guid}", GetEnforcementZone).Produces<EnforcementZoneResponse>().Produces(StatusCodes.Status404NotFound);
        zones.MapPost("", CreateEnforcementZone).Produces<EnforcementZoneResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        zones.MapPut("/{id:guid}", UpdateEnforcementZone).Produces<EnforcementZoneResponse>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        zones.MapPost("/{id:guid}/locations", AddEnforcementZoneLocation).Produces<EnforcementZoneLocationResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        zones.MapDelete("/locations/{locationLinkId:guid}", DeleteEnforcementZoneLocation).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> ListEnforcementZones([AsParameters] ListRequirementsRequest request, RequirementsDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<EnforcementZone> query = db.EnforcementZones.AsNoTracking();
        if (request.IsActive.HasValue)
            query = query.Where(item => item.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(item => EF.Functions.ILike(item.Code, filter) || EF.Functions.ILike(item.Name, filter));
        }

        IPaged<EnforcementZone> page = await query.OrderBy(item => item.Name).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(page.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetEnforcementZone(Guid id, RequirementsDbContext db, CancellationToken cancellationToken = default)
    {
        EnforcementZone? zone = await db.EnforcementZones.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return zone is null ? Results.NotFound() : Results.Ok(zone.ToResponse());
    }

    private static async Task<IResult> CreateEnforcementZone([FromBody] CreateEnforcementZoneRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<EnforcementZone, EnforcementZoneErrors> result = await service.CreateEnforcementZoneAsync(request, cancellationToken);
        return result.Match<IResult>(zone => Results.Created($"/api/requirements/enforcement-zones/{zone.Id}", zone.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> UpdateEnforcementZone(Guid id, [FromBody] UpdateEnforcementZoneRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<EnforcementZone, EnforcementZoneErrors> result = await service.UpdateEnforcementZoneAsync(id, request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> AddEnforcementZoneLocation(Guid id, [FromBody] CreateEnforcementZoneLocationRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<EnforcementZoneLocation, RequirementsEvaluationErrors> result = await service.AddZoneLocationAsync(request with { EnforcementZoneId = id }, cancellationToken);
        return result.Match<IResult>(link => Results.Created($"/api/requirements/enforcement-zones/locations/{link.Id}", link.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapEvaluationError));
    }

    private static async Task<IResult> DeleteEnforcementZoneLocation(Guid locationLinkId, RequirementsService service, CancellationToken cancellationToken = default)
    {
        bool deleted = await service.DeleteZoneLocationAsync(locationLinkId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(EnforcementZoneErrors error) => error switch
    {
        EnforcementZoneErrors.EnforcementZoneNotFound => Problem(StatusCodes.Status404NotFound, "Enforcement zone not found."),
        EnforcementZoneErrors.CodeRequired => Problem(StatusCodes.Status400BadRequest, "Enforcement zone code is required."),
        EnforcementZoneErrors.NameRequired => Problem(StatusCodes.Status400BadRequest, "Enforcement zone name is required."),
        _ => Problem(StatusCodes.Status400BadRequest, "Enforcement zone request is invalid.")
    };

    private static (int statusCode, ProblemDetails? problemDetails) MapEvaluationError(RequirementsEvaluationErrors error) => error switch
    {
        RequirementsEvaluationErrors.EnforcementZoneNotFound => Problem(StatusCodes.Status404NotFound, "Enforcement zone not found."),
        RequirementsEvaluationErrors.LocationNotFound => Problem(StatusCodes.Status404NotFound, "Location not found."),
        _ => Problem(StatusCodes.Status400BadRequest, "Request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
