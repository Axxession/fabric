using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Fabric.Hardware.Contracts;
using Fabric.Server.Automation.Kiosk;
using Fabric.Server.Hardware.Application;
using Fabric.Server.Kiosk.Application;
using Fabric.Server.Kiosk.Domain;
using Fabric.Server.Sagas.Kiosk;

namespace Fabric.Server.Automation.Kiosk.Activities;

[Activity("Fabric", "Kiosk", "Read a Belgian eID card, optionally verify PIN, and wait for removal.", DisplayName = "Read Eid Card")]
[FlowNode(ValidEidOutcome, InvalidPinOutcome, ExpiredEidOutcome, CancelledOutcome, ErrorOutcome)]
public sealed class ReadEidCardActivity : Activity<ReadEidCardResult>
{
    private const string PendingIdentityPropertyName = "Kiosk.ReadEid.PendingIdentity";

    public const string ValidEidOutcome = "Valid Eid";
    public const string InvalidPinOutcome = "Invalid Pin";
    public const string ExpiredEidOutcome = "Expired Eid";
    public const string CancelledOutcome = "Cancelled";
    public const string ErrorOutcome = "Error";

    [Input(DisplayName = "eID reader slot number")]
    public Input<int> SlotNumber { get; set; } = default!;

    [Input(DisplayName = "Ask for PIN")]
    public Input<bool> AskForPin { get; set; } = new(true);

    [Input(DisplayName = "PIN label")]
    public Input<string> PinLabel { get; set; } = new("eid-pin");

    [Input(DisplayName = "Insert layout mode", Description = "default, split-left-visual, or split-right-visual")]
    public Input<string> InsertMode { get; set; } = new("default");

    [Input(DisplayName = "Insert background asset")]
    public Input<string?> InsertBackgroundAssetName { get; set; } = default!;

    [Input(DisplayName = "Insert image asset")]
    public Input<string?> InsertImageAssetName { get; set; } = default!;

    [Input(DisplayName = "Insert title")]
    public Input<string?> InsertTitle { get; set; } = default!;

    [Input(DisplayName = "Insert message")]
    public Input<string?> InsertMessage { get; set; } = default!;

    [Input(DisplayName = "Remove layout mode", Description = "default, split-left-visual, or split-right-visual")]
    public Input<string> RemoveMode { get; set; } = new("default");

    [Input(DisplayName = "Remove background asset")]
    public Input<string?> RemoveBackgroundAssetName { get; set; } = default!;

    [Input(DisplayName = "Remove image asset")]
    public Input<string?> RemoveImageAssetName { get; set; } = default!;

    [Input(DisplayName = "Remove title")]
    public Input<string?> RemoveTitle { get; set; } = default!;

    [Input(DisplayName = "Remove message")]
    public Input<string?> RemoveMessage { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var accessor = context.GetRequiredService<KioskWorkflowAccessor>();
        var deviceResolver = context.GetRequiredService<KioskDeviceResolver>();
        var eidReaderService = context.GetRequiredService<IEidReaderService>();
        var instructionService = context.GetRequiredService<KioskInstructionService>();
        var sagaService = context.GetRequiredService<KioskSagaService>();
        TimeProvider timeProvider = context.GetRequiredService<TimeProvider>();

        Guid? sessionId = null;
        try
        {
            KioskSession session = await accessor.GetRequiredSessionAsync(context, context.CancellationToken);
            sessionId = session.Id;

            int slotNumber = context.Get(SlotNumber);
            if (slotNumber <= 0)
            {
                await CompleteAsync(context, new ReadEidCardResult(null, null, null, null, null, null, null, null, false, false, "invalid_slot", "eID reader slot number must be greater than zero."), ErrorOutcome);
                return;
            }

            KioskDeviceResolutionResult resolutionResult = await deviceResolver.ResolveDetailedAsync(session.KioskId, KioskDeviceType.EidReader, slotNumber, context.CancellationToken);
            KioskDeviceResolution? resolution = resolutionResult.Resolution;
            if (resolution is null)
            {
                await CompleteAsync(context, new ReadEidCardResult(null, null, null, null, null, null, null, null, false, false, "device_unavailable", resolutionResult.ErrorMessage ?? $"Kiosk eID reader slot '{slotNumber}' is not configured or available."), ErrorOutcome);
                return;
            }

            await instructionService.ShowMessageAsync(
                session.Id,
                new KioskInstructionLayout(context.Get(InsertMode) ?? "default", context.Get(InsertBackgroundAssetName), context.Get(InsertImageAssetName)),
                new KioskInstructionContent(context.Get(InsertTitle), context.Get(InsertMessage)),
                context.CancellationToken);

            EidReadResponse readResponse;
            try
            {
                readResponse = await eidReaderService.ReadAsync(resolution.Device, context.CancellationToken);
            }
            finally
            {
                await instructionService.ClearCurrentInstructionAsync(session.Id, context.CancellationToken);
            }

            if (readResponse.Status == HardwareOperationStatus.Cancelled)
            {
                await CompleteAsync(context, ToResult(readResponse, false, false), CancelledOutcome);
                return;
            }

            if (readResponse.Status != HardwareOperationStatus.Succeeded)
            {
                await CompleteAsync(context, ToResult(readResponse, false, false), ErrorOutcome);
                return;
            }

            bool expired = IsExpired(readResponse.ExpiryDate, timeProvider);
            bool requiresPin = context.Get(AskForPin) && !expired;
            if (!requiresPin)
            {
                string outcome = expired ? ExpiredEidOutcome : ValidEidOutcome;
                await ShowRemoveAndWaitAsync(context, instructionService, eidReaderService, resolution.Device, session.Id, ToResult(readResponse, !expired, expired), outcome);
                return;
            }

            context.WorkflowExecutionContext.SetProperty(PendingIdentityPropertyName, JsonSerializer.Serialize(readResponse));

            KioskInstructionBookmark bookmark = await sagaService.ScheduleInstructionAsync(
                session.Id,
                context.WorkflowExecutionContext.Id,
                BuildPinInstruction(context),
                context.CancellationToken);

            context.CreateBookmark(bookmark, ResumeAfterPinAsync, includeActivityInstanceId: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (sessionId.HasValue)
                await context.GetRequiredService<KioskInstructionService>().ClearCurrentInstructionAsync(sessionId.Value, context.CancellationToken);

            await CompleteAsync(context, new ReadEidCardResult(null, null, null, null, null, null, null, null, false, false, "exception", ex.Message), ErrorOutcome);
        }
    }

    private async ValueTask ResumeAfterPinAsync(ActivityExecutionContext context)
    {
        var accessor = context.GetRequiredService<KioskWorkflowAccessor>();
        var deviceResolver = context.GetRequiredService<KioskDeviceResolver>();
        var eidReaderService = context.GetRequiredService<IEidReaderService>();
        var instructionService = context.GetRequiredService<KioskInstructionService>();
        TimeProvider timeProvider = context.GetRequiredService<TimeProvider>();

        KioskSession session = await accessor.GetRequiredSessionAsync(context, context.CancellationToken);
        await instructionService.ClearCurrentInstructionAsync(session.Id, context.CancellationToken);

        if (context.TryGetWorkflowInput(KioskWorkflowContext.CancelledInputName, out bool cancelled) && cancelled)
        {
            EidReadResponse? cancelledReadResponse = GetPendingReadResponse(context);
            await CompleteAsync(context, cancelledReadResponse is null ? new ReadEidCardResult(null, null, null, null, null, null, null, null, false, false, "cancelled", "eID read was cancelled.") : ToResult(cancelledReadResponse, false, false), CancelledOutcome);
            return;
        }

        EidReadResponse readResponse = GetPendingReadResponse(context) ?? throw new InvalidOperationException("eID activity state was not found.");
        int slotNumber = context.Get(SlotNumber);
        KioskDeviceResolutionResult resolutionResult = await deviceResolver.ResolveDetailedAsync(session.KioskId, KioskDeviceType.EidReader, slotNumber, context.CancellationToken);
        KioskDeviceResolution? resolution = resolutionResult.Resolution;
        if (resolution is null)
        {
            await CompleteAsync(context, ToResult(readResponse, false, false, "device_unavailable", resolutionResult.ErrorMessage ?? $"Kiosk eID reader slot '{slotNumber}' is not configured or available."), ErrorOutcome);
            return;
        }

        KioskInstructionResult response = context.GetWorkflowInput<KioskInstructionResult>(KioskWorkflowContext.InstructionResponseInputName);
        string pin = response is KioskFormInstructionResult formResult
            ? formResult.Values.GetValueOrDefault("pin") ?? string.Empty
            : throw new InvalidOperationException($"Expected {nameof(KioskFormInstructionResult)} but received {response.GetType().Name}.");

        EidVerifyPinResponse pinResponse = await eidReaderService.VerifyPinAsync(resolution.Device, pin, context.CancellationToken);
        if (pinResponse.Status == HardwareOperationStatus.Cancelled)
        {
            await CompleteAsync(context, ToResult(readResponse, false, false), CancelledOutcome);
            return;
        }

        if (pinResponse.Status != HardwareOperationStatus.Succeeded)
        {
            await CompleteAsync(context, ToResult(readResponse, false, false, pinResponse.Error?.Code, pinResponse.Error?.Message), ErrorOutcome);
            return;
        }

        bool expired = IsExpired(readResponse.ExpiryDate, timeProvider);
        if (!pinResponse.ValidPin)
        {
            await ShowRemoveAndWaitAsync(context, instructionService, eidReaderService, resolution.Device, session.Id, ToResult(readResponse, false, expired), InvalidPinOutcome);
            return;
        }

        string outcome = expired ? ExpiredEidOutcome : ValidEidOutcome;
        await ShowRemoveAndWaitAsync(context, instructionService, eidReaderService, resolution.Device, session.Id, ToResult(readResponse, !expired, expired), outcome);
    }

    private KioskInstructionDefinition BuildPinInstruction(ActivityExecutionContext context) =>
        new(
            KioskInstructionActivityKind.Form,
            "display-form",
            new KioskInstructionLayout("default", null, null),
            new KioskInstructionContent("Enter PIN", null),
            [],
            [new KioskFormField("pin", context.Get(PinLabel) ?? "eid-pin", null, true, true)]);

    private async Task ShowRemoveAndWaitAsync(ActivityExecutionContext context, KioskInstructionService instructionService, IEidReaderService eidReaderService, HardwareDeviceRef device, Guid sessionId, ReadEidCardResult result, string successOutcome)
    {
        await instructionService.ShowMessageAsync(
            sessionId,
            new KioskInstructionLayout(context.Get(RemoveMode) ?? "default", context.Get(RemoveBackgroundAssetName), context.Get(RemoveImageAssetName)),
            new KioskInstructionContent(context.Get(RemoveTitle), context.Get(RemoveMessage)),
            context.CancellationToken);

        try
        {
            EidWaitRemovalResponse removalResponse = await eidReaderService.WaitForRemovalAsync(device, context.CancellationToken);
            if (removalResponse.Status == HardwareOperationStatus.Cancelled)
            {
                await CompleteAsync(context, result with { ErrorCode = removalResponse.Error?.Code, ErrorMessage = removalResponse.Error?.Message }, CancelledOutcome);
                return;
            }

            if (removalResponse.Status != HardwareOperationStatus.Succeeded)
            {
                await CompleteAsync(context, result with { ErrorCode = removalResponse.Error?.Code, ErrorMessage = removalResponse.Error?.Message }, ErrorOutcome);
                return;
            }
        }
        finally
        {
            await instructionService.ClearCurrentInstructionAsync(sessionId, context.CancellationToken);
        }

        await CompleteAsync(context, result, successOutcome);
    }

    private static EidReadResponse? GetPendingReadResponse(ActivityExecutionContext context)
    {
        string? json = context.WorkflowExecutionContext.GetProperty<string>(PendingIdentityPropertyName);
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<EidReadResponse>(json);
    }

    private static bool IsExpired(DateOnly? expiryDate, TimeProvider timeProvider) =>
        expiryDate.HasValue && expiryDate.Value < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    private static ReadEidCardResult ToResult(EidReadResponse response, bool validPin, bool expired, string? errorCode = null, string? errorMessage = null) =>
        new(
            response.FirstName,
            response.LastName,
            response.NationalNumber,
            response.DocumentNumber,
            response.ExpiryDate,
            response.Nationality,
            response.BirthLocation,
            response.BirthDateRaw,
            validPin,
            expired,
            errorCode ?? response.Error?.Code,
            errorMessage ?? response.Error?.Message);

    private async ValueTask CompleteAsync(ActivityExecutionContext context, ReadEidCardResult result, string outcome)
    {
        context.WorkflowExecutionContext.SetProperty(PendingIdentityPropertyName, string.Empty);
        context.Set(Result, result);
        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}

public sealed record ReadEidCardResult(
    string? FirstName,
    string? LastName,
    string? NationalNumber,
    string? DocumentNumber,
    DateOnly? ExpiryDate,
    string? Nationality,
    string? BirthLocation,
    string? BirthDateRaw,
    bool ValidPin,
    bool Expired,
    string? ErrorCode,
    string? ErrorMessage);
