using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Locations.Application;
using Fabric.Server.Locations.Domain;
using Fabric.Server.Notifications.Services;
using Fabric.Server.Reception.Application;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Visitors.Application;
using Fabric.Server.Visitors.Domain;
using Fabric.Server.Visitors.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.VisitorPreOnboarding;

public enum SagaStepResult
{
    Continue,
    Retry,
    Fail,
}

public class VisitorPreOnboardingSagaService(SagasDbContext db, VisitorsDbContext visitorsDb,
        ReceptionService receptionService,
        VisitService visitService,
        LocationService locationService,
        EmailNotificationSender emailNotificationSender,
        TenantBaseUrlResolver tenantBaseUrlResolver,
        VisitorPreOnboardingSagaTrigger trigger,
        IWebHostEnvironment webHostEnvironment,
        TimeProvider timeProvider)
{

    private static readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(10);
    private const string InvitationTemplate = "invitation.html";
    private const string ConfirmationTemplate = "confirmation-to-organizer.html";
    private const string CancellationTemplate = "cancellation.html";
    private const string RescheduleTemplate = "reschedule.html";
    private const string RelocationTemplate = "relocation.html";
    private const string ArrivalTemplate = "arrival-to-organizer.html";
    private const string InvitationSubject = "You're invited to a visit";
    private const string ConfirmationSubject = "Visitor confirmed participation";
    private const string CancellationSubject = "Your visit has been cancelled";
    private const string RescheduleSubject = "Your visit has been rescheduled";
    private const string RelocationSubject = "Your visit location has changed";
    private const string ArrivalSubject = "Visitor has arrived";

    public async Task<VisitorPreOnboardingSagaConfig> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        VisitorPreOnboardingSagaConfig? config = await db.VisitorPreOnboardingSagaConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        return config ?? VisitorPreOnboardingSagaConfig.Default;
    }

    public async Task<VisitorPreOnboardingSagaConfig> UpdateConfigurationAsync(VisitorPreOnboardingSagaConfig config, CancellationToken cancellationToken = default)
    {
        VisitorPreOnboardingSagaConfig? existing = await db.VisitorPreOnboardingSagaConfigs
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            existing = new VisitorPreOnboardingSagaConfig { Id = Guid.NewGuid() };
            db.VisitorPreOnboardingSagaConfigs.Add(existing);
        }

        existing.UseCustomInviteNotification = config.UseCustomInviteNotification;
        existing.CustomInviteNotification = config.CustomInviteNotification;
        existing.QrGenerationMode = config.QrGenerationMode;
        existing.SystemId = config.SystemId;
        existing.BadgeTypeId = config.BadgeTypeId;
        existing.SendConfirmNotificationToOrganizer = config.SendConfirmNotificationToOrganizer;
        existing.UseCustomConfirmNotification = config.UseCustomConfirmNotification;
        existing.CustomConfirmNotification = config.CustomConfirmNotification;
        existing.SendCancellationNotification = config.SendCancellationNotification;
        existing.UseCustomCancellationNotification = config.UseCustomCancellationNotification;
        existing.CustomCancellationNotification = config.CustomCancellationNotification;
        existing.SendRescheduleNotification = config.SendRescheduleNotification;
        existing.UseCustomRescheduleNotification = config.UseCustomRescheduleNotification;
        existing.CustomRescheduleNotification = config.CustomRescheduleNotification;
        existing.SendRelocationNotification = config.SendRelocationNotification;
        existing.UseCustomRelocationNotification = config.UseCustomRelocationNotification;
        existing.CustomRelocationNotification = config.CustomRelocationNotification;
        existing.SendArrivalNotificationToOrganizer = config.SendArrivalNotificationToOrganizer;
        existing.UseCustomArrivalNotification = config.UseCustomArrivalNotification;
        existing.CustomArrivalNotification = config.CustomArrivalNotification;

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<VisitorPreOnboardingSaga> StartAsync(Guid visitId, Guid invitationId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        VisitorPreOnboardingState[] terminalStates = [VisitorPreOnboardingState.Cancelled, VisitorPreOnboardingState.Expired];
        bool existing = await db.VisitorPreOnboardingSagas
            .AnyAsync(x => x.VisitId == visitId && x.InvitationId == invitationId && !terminalStates.Contains(x.State), cancellationToken);

        if (existing)
            throw new InvalidOperationException($"Saga already exists for visit {visitId} and invitation {invitationId}");

        var saga = new VisitorPreOnboardingSaga
        {
            Id = Guid.NewGuid(),
            VisitId = visitId,
            InvitationId = invitationId,
            CreatedAt = timeProvider.GetUtcNow(),
            ExpiresAt = expiresAt,
            RetryCount = 0,
            State = VisitorPreOnboardingState.RegisteringArrival,
        };

        db.VisitorPreOnboardingSagas.Add(saga);
        db.VisitorPreOnboardingSagaEvents.Add(VisitorPreOnboardingSagaEvent.Create(
            VisitorPreOnboardingSagaEventType.Started,
            timeProvider.GetUtcNow(),
            sagaId: saga.Id));
        await db.SaveChangesAsync(cancellationToken);
        trigger.Notify();
        return saga;
    }

    public async Task EnqueueVisitorConfirmedAsync(Guid visitId, Guid invitationId, CancellationToken cancellationToken = default) =>
        await EnqueueEventAsync(VisitorPreOnboardingSagaEventType.VisitorConfirmed, visitId: visitId, invitationId: invitationId, cancellationToken: cancellationToken);

    public async Task EnqueueVisitorRejectedAsync(Guid visitId, Guid invitationId, CancellationToken cancellationToken = default) =>
        await EnqueueEventAsync(VisitorPreOnboardingSagaEventType.VisitorRejected, visitId: visitId, invitationId: invitationId, cancellationToken: cancellationToken);

    public async Task EnqueueVisitCancelledAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        await EnqueueEventAsync(VisitorPreOnboardingSagaEventType.VisitCancelled, visitId: visitId, cancellationToken: cancellationToken);

    public async Task EnqueueVisitRescheduledAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        await EnqueueEventAsync(VisitorPreOnboardingSagaEventType.VisitRescheduled, visitId: visitId, cancellationToken: cancellationToken);

    public async Task EnqueueVisitRelocatedAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        await EnqueueEventAsync(VisitorPreOnboardingSagaEventType.VisitRelocated, visitId: visitId, cancellationToken: cancellationToken);

    public async Task EnqueueVisitorArrivedAsync(Guid arrivalId, CancellationToken cancellationToken = default) =>
        await EnqueueEventAsync(VisitorPreOnboardingSagaEventType.VisitorArrived, arrivalId: arrivalId, cancellationToken: cancellationToken);

    private async Task EnqueueEventAsync(
        VisitorPreOnboardingSagaEventType type,
        Guid? sagaId = null,
        Guid? visitId = null,
        Guid? invitationId = null,
        Guid? arrivalId = null,
        CancellationToken cancellationToken = default)
    {
        db.VisitorPreOnboardingSagaEvents.Add(VisitorPreOnboardingSagaEvent.Create(
            type,
            timeProvider.GetUtcNow(),
            sagaId,
            visitId,
            invitationId,
            arrivalId));
        await db.SaveChangesAsync(cancellationToken);
        trigger.Notify();
    }

    public async Task ConfirmAsync(Visitor visitor, Guid visitId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        VisitorPreOnboardingSaga? saga = await db.VisitorPreOnboardingSagas
        .Where(x => x.VisitId == visitId && x.InvitationId == invitationId)
        .SingleOrDefaultAsync(cancellationToken);

        if (saga is null || saga.State is VisitorPreOnboardingState.Confirmed or VisitorPreOnboardingState.Cancelled or VisitorPreOnboardingState.Expired)
            return;

        if (saga.ArrivalId.HasValue)
            _ = await receptionService.ConfirmVisitor(visitor.FirstName, visitor.LastName, visitor.Company, saga.ArrivalId.Value, cancellationToken);

        saga.State = VisitorPreOnboardingState.Confirmed;
        await db.SaveChangesAsync(cancellationToken);

        VisitorPreOnboardingSagaConfig config = await GetConfigurationAsync(cancellationToken);
        if (config.SendConfirmNotificationToOrganizer)
            await SendConfirmationToOrganizerAsync(config, saga, cancellationToken);
    }

    public async Task RejectAsync(Guid visitId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        VisitorPreOnboardingSaga? saga = await db.VisitorPreOnboardingSagas
        .Where(x => x.VisitId == visitId && x.InvitationId == invitationId)
        .SingleOrDefaultAsync(cancellationToken);

        if (saga is null || saga.State is VisitorPreOnboardingState.Rejected or VisitorPreOnboardingState.Cancelled or VisitorPreOnboardingState.Expired)
            return;

        if (saga.ArrivalId.HasValue)
            _ = await receptionService.RejectConfirmationVisitor(saga.ArrivalId.Value, cancellationToken);

        saga.State = VisitorPreOnboardingState.Rejected;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task VisitRescheduled(Guid visitId, DateTimeOffset startDate, DateTimeOffset stopDate, CancellationToken cancellationToken = default)
    {
        List<VisitorPreOnboardingSaga> sagas = await db.VisitorPreOnboardingSagas
      .Where(x => x.VisitId == visitId)
      .ToListAsync(cancellationToken);

        foreach (VisitorPreOnboardingSaga? saga in sagas)
        {
            saga.ExpiresAt = startDate;

            if (saga.ArrivalId.HasValue)
                _ = await receptionService.Reschedule(saga.ArrivalId.Value, startDate, stopDate, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        VisitorPreOnboardingSagaConfig config = await GetConfigurationAsync(cancellationToken);
        if (!config.SendRescheduleNotification)
            return;

        foreach (VisitorPreOnboardingSaga saga in sagas)
            await SendVisitorNotificationAsync(saga.VisitId, saga.InvitationId, saga.QrCode, RescheduleTemplate, config.UseCustomRescheduleNotification, config.CustomRescheduleNotification, RescheduleSubject, cancellationToken);

    }

    public async Task VisitRelocated(Guid visitId, Guid locationId, CancellationToken cancellationToken = default)
    {
        List<VisitorPreOnboardingSaga> sagas = await db.VisitorPreOnboardingSagas
            .Where(x => x.VisitId == visitId)
            .ToListAsync(cancellationToken);

        foreach (VisitorPreOnboardingSaga saga in sagas)
        {
            if (saga.ArrivalId.HasValue)
                _ = await receptionService.Relocate(saga.ArrivalId.Value, locationId, cancellationToken);
        }

        VisitorPreOnboardingSagaConfig config = await GetConfigurationAsync(cancellationToken);
        if (!config.SendRelocationNotification)
            return;

        foreach (VisitorPreOnboardingSaga saga in sagas)
            await SendVisitorNotificationAsync(saga.VisitId, saga.InvitationId, saga.QrCode, RelocationTemplate, config.UseCustomRelocationNotification, config.CustomRelocationNotification, RelocationSubject, cancellationToken);
    }

    public async Task CancelForVisitAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        List<VisitorPreOnboardingSaga> sagas = await db.VisitorPreOnboardingSagas
        .Where(x => x.VisitId == visitId)
        .Where(x => x.State != VisitorPreOnboardingState.Cancelled
                 && x.State != VisitorPreOnboardingState.Expired)
        .ToListAsync(cancellationToken);

        foreach (VisitorPreOnboardingSaga? saga in sagas)
        {
            saga.State = VisitorPreOnboardingState.Cancelling;
            saga.RetryCount = 0;
            saga.NextRetryAt = null;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (VisitorPreOnboardingSaga saga in sagas)
            await ProcessAsync(saga, cancellationToken);
    }

    public async Task RetryAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        VisitorPreOnboardingSaga saga = await db.VisitorPreOnboardingSagas
        .Where(x => x.Id == sagaId)
        .Where(x => x.State == VisitorPreOnboardingState.Expired)
        .SingleAsync(cancellationToken);

        if (timeProvider.GetUtcNow() > saga.ExpiresAt)
            throw new InvalidOperationException($"Saga {sagaId} has expired and cannot be retried (visit date has passed).");

        saga.RetryCount = 0;
        saga.NextRetryAt = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VisitorPreOnboardingSaga>> GetRetryableAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        return await db.VisitorPreOnboardingSagas
            .Where(x => x.State == VisitorPreOnboardingState.RegisteringArrival
                     || x.State == VisitorPreOnboardingState.GeneratingQr
                     || x.State == VisitorPreOnboardingState.UpdatingArrivalQr
                     || x.State == VisitorPreOnboardingState.SendingInvitation
                     || x.State == VisitorPreOnboardingState.Cancelling)
            .Where(x => x.NextRetryAt == null || x.NextRetryAt <= now)
            .Where(x => x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VisitorPreOnboardingSagaWorkItem>> GetRetryableWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        return await db.VisitorPreOnboardingSagas
            .IgnoreQueryFilters()
            .Where(x => x.State == VisitorPreOnboardingState.RegisteringArrival
                     || x.State == VisitorPreOnboardingState.GeneratingQr
                     || x.State == VisitorPreOnboardingState.UpdatingArrivalQr
                     || x.State == VisitorPreOnboardingState.SendingInvitation
                     || x.State == VisitorPreOnboardingState.Cancelling)
            .Where(x => x.NextRetryAt == null || x.NextRetryAt <= now)
            .Where(x => x.ExpiresAt > now)
            .Select(x => new VisitorPreOnboardingSagaWorkItem(
                EF.Property<string>(x, TenantDbContext.TenantIdPropertyName),
                x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ExpirePassedSagasAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        List<VisitorPreOnboardingSaga> expired = await db.VisitorPreOnboardingSagas
        .Where(x => x.State == VisitorPreOnboardingState.RegisteringArrival
                 || x.State == VisitorPreOnboardingState.GeneratingQr
                 || x.State == VisitorPreOnboardingState.UpdatingArrivalQr
                 || x.State == VisitorPreOnboardingState.SendingInvitation
                 || x.State == VisitorPreOnboardingState.AwaitingConfirmation)
        .Where(x => x.ExpiresAt <= now)
        .ToListAsync(cancellationToken);

        int count = 0;
        foreach (VisitorPreOnboardingSaga? saga in expired)
        {
            if (await ExpireSagaAsync(saga, cancellationToken))
                count++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return count;
    }

    public async Task<IReadOnlyList<VisitorPreOnboardingSagaWorkItem>> GetExpiredWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        return await db.VisitorPreOnboardingSagas
            .IgnoreQueryFilters()
            .Where(x => x.State == VisitorPreOnboardingState.RegisteringArrival
                     || x.State == VisitorPreOnboardingState.GeneratingQr
                     || x.State == VisitorPreOnboardingState.UpdatingArrivalQr
                     || x.State == VisitorPreOnboardingState.SendingInvitation
                      || x.State == VisitorPreOnboardingState.AwaitingConfirmation)
            .Where(x => x.ExpiresAt <= now)
            .Select(x => new VisitorPreOnboardingSagaWorkItem(
                EF.Property<string>(x, TenantDbContext.TenantIdPropertyName),
                x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExpireAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        VisitorPreOnboardingSaga? saga = await db.VisitorPreOnboardingSagas
            .Where(x => x.Id == sagaId)
            .Where(x => x.State == VisitorPreOnboardingState.RegisteringArrival
                     || x.State == VisitorPreOnboardingState.GeneratingQr
                     || x.State == VisitorPreOnboardingState.UpdatingArrivalQr
                     || x.State == VisitorPreOnboardingState.SendingInvitation
                     || x.State == VisitorPreOnboardingState.AwaitingConfirmation)
            .Where(x => x.ExpiresAt <= now)
            .SingleOrDefaultAsync(cancellationToken);

        if (saga is null)
            return false;

        if (!await ExpireSagaAsync(saga, cancellationToken))
            return false;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Guid>> GetDueEventIdsAsync(int take, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        return await db.VisitorPreOnboardingSagaEvents
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null)
            .Where(x => x.NextRetryAt == null || x.NextRetryAt <= now)
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VisitorPreOnboardingSagaEventWorkItem>> GetDueEventWorkItemsAsync(int take, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        return await db.VisitorPreOnboardingSagaEvents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null)
            .Where(x => x.NextRetryAt == null || x.NextRetryAt <= now)
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .Select(x => new VisitorPreOnboardingSagaEventWorkItem(
                EF.Property<string>(x, TenantDbContext.TenantIdPropertyName),
                x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task ProcessEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        VisitorPreOnboardingSagaEvent? sagaEvent = await db.VisitorPreOnboardingSagaEvents
            .SingleOrDefaultAsync(x => x.Id == eventId && x.ProcessedAt == null, cancellationToken);

        if (sagaEvent is null)
            return;

        try
        {
            bool processed = await HandleEventAsync(sagaEvent, cancellationToken);
            if (processed)
            {
                sagaEvent.MarkProcessed(timeProvider.GetUtcNow());
            }
            else
            {
                ScheduleEventRetry(sagaEvent, "Event handler could not complete.");
            }
        }
        catch (Exception ex)
        {
            ScheduleEventRetry(sagaEvent, ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HandleEventAsync(VisitorPreOnboardingSagaEvent sagaEvent, CancellationToken cancellationToken) =>
        sagaEvent.Type switch
        {
            VisitorPreOnboardingSagaEventType.Started when sagaEvent.SagaId.HasValue => await HandleStartedEventAsync(sagaEvent.SagaId.Value, cancellationToken),
            VisitorPreOnboardingSagaEventType.VisitorConfirmed when sagaEvent.VisitId.HasValue && sagaEvent.InvitationId.HasValue => await HandleVisitorConfirmedEventAsync(sagaEvent.VisitId.Value, sagaEvent.InvitationId.Value, cancellationToken),
            VisitorPreOnboardingSagaEventType.VisitorRejected when sagaEvent.VisitId.HasValue && sagaEvent.InvitationId.HasValue => await HandleVisitorRejectedEventAsync(sagaEvent.VisitId.Value, sagaEvent.InvitationId.Value, cancellationToken),
            VisitorPreOnboardingSagaEventType.VisitCancelled when sagaEvent.VisitId.HasValue => await HandleVisitCancelledEventAsync(sagaEvent.VisitId.Value, cancellationToken),
            VisitorPreOnboardingSagaEventType.VisitRescheduled when sagaEvent.VisitId.HasValue => await HandleVisitRescheduledEventAsync(sagaEvent.VisitId.Value, cancellationToken),
            VisitorPreOnboardingSagaEventType.VisitRelocated when sagaEvent.VisitId.HasValue => await HandleVisitRelocatedEventAsync(sagaEvent.VisitId.Value, cancellationToken),
            VisitorPreOnboardingSagaEventType.VisitorArrived when sagaEvent.ArrivalId.HasValue => await HandleVisitorArrivedEventAsync(sagaEvent.ArrivalId.Value, cancellationToken),
            _ => true,
        };

    private async Task<bool> HandleStartedEventAsync(Guid sagaId, CancellationToken cancellationToken)
    {
        await ProcessAsync(sagaId, cancellationToken);
        return true;
    }

    private async Task<bool> HandleVisitorConfirmedEventAsync(Guid visitId, Guid invitationId, CancellationToken cancellationToken)
    {
        VisitorPreOnboardingSaga? saga = await db.VisitorPreOnboardingSagas
            .SingleOrDefaultAsync(x => x.VisitId == visitId && x.InvitationId == invitationId, cancellationToken);
        if (saga is null)
            return false;

        if (saga.State is VisitorPreOnboardingState.Confirmed or VisitorPreOnboardingState.Cancelled or VisitorPreOnboardingState.Expired)
            return true;

        if (saga.State != VisitorPreOnboardingState.AwaitingConfirmation)
            return false;

        Visit? visit = await visitorsDb.Visits
            .Include(x => x.Invitations)
            .SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken);
        VisitInvitation? invitation = visit?.Invitations.SingleOrDefault(x => x.Id == invitationId);
        if (visit is null || invitation is null)
            return false;

        Visitor? visitor = await visitorsDb.Visitors.SingleOrDefaultAsync(x => x.Id == invitation.VisitorId, cancellationToken);
        if (visitor is null)
            return false;

        await ConfirmAsync(visitor, visitId, invitationId, cancellationToken);
        return true;
    }

    private async Task<bool> HandleVisitorRejectedEventAsync(Guid visitId, Guid invitationId, CancellationToken cancellationToken)
    {
        VisitorPreOnboardingSaga? saga = await db.VisitorPreOnboardingSagas
            .SingleOrDefaultAsync(x => x.VisitId == visitId && x.InvitationId == invitationId, cancellationToken);
        if (saga is null)
            return false;

        if (saga.State is VisitorPreOnboardingState.Rejected or VisitorPreOnboardingState.Cancelled or VisitorPreOnboardingState.Expired)
            return true;

        if (saga.State != VisitorPreOnboardingState.AwaitingConfirmation)
            return false;

        await RejectAsync(visitId, invitationId, cancellationToken);
        return true;
    }

    private async Task<bool> HandleVisitCancelledEventAsync(Guid visitId, CancellationToken cancellationToken)
    {
        await CancelForVisitAsync(visitId, cancellationToken);
        return true;
    }

    private async Task<bool> HandleVisitRescheduledEventAsync(Guid visitId, CancellationToken cancellationToken)
    {
        Visit? visit = await visitorsDb.Visits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken);
        if (visit is null)
            return false;

        await VisitRescheduled(visitId, visit.Start, visit.Stop, cancellationToken);
        return true;
    }

    private async Task<bool> HandleVisitRelocatedEventAsync(Guid visitId, CancellationToken cancellationToken)
    {
        Visit? visit = await visitorsDb.Visits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken);
        if (visit is null || !visit.LocationId.HasValue)
            return false;

        await VisitRelocated(visitId, visit.LocationId.Value, cancellationToken);
        return true;
    }

    private async Task<bool> HandleVisitorArrivedEventAsync(Guid arrivalId, CancellationToken cancellationToken)
    {
        VisitorPreOnboardingSaga? saga = await db.VisitorPreOnboardingSagas
            .SingleOrDefaultAsync(x => x.ArrivalId == arrivalId, cancellationToken);
        if (saga is null)
            return false;

        Result<VisitErrors> result = await visitService.MarkVisitorArrived(saga.VisitId, saga.InvitationId, cancellationToken);
        if (result.IsFailure(out _))
            return false;

        VisitorPreOnboardingSagaConfig config = await GetConfigurationAsync(cancellationToken);
        if (config.SendArrivalNotificationToOrganizer && !saga.ArrivalNotificationSentAt.HasValue)
        {
            bool sent = await SendOrganizerNotificationAsync(config, saga, ArrivalTemplate, config.UseCustomArrivalNotification, config.CustomArrivalNotification, ArrivalSubject, cancellationToken);
            if (!sent)
                return false;

            saga.ArrivalNotificationSentAt = timeProvider.GetUtcNow();
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void ScheduleEventRetry(VisitorPreOnboardingSagaEvent sagaEvent, string? failureReason)
    {
        sagaEvent.ScheduleRetry(GetRetryAt(sagaEvent.RetryCount + 1), failureReason);
    }

    public async Task ProcessAsync(Guid sagaId, CancellationToken cancellationToken)
    {
        VisitorPreOnboardingSaga? saga = await db.VisitorPreOnboardingSagas
            .SingleOrDefaultAsync(x => x.Id == sagaId, cancellationToken);

        if (saga is null)
            return;

        await ProcessAsync(saga, cancellationToken);
    }

    public async Task ProcessAsync(VisitorPreOnboardingSaga saga, CancellationToken cancellationToken)
    {
        SagaStepResult result;
        do
        {
            result = await StepAsync(saga, cancellationToken);
        }
        while (result == SagaStepResult.Continue && IsRetryableState(saga.State));
    }

    private static bool IsRetryableState(VisitorPreOnboardingState state) =>
        state is VisitorPreOnboardingState.RegisteringArrival
            or VisitorPreOnboardingState.GeneratingQr
            or VisitorPreOnboardingState.UpdatingArrivalQr
            or VisitorPreOnboardingState.SendingInvitation
            or VisitorPreOnboardingState.Cancelling;

    internal async Task<SagaStepResult> StepAsync(VisitorPreOnboardingSaga saga, CancellationToken cancellationToken)
    {
        return saga.State switch
        {
            VisitorPreOnboardingState.RegisteringArrival => await RegisterArrivalAsync(saga, cancellationToken),
            VisitorPreOnboardingState.GeneratingQr => await GenerateQrCodeAsync(saga, cancellationToken),
            VisitorPreOnboardingState.UpdatingArrivalQr => await UpdateArrivalQrAsync(saga, cancellationToken),
            VisitorPreOnboardingState.SendingInvitation => await SendInvitationAsync(saga, cancellationToken),
            VisitorPreOnboardingState.Cancelling => await CancelSagaAsync(saga, cancellationToken),
            _ => SagaStepResult.Continue,
        };
    }

    private async Task<SagaStepResult> RegisterArrivalAsync(VisitorPreOnboardingSaga saga, CancellationToken cancellationToken)
    {

        Visit visit = await visitorsDb.Visits
            .Include(x => x.Invitations)
            .SingleAsync(x => x.Id == saga.VisitId, cancellationToken);

        VisitInvitation invitation = visit.Invitations.Single(x => x.Id == saga.InvitationId);

        Result<ExpectedArrival, ReceptionErrors> result = await receptionService.RegisterVisitorArrival(invitation.FirstName, invitation.LastName, invitation.Company, invitation.VisitorId, invitation.Id, visit.Start, visit.Stop, null, visit.LocationId, cancellationToken);


        if (result.IsSuccess(out ExpectedArrival? arrival))
        {
            saga.ArrivalId = arrival.Id;
            saga.State = VisitorPreOnboardingState.GeneratingQr;
            saga.RetryCount = 0;
            saga.NextRetryAt = null;
        }

        if (result.IsFailure(out ReceptionErrors failure))
        {
            saga.ArrivalId = null;
            saga.RetryCount++;
            saga.State = VisitorPreOnboardingState.RegisteringArrival;
            saga.NextRetryAt = timeProvider.GetUtcNow().Add(_retryInterval);
        }

        await db.SaveChangesAsync(cancellationToken);
        return saga.NextRetryAt == null ? SagaStepResult.Continue : SagaStepResult.Retry;
    }

    private async Task<SagaStepResult> GenerateQrCodeAsync(VisitorPreOnboardingSaga saga, CancellationToken cancellationToken)
    {
        VisitorPreOnboardingSagaConfig config = await GetConfigurationAsync(cancellationToken);
        if (config.QrGenerationMode == CredentialGenerationMode.AccessControlQr)
            return await GenerateAccessControlQrCodeAsync(saga, config, cancellationToken);

        saga.QrCode = Guid.NewGuid().ToString();
        saga.State = VisitorPreOnboardingState.UpdatingArrivalQr;
        saga.RetryCount = 0;
        saga.NextRetryAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return SagaStepResult.Continue;
    }

    private async Task<SagaStepResult> GenerateAccessControlQrCodeAsync(VisitorPreOnboardingSaga saga, VisitorPreOnboardingSagaConfig config, CancellationToken cancellationToken)
    {
        if (!config.SystemId.HasValue || !config.BadgeTypeId.HasValue)
        {
            SagaStepResult result = ScheduleRetry(saga);
            await db.SaveChangesAsync(cancellationToken);
            return result;
        }

        Visit? visit = await visitorsDb.Visits
            .Include(x => x.Invitations)
            .SingleOrDefaultAsync(x => x.Id == saga.VisitId, cancellationToken);
        VisitInvitation? invitation = visit?.Invitations.FirstOrDefault(x => x.Id == saga.InvitationId);
        if (visit is null || invitation is null)
        {
            SagaStepResult result = ScheduleRetry(saga);
            await db.SaveChangesAsync(cancellationToken);
            return result;
        }

        if (!saga.AccessPolicyId.HasValue)
            throw new NotImplementedException("Visitor pre-onboarding PACS policy creation has not been migrated from AccessPolicies yet.");

        saga.QrCode ??= saga.AccessPolicyId.Value.ToString();
        saga.State = VisitorPreOnboardingState.UpdatingArrivalQr;
        saga.RetryCount = 0;
        saga.NextRetryAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return SagaStepResult.Continue;
    }

    private async Task<SagaStepResult> UpdateArrivalQrAsync(VisitorPreOnboardingSaga saga, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(saga.QrCode) && saga.ArrivalId.HasValue)
        {
            Result<ReceptionErrors> result = await receptionService.UpdateArrivalCode(saga.ArrivalId.Value, saga.QrCode, cancellationToken);
            if (result.IsSuccess(out _))
            {
                saga.State = VisitorPreOnboardingState.SendingInvitation;
                saga.RetryCount = 0;
                saga.NextRetryAt = null;
                await db.SaveChangesAsync(cancellationToken);

                return SagaStepResult.Continue;
            }
        }

        saga.State = VisitorPreOnboardingState.UpdatingArrivalQr;
        saga.RetryCount++;
        saga.NextRetryAt = timeProvider.GetUtcNow().Add(_retryInterval);
        await db.SaveChangesAsync(cancellationToken);
        return SagaStepResult.Retry;
    }

    private async Task<SagaStepResult> SendInvitationAsync(VisitorPreOnboardingSaga saga, CancellationToken cancellationToken)
    {
        Visit? visit = await visitorsDb.Visits
        .Include(x => x.Invitations)
        .SingleOrDefaultAsync(x => x.Id == saga.VisitId, cancellationToken);

        if (visit is null)
        {
            SagaStepResult result = ScheduleRetry(saga);
            await db.SaveChangesAsync(cancellationToken);
            return result;
        }

        VisitInvitation? invitation = visit.Invitations.FirstOrDefault(x => x.Id == saga.InvitationId);
        if (invitation is null)
        {
            SagaStepResult result = ScheduleRetry(saga);
            await db.SaveChangesAsync(cancellationToken);
            return result;
        }

        VisitorPreOnboardingSagaConfig config = await GetConfigurationAsync(cancellationToken);
        NotificationContent notification = await GetNotificationContentAsync(InvitationSubject, InvitationTemplate, config.UseCustomInviteNotification, config.CustomInviteNotification, cancellationToken);
        Result<EmailErrors> emailResult = await emailNotificationSender.SendEmail(
            notification.Subject,
            notification.Body,
            await CreateNotificationModelAsync(visit, invitation, saga.QrCode, cancellationToken),
            [invitation.Email],
            ct: cancellationToken);

        if (emailResult.IsFailure(out _))
        {
            SagaStepResult result = ScheduleRetry(saga);
            await db.SaveChangesAsync(cancellationToken);
            return result;
        }

        saga.State = VisitorPreOnboardingState.AwaitingConfirmation;
        saga.RetryCount = 0;
        saga.NextRetryAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return SagaStepResult.Continue;
    }

    private async Task<SagaStepResult> CancelSagaAsync(VisitorPreOnboardingSaga saga, CancellationToken cancellationToken)
    {
        if (saga.ArrivalId.HasValue)
        {
            _ = await receptionService.Cancel(saga.ArrivalId.Value, cancellationToken);
        }

        if (saga.AccessPolicyId.HasValue)
            throw new NotImplementedException("Visitor pre-onboarding PACS policy retraction has not been migrated from AccessPolicies yet.");

        VisitorPreOnboardingSagaConfig config = await GetConfigurationAsync(cancellationToken);
        if (config.SendCancellationNotification)
        {
            bool sent = await SendVisitorNotificationAsync(saga.VisitId, saga.InvitationId, saga.QrCode, CancellationTemplate, config.UseCustomCancellationNotification, config.CustomCancellationNotification, CancellationSubject, cancellationToken);
            if (!sent)
            {
                SagaStepResult result = ScheduleRetry(saga);
                await db.SaveChangesAsync(cancellationToken);
                return result;
            }
        }

        saga.State = VisitorPreOnboardingState.Cancelled;
        saga.RetryCount = 0;
        saga.NextRetryAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return SagaStepResult.Continue;
    }

    private async Task<bool> ExpireSagaAsync(VisitorPreOnboardingSaga saga, CancellationToken cancellationToken)
    {
        if (saga.AccessPolicyId.HasValue)
            throw new NotImplementedException("Visitor pre-onboarding PACS policy retraction has not been migrated from AccessPolicies yet.");

        saga.State = VisitorPreOnboardingState.Expired;
        saga.RetryCount = 0;
        saga.NextRetryAt = null;
        return true;
    }

    private SagaStepResult ScheduleRetry(VisitorPreOnboardingSaga saga)
    {
        saga.RetryCount++;
        saga.NextRetryAt = GetRetryAt(saga.RetryCount);
        return SagaStepResult.Retry;
    }

    private DateTimeOffset GetRetryAt(int retryCount)
    {
        var delay = TimeSpan.FromMinutes(5 * Math.Pow(2, retryCount - 1));
        var capped = TimeSpan.FromMinutes(Math.Min(delay.TotalMinutes, 60));
        return timeProvider.GetUtcNow().Add(capped);
    }

    private async Task SendConfirmationToOrganizerAsync(VisitorPreOnboardingSagaConfig config, VisitorPreOnboardingSaga saga, CancellationToken cancellationToken)
    {
        _ = await SendOrganizerNotificationAsync(config, saga, ConfirmationTemplate, config.UseCustomConfirmNotification, config.CustomConfirmNotification, ConfirmationSubject, cancellationToken);
    }

    private async Task<bool> SendOrganizerNotificationAsync(
        VisitorPreOnboardingSagaConfig config,
        VisitorPreOnboardingSaga saga,
        string defaultTemplate,
        bool useCustomTemplate,
        CustomNotification? customNotification,
        string subject,
        CancellationToken cancellationToken)
    {
        Visit? visit = await visitorsDb.Visits
            .Include(x => x.Invitations)
            .SingleOrDefaultAsync(x => x.Id == saga.VisitId, cancellationToken);

        if (visit is null)
            return false;

        VisitInvitation? invitation = visit.Invitations.FirstOrDefault(x => x.Id == saga.InvitationId);
        if (invitation is null)
            return false;

        Organizer? organizer = await visitorsDb.Organizers.SingleOrDefaultAsync(x => x.Id == visit.OrganizerId, cancellationToken);
        if (organizer is null)
            return false;

        NotificationContent notification = await GetNotificationContentAsync(subject, defaultTemplate, useCustomTemplate, customNotification, cancellationToken);
        Result<EmailErrors> emailResult = await emailNotificationSender.SendEmail(
            notification.Subject,
            notification.Body,
            await CreateNotificationModelAsync(visit, invitation, saga.QrCode, cancellationToken),
            [organizer.Email],
            ct: cancellationToken);

        return emailResult.IsSuccess(out _);
    }

    private async Task<bool> SendVisitorNotificationAsync(
        Guid visitId,
        Guid invitationId,
        string? qrCode,
        string defaultTemplate,
        bool useCustomTemplate,
        CustomNotification? customNotification,
        string subject,
        CancellationToken cancellationToken)
    {
        Visit? visit = await visitorsDb.Visits
            .Include(x => x.Invitations)
            .SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken);

        if (visit is null)
            return false;

        VisitInvitation? invitation = visit.Invitations.FirstOrDefault(x => x.Id == invitationId);
        if (invitation is null)
            return false;

        NotificationContent notification = await GetNotificationContentAsync(subject, defaultTemplate, useCustomTemplate, customNotification, cancellationToken);
        Result<EmailErrors> emailResult = await emailNotificationSender.SendEmail(
            notification.Subject,
            notification.Body,
            await CreateNotificationModelAsync(visit, invitation, qrCode, cancellationToken),
            [invitation.Email],
            ct: cancellationToken);

        return emailResult.IsSuccess(out _);
    }

    private async Task<SagaNotificationModel> CreateNotificationModelAsync(Visit visit, VisitInvitation invitation, string? qrCode, CancellationToken cancellationToken)
    {
        string platformBaseUrl = tenantBaseUrlResolver.GetBaseUrl();
        string? qrCodeLink = string.IsNullOrWhiteSpace(qrCode)
            ? null
            : $"{platformBaseUrl}/api/sagas/visitor-pre-onboarding/qr?code={Uri.EscapeDataString(qrCode)}&size=150";
        string confirmationLink = $"{platformBaseUrl}/visitor-confirmation/{visit.Id}/{invitation.Id}";

        LocationNotificationModel? location = null;
        if (visit.LocationId.HasValue)
        {
            Location? visitLocation = await locationService.GetLocationById(visit.LocationId.Value, cancellationToken);
            location = visitLocation is null ? null : LocationNotificationModel.FromLocation(visitLocation);
        }

        return new SagaNotificationModel(invitation, VisitNotificationModel.FromVisit(visit), location, platformBaseUrl, qrCodeLink, confirmationLink);
    }

    private async Task<NotificationContent> GetNotificationContentAsync(string defaultSubject, string defaultTemplate, bool useCustomTemplate, CustomNotification? customNotification, CancellationToken cancellationToken)
    {
        if (useCustomTemplate && customNotification is not null)
            return new NotificationContent(customNotification.Subject, customNotification.Body);

        string path = Path.Combine(webHostEnvironment.ContentRootPath, "Sagas", "VisitorPreOnboarding", "default-templates", defaultTemplate);
        string body = await File.ReadAllTextAsync(path, cancellationToken);
        return new NotificationContent(defaultSubject, body);
    }

    private sealed record NotificationContent(string Subject, string Body);
}
