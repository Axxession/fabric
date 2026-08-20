using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.LearningRequirements;

public sealed record LearningRequirementRuleResponse(Guid Id, Guid RequirementDefinitionId, Guid CourseId, LearningRequirementSatisfactionMode SatisfactionMode, decimal? MinimumScore, bool IsEnabled);
public sealed record CreateLearningRequirementRuleRequest(Guid RequirementDefinitionId, Guid CourseId, LearningRequirementSatisfactionMode SatisfactionMode, decimal? MinimumScore);
public sealed record UpdateLearningRequirementRuleRequest(Guid RequirementDefinitionId, Guid CourseId, LearningRequirementSatisfactionMode SatisfactionMode, decimal? MinimumScore);
public sealed record SetLearningRequirementRuleEnabledRequest(bool IsEnabled);

public static class LearningRequirementAutomationEndpoints
{
    public static IEndpointRouteBuilder MapLearningRequirementAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/sagas/learning-requirements")
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole });

        group.MapGet("/course-rules", ListRules).Produces<Page<LearningRequirementRuleResponse>>();
        group.MapGet("/course-rules/{id:guid}", GetRule).Produces<LearningRequirementRuleResponse>().Produces(StatusCodes.Status404NotFound);
        group.MapPost("/course-rules", CreateRule).Produces<LearningRequirementRuleResponse>(StatusCodes.Status201Created);
        group.MapPut("/course-rules/{id:guid}", UpdateRule).Produces<LearningRequirementRuleResponse>().Produces(StatusCodes.Status404NotFound);
        group.MapPut("/course-rules/{id:guid}/enabled", SetRuleEnabled).Produces<LearningRequirementRuleResponse>();
        group.MapDelete("/course-rules/{id:guid}", DeleteRule).Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> ListRules([AsParameters] BaseListRequest request, SagasDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<LearningRequirementRule> result = await db.LearningRequirementRules.AsNoTracking().OrderBy(item => item.RequirementDefinitionId).ThenBy(item => item.CourseId).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => new LearningRequirementRuleResponse(item.Id, item.RequirementDefinitionId, item.CourseId, item.SatisfactionMode, item.MinimumScore, item.IsEnabled)));
    }

    private static async Task<IResult> GetRule(Guid id, SagasDbContext db, CancellationToken cancellationToken = default)
    {
        LearningRequirementRule? rule = await db.LearningRequirementRules.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return rule is null ? Results.NotFound() : Results.Ok(new LearningRequirementRuleResponse(rule.Id, rule.RequirementDefinitionId, rule.CourseId, rule.SatisfactionMode, rule.MinimumScore, rule.IsEnabled));
    }

    private static async Task<IResult> CreateRule([FromBody] CreateLearningRequirementRuleRequest request, LearningRequirementAutomationService service, CancellationToken cancellationToken = default)
    {
        Result<LearningRequirementRule, string> result = await service.CreateRuleAsync(request.RequirementDefinitionId, request.CourseId, request.SatisfactionMode, request.MinimumScore, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/sagas/learning-requirements/course-rules/{item.Id}", new LearningRequirementRuleResponse(item.Id, item.RequirementDefinitionId, item.CourseId, item.SatisfactionMode, item.MinimumScore, item.IsEnabled)), error => Results.Problem(error, statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<IResult> UpdateRule(Guid id, [FromBody] UpdateLearningRequirementRuleRequest request, LearningRequirementAutomationService service, CancellationToken cancellationToken = default)
    {
        Result<LearningRequirementRule, string> result = await service.UpdateRuleAsync(id, request.RequirementDefinitionId, request.CourseId, request.SatisfactionMode, request.MinimumScore, cancellationToken);
        return result.Match<IResult>(item => Results.Ok(new LearningRequirementRuleResponse(item.Id, item.RequirementDefinitionId, item.CourseId, item.SatisfactionMode, item.MinimumScore, item.IsEnabled)), error => error == "Rule not found." ? Results.NotFound() : Results.Problem(error, statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<IResult> SetRuleEnabled(Guid id, [FromBody] SetLearningRequirementRuleEnabledRequest request, SagasDbContext db, LearningRequirementAutomationService service, CancellationToken cancellationToken = default)
    {
        LearningRequirementRule? rule = await db.LearningRequirementRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null)
            return Results.NotFound();

        await service.ToggleRuleAsync(id, request.IsEnabled, cancellationToken);
        rule = await db.LearningRequirementRules.AsNoTracking().SingleAsync(item => item.Id == id, cancellationToken);
        return Results.Ok(new LearningRequirementRuleResponse(rule.Id, rule.RequirementDefinitionId, rule.CourseId, rule.SatisfactionMode, rule.MinimumScore, rule.IsEnabled));
    }

    private static async Task<IResult> DeleteRule(Guid id, LearningRequirementAutomationService service, CancellationToken cancellationToken = default)
    {
        bool deleted = await service.DeleteRuleAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
