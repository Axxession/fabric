using Fabric.Server.Core;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Endpoints;

public static class ZoneComplianceEndpoints
{
    public static IEndpointRouteBuilder MapZoneComplianceEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder compliances = app.MapGroup("/api/requirements/compliances");
        compliances.MapGet("", ListZoneCompliances).Produces<Page<ZoneComplianceResponse>>();
        compliances.MapGet("/{id:guid}", GetZoneCompliance).Produces<ZoneComplianceResponse>().Produces(StatusCodes.Status404NotFound);
        compliances.MapPost("/evaluate", EvaluateForLocation).Produces<ZoneComplianceResponse[]>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> ListZoneCompliances([AsParameters] ListRequirementsRequest request, [FromQuery] Guid? identityId, RequirementsDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<ZoneCompliance> query = db.ZoneCompliances.AsNoTracking().Include(item => item.RequirementResults);
        if (identityId.HasValue)
            query = query.Where(item => item.IdentityId == identityId.Value);

        IPaged<ZoneCompliance> page = await query.OrderByDescending(item => item.LastEvaluatedAt).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(page.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetZoneCompliance(Guid id, RequirementsDbContext db, CancellationToken cancellationToken = default)
    {
        ZoneCompliance? compliance = await db.ZoneCompliances.AsNoTracking().Include(item => item.RequirementResults).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return compliance is null ? Results.NotFound() : Results.Ok(compliance.ToResponse());
    }

    private static async Task<IResult> EvaluateForLocation([FromBody] EvaluateZoneComplianceRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<ZoneCompliance>, RequirementsEvaluationErrors> result = await service.EvaluateForLocationAsync(request, cancellationToken);
        return result.Match<IResult>(compliances => Results.Ok(compliances.Select(item => item.ToResponse()).ToArray()), error => result.Map(items => items.Select(item => item.ToResponse()).ToArray()).AsResponse(MapError));
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(RequirementsEvaluationErrors error) => error switch
    {
        RequirementsEvaluationErrors.LocationNotFound => Problem(StatusCodes.Status404NotFound, "Location not found."),
        RequirementsEvaluationErrors.IdentityNotFound => Problem(StatusCodes.Status404NotFound, "Identity not found."),
        _ => Problem(StatusCodes.Status400BadRequest, "Compliance evaluation request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
