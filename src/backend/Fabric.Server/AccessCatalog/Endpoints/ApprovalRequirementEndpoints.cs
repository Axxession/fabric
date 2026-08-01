using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Contracts;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Endpoints;

public static class ApprovalRequirementEndpoints
{
    public static IEndpointRouteBuilder MapApprovalRequirementEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder requirements = app.MapGroup("/api/access-catalog/approval-requirements");

        requirements.MapGet("", ListApprovalRequirements).Produces<Page<ApprovalRequirementResponse>>();
        requirements.MapPost("/{approvalRequirementId:guid}/decisions", CreateApprovalDecision).Produces<ApprovalDecisionResponse>();

        return app;
    }

    private static async Task<IResult> ListApprovalRequirements([AsParameters] ListApprovalRequirementsRequest request, [FromQuery] Guid[]? ids, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<ApprovalRequirement> query = db.ApprovalRequirements.AsNoTracking();
        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));
        if (request.RequestId.HasValue)
            query = query.Where(item => item.RequestId == request.RequestId.Value);
        if (request.RequiredApproverIdentityId.HasValue)
            query = query.Where(item => item.RequiredApproverIdentityId == request.RequiredApproverIdentityId.Value);
        if (request.ApprovalGroupId.HasValue)
            query = query.Where(item => item.ApprovalGroupId == request.ApprovalGroupId.Value);
        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);

        IPaged<ApprovalRequirement> result = await query.OrderBy(item => item.CreatedAt).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateApprovalDecision(Guid approvalRequirementId, [FromBody] CreateApprovalDecisionRequest request, ApprovalDecisionService service, CancellationToken cancellationToken = default)
    {
        Result<ApprovalDecision, AccessCatalogErrors> result = await service.DecideAsync(approvalRequirementId, request.ApproverIdentityId, request.DecisionKind, request.Note, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(AccessCatalogErrors error) => error switch
    {
        AccessCatalogErrors.PackageRequestNotFound => Problem(StatusCodes.Status404NotFound, "Package request not found."),
        AccessCatalogErrors.ApprovalRequirementNotFound => Problem(StatusCodes.Status404NotFound, "Approval requirement not found."),
        AccessCatalogErrors.ApprovalRequirementAlreadyCompleted => Problem(StatusCodes.Status409Conflict, "Approval requirement already completed."),
        AccessCatalogErrors.ApprovalDecisionNotAllowed => Problem(StatusCodes.Status403Forbidden, "Approver is not allowed to decide this requirement."),
        AccessCatalogErrors.AccessGrantNotFound => Problem(StatusCodes.Status404NotFound, "Access grant not found."),
        AccessCatalogErrors.AccessProvisioningFailed => Problem(StatusCodes.Status409Conflict, "Failed to provision one or more PACS assignments for this access grant."),
        _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected access catalog error.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) => (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
