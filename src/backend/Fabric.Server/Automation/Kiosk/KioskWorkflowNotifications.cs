using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Notifications;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Kiosk.Persistence;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.Kiosk;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Automation.Kiosk;

public sealed class KioskWorkflowFinishedHandler(KioskDbContext kioskDb, SagasDbContext sagasDb, AutomationTenantScopeRunner tenantScopeRunner) : INotificationHandler<WorkflowFinished>
{
    public async Task HandleAsync(WorkflowFinished notification, CancellationToken cancellationToken)
    {
        Guid? sessionId = GetSessionId(notification.WorkflowExecutionContext.CorrelationId, notification.WorkflowState.CorrelationId);
        if (sessionId is null)
            return;

        string? tenantId = await ResolveTenantIdAsync(sessionId.Value, notification.WorkflowExecutionContext.Id, notification.WorkflowState.Id, cancellationToken);
        if (string.IsNullOrWhiteSpace(tenantId))
            return;

        await tenantScopeRunner.RunInTenantScopeAsync(tenantId, async (serviceProvider, innerCancellationToken) =>
        {
            KioskSagaService sagaService = serviceProvider.GetRequiredService<KioskSagaService>();

            switch (notification.WorkflowState.SubStatus)
            {
                case WorkflowSubStatus.Faulted:
                    await sagaService.HandleWorkflowFaultedAsync(sessionId.Value, GetIncidentMessage(notification.WorkflowState.Incidents.LastOrDefault()), innerCancellationToken);
                    break;
                case WorkflowSubStatus.Cancelled:
                    await sagaService.HandleWorkflowCancelledAsync(sessionId.Value, innerCancellationToken);
                    break;
                default:
                    await sagaService.HandleWorkflowFinishedAsync(sessionId.Value, innerCancellationToken);
                    break;
            }
        }, cancellationToken);
    }

    private async Task<string?> ResolveTenantIdAsync(Guid sessionId, string workflowExecutionContextId, string workflowStateId, CancellationToken cancellationToken)
    {
        string? tenantId = await kioskDb.Sessions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(session => session.Id == sessionId)
            .Select(session => EF.Property<string>(session, TenantDbContext.TenantIdPropertyName))
            .SingleOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(tenantId))
            return tenantId;

        string workflowInstanceId = !string.IsNullOrWhiteSpace(workflowExecutionContextId)
            ? workflowExecutionContextId
            : workflowStateId;

        if (string.IsNullOrWhiteSpace(workflowInstanceId))
            return null;

        return await sagasDb.KioskSagas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(saga => saga.WorkflowInstanceId == workflowInstanceId)
            .Select(saga => EF.Property<string>(saga, TenantDbContext.TenantIdPropertyName))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static string? GetIncidentMessage(ActivityIncident? incident)
        => !string.IsNullOrWhiteSpace(incident?.Message)
            ? incident.Message.Trim()
            : incident?.Exception?.Message;

    private static Guid? GetSessionId(string? workflowExecutionContextCorrelationId, string? workflowStateCorrelationId)
    {
        if (Guid.TryParse(workflowExecutionContextCorrelationId, out Guid sessionId))
            return sessionId;
        if (Guid.TryParse(workflowStateCorrelationId, out sessionId))
            return sessionId;
        return null;
    }
}
