using Elsa.Common.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Kiosk.Domain;
using Fabric.Server.Kiosk.Persistence;

namespace Fabric.Server.Automation.Kiosk;

public sealed class KioskWorkflowStarter(AutomationTenantScopeRunner tenantScopeRunner, ITenantContext tenantContext, KioskDbContext db, TimeProvider timeProvider, ILogger<KioskWorkflowStarter> logger)
{
    public async Task StartSessionWorkflowAsync(Server.Kiosk.Domain.Kiosk kiosk, KioskSession session, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(kiosk.WorkflowDefinitionId))
            throw new InvalidOperationException("Kiosk has no assigned workflow definition.");

        logger.StartingWorkflow(kiosk.Id, session.Id, kiosk.WorkflowDefinitionId);

        try
        {
            string tenantId = tenantContext.TenantId;
            RunWorkflowInstanceResponse result = await tenantScopeRunner.RunInTenantScopeAsync(tenantId, async (serviceProvider, innerCancellationToken) =>
            {
                IWorkflowRuntime workflowRuntime = serviceProvider.GetRequiredService<IWorkflowRuntime>();
                IWorkflowClient client = await workflowRuntime.CreateClientAsync(innerCancellationToken);
                return await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
                {
                    WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(kiosk.WorkflowDefinitionId, VersionOptions.Published),
                    CorrelationId = session.Id.ToString("N"),
                    Input = new Dictionary<string, object>
                    {
                        [KioskWorkflowContext.TenantIdInputName] = tenantId,
                    [KioskWorkflowContext.KioskIdInputName] = kiosk.Id,
                    [KioskWorkflowContext.SessionIdInputName] = session.Id,
                    [KioskWorkflowContext.ProfileIdInputName] = kiosk.ProfileId,
                    [KioskWorkflowContext.LanguageCodeInputName] = session.LanguageCode
                    }
                }, innerCancellationToken);
            }, cancellationToken);

            DateTimeOffset now = timeProvider.GetUtcNow();
            session.AssignWorkflowInstance(result.WorkflowInstanceId, now);
            session.MarkRunning(now);
            await db.SaveChangesAsync(cancellationToken);
            logger.WorkflowStarted(kiosk.Id, session.Id, result.WorkflowInstanceId);
        }
        catch (Exception exception)
        {
            logger.WorkflowStartFailed(exception, kiosk.Id, session.Id, kiosk.WorkflowDefinitionId);
            throw;
        }
    }
}

internal static partial class KioskWorkflowStarterLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting kiosk workflow for kiosk {KioskId}, session {SessionId}, definition {WorkflowDefinitionId}")]
    public static partial void StartingWorkflow(this ILogger logger, Guid kioskId, Guid sessionId, string workflowDefinitionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Started kiosk workflow for kiosk {KioskId}, session {SessionId}, workflow instance {WorkflowInstanceId}")]
    public static partial void WorkflowStarted(this ILogger logger, Guid kioskId, Guid sessionId, string workflowInstanceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start kiosk workflow for kiosk {KioskId}, session {SessionId}, definition {WorkflowDefinitionId}")]
    public static partial void WorkflowStartFailed(this ILogger logger, Exception exception, Guid kioskId, Guid sessionId, string workflowDefinitionId);
}
