using System.Text.Json;
using Fabric.Server.Automation.Kiosk;
using Fabric.Server.Automation;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Kiosk;
using Fabric.Server.Kiosk.Application;
using Fabric.Server.Kiosk.Domain;
using Fabric.Server.Kiosk.Persistence;
using Microsoft.EntityFrameworkCore;
using Elsa.Workflows.Runtime;

namespace Fabric.Server.Sagas.Kiosk;

public sealed class KioskSagaService(
    SagasDbContext db,
    KioskDbContext kioskDb,
    KioskInstructionService instructionService,
    KioskWorkflowResumer workflowResumer,
    AutomationTenantScopeRunner tenantScopeRunner,
    ITenantContext tenantContext,
    TimeProvider timeProvider,
    ILogger<KioskSagaService> logger)
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);

    public async Task StartAsync(Guid sessionId, string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        KioskSaga saga = await GetOrCreateSagaAsync(sessionId, workflowInstanceId, cancellationToken);
        saga.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        KioskSaga? saga = await db.KioskSagas.SingleOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
        if (saga is null)
            return;

        await db.KioskSagaEvents.Where(x => x.SagaId == saga.Id).ExecuteDeleteAsync(cancellationToken);
        db.KioskSagas.Remove(saga);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<KioskSession> CancelSessionAsync(Guid sessionId, KioskSessionCancellationSource source, CancellationToken cancellationToken = default)
    {
        KioskSession session = await kioskDb.Sessions.SingleAsync(x => x.Id == sessionId, cancellationToken);
        if (session.Status is KioskSessionStatus.Cancelled or KioskSessionStatus.Completed or KioskSessionStatus.Failed or KioskSessionStatus.TimedOut)
            return session;

        DateTimeOffset now = timeProvider.GetUtcNow();
        TerminalDisplay terminalDisplay = GetCancellationTerminalDisplay(source);
        session.Cancel(now, terminalDisplay.Title, terminalDisplay.Message);
        session.ClearInstruction(now);
        await kioskDb.SaveChangesAsync(cancellationToken);
        await CleanupAsync(session.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(session.WorkflowInstanceId))
        {
            try
            {
                await tenantScopeRunner.RunInTenantScopeAsync(tenantContext.TenantId, async (serviceProvider, innerCancellationToken) =>
                {
                    IWorkflowRuntime workflowRuntime = serviceProvider.GetRequiredService<IWorkflowRuntime>();
                    var client = await workflowRuntime.CreateClientAsync(session.WorkflowInstanceId, innerCancellationToken);
                    await client.CancelAsync(innerCancellationToken);
                }, cancellationToken);
            }
            catch (Exception exception)
            {
                KioskSagaServiceLog.WorkflowCancelFailed(logger, exception, session.WorkflowInstanceId, session.Id);
            }
        }

        return session;
    }

    public async Task HandleWorkflowFinishedAsync(Guid sessionId, CancellationToken cancellationToken = default) => await SetSessionTerminalStateAsync(sessionId, KioskSessionStatus.Completed, cancellationToken);

    public async Task HandleWorkflowCancelledAsync(Guid sessionId, CancellationToken cancellationToken = default) => await SetSessionTerminalStateAsync(sessionId, KioskSessionStatus.Cancelled, GetCancellationTerminalDisplay(KioskSessionCancellationSource.WorkflowCancelled), cancellationToken);

    public async Task HandleWorkflowFaultedAsync(Guid sessionId, string? detail, CancellationToken cancellationToken = default)
    {
        KioskSession? session = await kioskDb.Sessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
            return;

        TerminalDisplay terminalDisplay = await BuildFailureTerminalDisplayAsync(session, "Workflow error", detail, cancellationToken);
        await SetSessionTerminalStateAsync(session, KioskSessionStatus.Failed, terminalDisplay, cancellationToken);
    }

    public async Task<KioskInstructionBookmark> ScheduleInstructionAsync(Guid sessionId, string workflowInstanceId, KioskInstructionDefinition definition, CancellationToken cancellationToken)
    {
        KioskSaga saga = await GetOrCreateSagaAsync(sessionId, workflowInstanceId, cancellationToken);
        KioskInstructionBookmark bookmark = await instructionService.ShowInstructionAsync(saga.SessionId, definition, cancellationToken);
        saga.CurrentInstructionId = bookmark.InstructionId;
        saga.CurrentInstructionKind = bookmark.Kind;
        saga.State = KioskSagaState.InstructionScheduled;
        saga.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return bookmark;
    }

    public async Task<IReadOnlyList<KioskSagaEventWorkItem>> GetDueEventWorkItemsAsync(int take, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return await db.KioskSagaEvents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null)
            .Where(x => x.NextRetryAt == null || x.NextRetryAt <= now)
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .Select(x => new KioskSagaEventWorkItem(EF.Property<string>(x, TenantDbContext.TenantIdPropertyName), x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task ProcessEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        KioskSagaEvent? sagaEvent = await db.KioskSagaEvents.SingleOrDefaultAsync(x => x.Id == eventId && x.ProcessedAt == null, cancellationToken);
        if (sagaEvent is null)
            return;

        try
        {
            KioskSaga? saga = await db.KioskSagas.SingleOrDefaultAsync(x => x.Id == sagaEvent.SagaId, cancellationToken);
            if (saga is null)
            {
                sagaEvent.MarkProcessed(timeProvider.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                KioskSagaServiceLog.StaleEventIgnored(logger, sagaEvent.Id, sagaEvent.SagaId, "saga");
                return;
            }

            KioskSession? session = await kioskDb.Sessions.SingleOrDefaultAsync(x => x.Id == saga.SessionId, cancellationToken);
            if (session is null)
            {
                sagaEvent.MarkProcessed(timeProvider.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                KioskSagaServiceLog.StaleEventIgnored(logger, sagaEvent.Id, saga.Id, "session");
                return;
            }

            switch (sagaEvent.Type)
            {
                case KioskSagaEventType.InstructionCompleted:
                    await workflowResumer.ResumeInstructionAsync(session, sagaEvent.InstructionId, sagaEvent.InstructionKind, DeserializeResult(sagaEvent.InstructionKind, sagaEvent.ResultJson), cancellationToken);
                    saga.State = KioskSagaState.Active;
                    break;
                case KioskSagaEventType.InstructionCancelled:
                    await workflowResumer.CancelInstructionAsync(session, sagaEvent.InstructionId, sagaEvent.InstructionKind, cancellationToken);
                    saga.State = KioskSagaState.Cancelled;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported kiosk saga event type '{sagaEvent.Type}'.");
            }

            saga.CurrentInstructionId = null;
            saga.CurrentInstructionKind = null;
            saga.UpdatedAt = timeProvider.GetUtcNow();
            sagaEvent.MarkProcessed(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return;
        }
        catch (Exception exception)
        {
            sagaEvent.ScheduleRetry(timeProvider.GetUtcNow().Add(RetryInterval), exception.Message);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return;
            }

            throw;
        }
    }

    private static KioskInstructionResult DeserializeResult(KioskInstructionActivityKind kind, string? resultJson) => kind switch
    {
        KioskInstructionActivityKind.Choice when resultJson is not null => JsonSerializer.Deserialize(resultJson, KioskJsonSerializerContext.Default.KioskChoiceInstructionResult) ?? throw new InvalidOperationException("Choice instruction result is invalid."),
        KioskInstructionActivityKind.Form when resultJson is not null => JsonSerializer.Deserialize(resultJson, KioskJsonSerializerContext.Default.KioskFormInstructionResult) ?? throw new InvalidOperationException("Form instruction result is invalid."),
        _ => throw new InvalidOperationException($"Unsupported kiosk instruction result kind '{kind}'.")
    };

    private async Task<KioskSaga> GetOrCreateSagaAsync(Guid sessionId, string workflowInstanceId, CancellationToken cancellationToken)
    {
        KioskSaga? saga = await db.KioskSagas.SingleOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);

        if (saga is null)
        {
            saga = CreateSaga(sessionId, workflowInstanceId);
            db.KioskSagas.Add(saga);
        }

        if (!string.Equals(saga.WorkflowInstanceId, workflowInstanceId, StringComparison.Ordinal))
            saga.WorkflowInstanceId = workflowInstanceId;

        return saga;
    }

    private KioskSaga CreateSaga(Guid sessionId, string workflowInstanceId)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return new KioskSaga
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            WorkflowInstanceId = workflowInstanceId,
            State = KioskSagaState.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private async Task SetSessionTerminalStateAsync(Guid sessionId, KioskSessionStatus status, CancellationToken cancellationToken)
    {
        KioskSession? session = await kioskDb.Sessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
            return;

        await SetSessionTerminalStateAsync(session, status, null, cancellationToken);
    }

    private async Task SetSessionTerminalStateAsync(Guid sessionId, KioskSessionStatus status, TerminalDisplay terminalDisplay, CancellationToken cancellationToken)
    {
        KioskSession? session = await kioskDb.Sessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
            return;

        await SetSessionTerminalStateAsync(session, status, terminalDisplay, cancellationToken);
    }

    private async Task SetSessionTerminalStateAsync(KioskSession session, KioskSessionStatus status, TerminalDisplay? terminalDisplay, CancellationToken cancellationToken)
    {
        Guid sessionId = session.Id;

        if (session.Status is KioskSessionStatus.Cancelled or KioskSessionStatus.Completed or KioskSessionStatus.Failed or KioskSessionStatus.TimedOut)
        {
            await CleanupAsync(sessionId, cancellationToken);
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        switch (status)
        {
            case KioskSessionStatus.Completed:
                session.MarkCompleted(now, terminalDisplay?.Title, terminalDisplay?.Message);
                break;
            case KioskSessionStatus.Cancelled:
                session.Cancel(now, terminalDisplay?.Title, terminalDisplay?.Message);
                break;
            case KioskSessionStatus.Failed:
                session.Fail(now, terminalDisplay?.Title, terminalDisplay?.Message);
                break;
            case KioskSessionStatus.TimedOut:
                session.Timeout(now, terminalDisplay?.Title, terminalDisplay?.Message);
                break;
            default:
                throw new InvalidOperationException($"Unsupported terminal kiosk session status '{status}'.");
        }

        session.ClearInstruction(now);
        await kioskDb.SaveChangesAsync(cancellationToken);
        await CleanupAsync(sessionId, cancellationToken);
    }

    private async Task<TerminalDisplay> BuildFailureTerminalDisplayAsync(KioskSession session, string title, string? detail, CancellationToken cancellationToken)
    {
        global::Fabric.Server.Kiosk.Domain.Kiosk kiosk = await kioskDb.Kiosks.AsNoTracking().SingleAsync(x => x.Id == session.KioskId, cancellationToken);
        string message = kiosk.ShowDetailedErrors && !string.IsNullOrWhiteSpace(detail)
            ? detail!.Trim()
            : "Something went wrong. Please contact guard.";
        return new TerminalDisplay(title, message);
    }

    private static TerminalDisplay GetCancellationTerminalDisplay(KioskSessionCancellationSource source) => source switch
    {
        KioskSessionCancellationSource.UserHome => new(null, null),
        KioskSessionCancellationSource.Guard => new("Session cancelled", "Session was cancelled by guard."),
        KioskSessionCancellationSource.Maintenance => new("Session cancelled", "Kiosk is temporarily unavailable."),
        KioskSessionCancellationSource.Disabled => new("Session cancelled", "Kiosk is unavailable."),
        KioskSessionCancellationSource.WorkflowCancelled => new("Session cancelled", "Session was cancelled."),
        _ => new("Session cancelled", "Session was cancelled.")
    };

    private sealed record TerminalDisplay(string? Title, string? Message);
}

internal static partial class KioskSagaServiceLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring stale kiosk saga event {EventId} because {MissingEntity} no longer exists for saga {SagaId}")]
    public static partial void StaleEventIgnored(this ILogger logger, Guid eventId, Guid sagaId, string missingEntity);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not cancel workflow instance {WorkflowInstanceId} for kiosk session {SessionId}")]
    public static partial void WorkflowCancelFailed(this ILogger logger, Exception exception, string workflowInstanceId, Guid sessionId);
}
