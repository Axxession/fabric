using Fabric.Server.Core;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Endpoints;

public static class RequirementEvidenceEndpoints
{
    public static IEndpointRouteBuilder MapRequirementEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder evidence = app.MapGroup("/api/requirements/evidence");
        evidence.MapGet("", ListRequirementEvidence).Produces<Page<RequirementEvidenceResponse>>();
        evidence.MapGet("/{id:guid}", GetRequirementEvidence).Produces<RequirementEvidenceResponse>().Produces(StatusCodes.Status404NotFound);
        evidence.MapPost("", CreateRequirementEvidence).Produces<RequirementEvidenceResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        evidence.MapPut("/{id:guid}", UpdateRequirementEvidence).Produces<RequirementEvidenceResponse>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> ListRequirementEvidence([AsParameters] ListRequirementsRequest request, [FromQuery] Guid? identityId, RequirementsDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<RequirementEvidence> query = db.RequirementEvidence.AsNoTracking();
        if (identityId.HasValue)
            query = query.Where(item => item.IdentityId == identityId.Value);
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(item => EF.Functions.ILike(item.Summary, filter) || item.SourceReference != null && EF.Functions.ILike(item.SourceReference, filter));
        }

        IPaged<RequirementEvidence> page = await query.OrderByDescending(item => item.VerifiedAt).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(page.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetRequirementEvidence(Guid id, RequirementsDbContext db, CancellationToken cancellationToken = default)
    {
        RequirementEvidence? evidence = await db.RequirementEvidence.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return evidence is null ? Results.NotFound() : Results.Ok(evidence.ToResponse());
    }

    private static async Task<IResult> CreateRequirementEvidence([FromBody] CreateRequirementEvidenceRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<RequirementEvidence, RequirementEvidenceErrors> result = await service.CreateRequirementEvidenceAsync(request, cancellationToken);
        return result.Match<IResult>(evidence => Results.Created($"/api/requirements/evidence/{evidence.Id}", evidence.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> UpdateRequirementEvidence(Guid id, [FromBody] UpdateRequirementEvidenceRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<RequirementEvidence, RequirementEvidenceErrors> result = await service.UpdateRequirementEvidenceAsync(id, request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(RequirementEvidenceErrors error) => error switch
    {
        RequirementEvidenceErrors.RequirementEvidenceNotFound => Problem(StatusCodes.Status404NotFound, "Requirement evidence not found."),
        RequirementEvidenceErrors.SummaryRequired => Problem(StatusCodes.Status400BadRequest, "Requirement evidence summary is required."),
        RequirementEvidenceErrors.ValidUntilMustBeAfterValidFrom => Problem(StatusCodes.Status400BadRequest, "Valid until must be after valid from."),
        _ => Problem(StatusCodes.Status400BadRequest, "Requirement evidence request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
