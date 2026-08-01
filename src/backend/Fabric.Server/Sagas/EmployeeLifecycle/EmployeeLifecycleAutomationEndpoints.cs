using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.EmployeeLifecycle;

public sealed record EmployeeLifecycleAutomationSettingsResponse(
    bool IsEnabled,
    bool DisableEmployeeOnLeave,
    DateTimeOffset? DisabledAt,
    DateTimeOffset? ReenabledAt,
    DateTimeOffset? LastFullReconciledAt);

public sealed record UpdateEmployeeLifecycleAutomationSettingsRequest(bool IsEnabled, bool DisableEmployeeOnLeave);

public sealed record OrganizationalUnitPackageRuleResponse(Guid Id, Guid OrganizationUnitId, Guid PackageId, bool IsEnabled);
public sealed record PersonaPackageRuleResponse(Guid Id, Guid PersonaId, Guid PackageId, bool IsEnabled);
public sealed record CreateOrganizationalUnitPackageRuleRequest(Guid OrganizationUnitId, Guid PackageId);
public sealed record CreatePersonaPackageRuleRequest(Guid PersonaId, Guid PackageId);
public sealed record SetRuleEnabledRequest(bool IsEnabled);

public static class EmployeeLifecycleAutomationEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeLifecycleAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/sagas/employee-lifecycle");

        group.MapGet("/settings", GetSettings).Produces<EmployeeLifecycleAutomationSettingsResponse>();
        group.MapPut("/settings", UpdateSettings).Produces<EmployeeLifecycleAutomationSettingsResponse>();
        group.MapGet("/ou-package-rules", ListOuRules).Produces<Page<OrganizationalUnitPackageRuleResponse>>();
        group.MapPost("/ou-package-rules", CreateOuRule).Produces<OrganizationalUnitPackageRuleResponse>(StatusCodes.Status201Created);
        group.MapPut("/ou-package-rules/{id:guid}/enabled", SetOuRuleEnabled).Produces<OrganizationalUnitPackageRuleResponse>();
        group.MapDelete("/ou-package-rules/{id:guid}", DeleteOuRule).Produces(StatusCodes.Status204NoContent);
        group.MapGet("/persona-package-rules", ListPersonaRules).Produces<Page<PersonaPackageRuleResponse>>();
        group.MapPost("/persona-package-rules", CreatePersonaRule).Produces<PersonaPackageRuleResponse>(StatusCodes.Status201Created);
        group.MapPut("/persona-package-rules/{id:guid}/enabled", SetPersonaRuleEnabled).Produces<PersonaPackageRuleResponse>();
        group.MapDelete("/persona-package-rules/{id:guid}", DeletePersonaRule).Produces(StatusCodes.Status204NoContent);
        group.MapPost("/reconcile/{employeeId:guid}", EnqueueReconcile).Produces(StatusCodes.Status202Accepted);

        return app;
    }

    private static async Task<IResult> GetSettings(EmployeeLifecycleAutomationService service, CancellationToken cancellationToken = default)
    {
        EmployeeLifecycleAutomationSettings settings = await service.GetSettingsAsync(cancellationToken);
        return Results.Ok(ToResponse(settings));
    }

    private static async Task<IResult> UpdateSettings([FromBody] UpdateEmployeeLifecycleAutomationSettingsRequest request, EmployeeLifecycleAutomationService service, CancellationToken cancellationToken = default)
    {
        EmployeeLifecycleAutomationSettings settings = await service.UpdateSettingsAsync(request.IsEnabled, request.DisableEmployeeOnLeave, cancellationToken);
        return Results.Ok(ToResponse(settings));
    }

    private static async Task<IResult> ListOuRules([AsParameters] BaseListRequest request, SagasDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<OrganizationalUnitPackageRule> result = await db.OrganizationalUnitPackageRules.AsNoTracking().OrderBy(item => item.OrganizationUnitId).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => new OrganizationalUnitPackageRuleResponse(item.Id, item.OrganizationUnitId, item.PackageId, item.IsEnabled)));
    }

    private static async Task<IResult> CreateOuRule([FromBody] CreateOrganizationalUnitPackageRuleRequest request, EmployeeLifecycleAutomationService service, CancellationToken cancellationToken = default)
    {
        Result<OrganizationalUnitPackageRule, string> result = await service.CreateOuRuleAsync(request.OrganizationUnitId, request.PackageId, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/sagas/employee-lifecycle/ou-package-rules/{item.Id}", new OrganizationalUnitPackageRuleResponse(item.Id, item.OrganizationUnitId, item.PackageId, item.IsEnabled)), error => Results.Problem(error, statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<IResult> SetOuRuleEnabled(Guid id, [FromBody] SetRuleEnabledRequest request, SagasDbContext db, EmployeeLifecycleAutomationService service, CancellationToken cancellationToken = default)
    {
        OrganizationalUnitPackageRule? rule = await db.OrganizationalUnitPackageRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null)
            return Results.NotFound();
        await service.ToggleOuRuleAsync(id, request.IsEnabled, cancellationToken);
        rule = await db.OrganizationalUnitPackageRules.AsNoTracking().SingleAsync(item => item.Id == id, cancellationToken);
        return Results.Ok(new OrganizationalUnitPackageRuleResponse(rule.Id, rule.OrganizationUnitId, rule.PackageId, rule.IsEnabled));
    }

    private static async Task<IResult> DeleteOuRule(Guid id, EmployeeLifecycleAutomationService service, CancellationToken cancellationToken = default)
    {
        bool deleted = await service.DeleteOuRuleAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ListPersonaRules([AsParameters] BaseListRequest request, SagasDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<PersonaPackageRule> result = await db.PersonaPackageRules.AsNoTracking().OrderBy(item => item.PersonaId).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => new PersonaPackageRuleResponse(item.Id, item.PersonaId, item.PackageId, item.IsEnabled)));
    }

    private static async Task<IResult> CreatePersonaRule([FromBody] CreatePersonaPackageRuleRequest request, EmployeeLifecycleAutomationService service, CancellationToken cancellationToken = default)
    {
        Result<PersonaPackageRule, string> result = await service.CreatePersonaRuleAsync(request.PersonaId, request.PackageId, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/sagas/employee-lifecycle/persona-package-rules/{item.Id}", new PersonaPackageRuleResponse(item.Id, item.PersonaId, item.PackageId, item.IsEnabled)), error => Results.Problem(error, statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<IResult> SetPersonaRuleEnabled(Guid id, [FromBody] SetRuleEnabledRequest request, SagasDbContext db, EmployeeLifecycleAutomationService service, CancellationToken cancellationToken = default)
    {
        PersonaPackageRule? rule = await db.PersonaPackageRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null)
            return Results.NotFound();
        await service.TogglePersonaRuleAsync(id, request.IsEnabled, cancellationToken);
        rule = await db.PersonaPackageRules.AsNoTracking().SingleAsync(item => item.Id == id, cancellationToken);
        return Results.Ok(new PersonaPackageRuleResponse(rule.Id, rule.PersonaId, rule.PackageId, rule.IsEnabled));
    }

    private static async Task<IResult> DeletePersonaRule(Guid id, EmployeeLifecycleAutomationService service, CancellationToken cancellationToken = default)
    {
        bool deleted = await service.DeletePersonaRuleAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> EnqueueReconcile(Guid employeeId, EmployeeLifecycleAutomationService service, CancellationToken cancellationToken = default)
    {
        await service.EnqueueAsync(employeeId, "ManualReconcile", cancellationToken);
        return Results.Accepted();
    }

    private static EmployeeLifecycleAutomationSettingsResponse ToResponse(EmployeeLifecycleAutomationSettings settings) =>
        new(settings.IsEnabled, settings.DisableEmployeeOnLeave, settings.DisabledAt, settings.ReenabledAt, settings.LastFullReconciledAt);
}
