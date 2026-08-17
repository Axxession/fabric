using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.ContractorJobs;

public sealed record ContractorJobPackageRuleResponse(Guid Id, Guid JobTypeId, Guid PackageId, Guid? LocationId, bool IsEnabled);
public sealed record CreateContractorJobPackageRuleRequest(Guid JobTypeId, Guid PackageId, Guid? LocationId);
public sealed record SetContractorJobPackageRuleEnabledRequest(bool IsEnabled);

public static class ContractorJobAccessAutomationEndpoints
{
    public static IEndpointRouteBuilder MapContractorJobAccessAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/sagas/contractor-jobs");

        group.MapPost("/onboarding/reconcile/{assignmentId:guid}", EnqueueOnboardingReconcile).Produces(StatusCodes.Status202Accepted);
        group.MapGet("/access-package-rules", ListRules).Produces<Page<ContractorJobPackageRuleResponse>>();
        group.MapPost("/access-package-rules", CreateRule).Produces<ContractorJobPackageRuleResponse>(StatusCodes.Status201Created);
        group.MapPut("/access-package-rules/{id:guid}/enabled", SetRuleEnabled).Produces<ContractorJobPackageRuleResponse>();
        group.MapDelete("/access-package-rules/{id:guid}", DeleteRule).Produces(StatusCodes.Status204NoContent);
        group.MapPost("/access/reconcile/{assignmentId:guid}", EnqueueAccessReconcile).Produces(StatusCodes.Status202Accepted);

        return app;
    }

    private static async Task<IResult> EnqueueOnboardingReconcile(Guid assignmentId, ContractorJobOnboardingSagaService service, CancellationToken cancellationToken = default)
    {
        await service.EnqueueAsync(assignmentId, "ManualReconcile", cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> ListRules([AsParameters] BaseListRequest request, SagasDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<ContractorJobPackageRule> result = await db.ContractorJobPackageRules
            .AsNoTracking()
            .OrderBy(item => item.JobTypeId)
            .ThenBy(item => item.LocationId)
            .ThenBy(item => item.PackageId)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(item => new ContractorJobPackageRuleResponse(item.Id, item.JobTypeId, item.PackageId, item.LocationId, item.IsEnabled)));
    }

    private static async Task<IResult> CreateRule([FromBody] CreateContractorJobPackageRuleRequest request, ContractorJobAccessAutomationService service, CancellationToken cancellationToken = default)
    {
        Result<ContractorJobPackageRule, string> result = await service.CreateRuleAsync(request.JobTypeId, request.PackageId, request.LocationId, cancellationToken);
        return result.Match<IResult>(
            item => Results.Created($"/api/sagas/contractor-jobs/access-package-rules/{item.Id}", new ContractorJobPackageRuleResponse(item.Id, item.JobTypeId, item.PackageId, item.LocationId, item.IsEnabled)),
            error => Results.Problem(error, statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<IResult> SetRuleEnabled(Guid id, [FromBody] SetContractorJobPackageRuleEnabledRequest request, SagasDbContext db, ContractorJobAccessAutomationService service, CancellationToken cancellationToken = default)
    {
        ContractorJobPackageRule? rule = await db.ContractorJobPackageRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null)
            return Results.NotFound();

        await service.ToggleRuleAsync(id, request.IsEnabled, cancellationToken);
        rule = await db.ContractorJobPackageRules.AsNoTracking().SingleAsync(item => item.Id == id, cancellationToken);
        return Results.Ok(new ContractorJobPackageRuleResponse(rule.Id, rule.JobTypeId, rule.PackageId, rule.LocationId, rule.IsEnabled));
    }

    private static async Task<IResult> DeleteRule(Guid id, ContractorJobAccessAutomationService service, CancellationToken cancellationToken = default)
    {
        bool deleted = await service.DeleteRuleAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> EnqueueAccessReconcile(Guid assignmentId, ContractorJobAccessAutomationService service, CancellationToken cancellationToken = default)
    {
        await service.EnqueueAsync(assignmentId, "ManualReconcile", cancellationToken);
        return Results.Accepted();
    }
}
