using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Fabric.Hardware.Contracts;
using Fabric.Server.Hardware.Application;
using Fabric.Server.Kiosk.Application;
using Fabric.Server.Kiosk.Domain;

namespace Fabric.Server.Automation.Kiosk.Activities;

[Activity("Fabric", "Kiosk", "Return a card from a kiosk-bound collector to the user and wait for removal.", DisplayName = "Return Card To User")]
[FlowNode(CardRemovedOutcome, CardInCollectorOutcome, ErrorOutcome)]
public sealed class ReturnCardToUserActivity : Activity<ReturnCardToUserResult>
{
    public const string CardRemovedOutcome = "Card Removed";
    public const string CardInCollectorOutcome = "Card In Collector";
    public const string ErrorOutcome = "Error";

    [Input(DisplayName = "Collector slot number")]
    public Input<int> SlotNumber { get; set; } = default!;

    [Input(DisplayName = "Timeout in seconds")]
    public Input<int> TimeoutSeconds { get; set; } = new(30);

    [Input(DisplayName = "Layout mode", Description = "default, split-left-visual, or split-right-visual")]
    public Input<string> Mode { get; set; } = new("default");

    [Input(DisplayName = "Background asset")]
    public Input<string?> BackgroundAssetName { get; set; } = default!;

    [Input(DisplayName = "Image asset")]
    public Input<string?> ImageAssetName { get; set; } = default!;

    [Input(DisplayName = "Title")]
    public Input<string?> Title { get; set; } = default!;

    [Input(DisplayName = "Message")]
    public Input<string?> Message { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var accessor = context.GetRequiredService<KioskWorkflowAccessor>();
        var collectorService = context.GetRequiredService<ICollectorService>();
        var deviceResolver = context.GetRequiredService<KioskDeviceResolver>();
        var instructionService = context.GetRequiredService<KioskInstructionService>();

        Guid? sessionId = null;
        bool clearInstruction = false;

        try
        {
            KioskSession session = await accessor.GetRequiredSessionAsync(context, context.CancellationToken);
            sessionId = session.Id;

            int slotNumber = context.Get(SlotNumber);
            if (slotNumber <= 0)
            {
                await CompleteAsync(
                    context,
                    new ReturnCardToUserResult(false, Status: null, ErrorCode: "invalid_slot", ErrorMessage: "Collector slot number must be greater than zero."),
                    ErrorOutcome);
                return;
            }

            int timeoutSeconds = context.Get(TimeoutSeconds);
            if (timeoutSeconds <= 0)
            {
                await CompleteAsync(
                    context,
                    new ReturnCardToUserResult(false, Status: null, ErrorCode: "invalid_timeout", ErrorMessage: "Timeout in seconds must be greater than zero."),
                    ErrorOutcome);
                return;
            }

            KioskDeviceResolutionResult resolutionResult = await deviceResolver.ResolveDetailedAsync(session.KioskId, KioskDeviceType.Collector, slotNumber, context.CancellationToken);
            KioskDeviceResolution? resolution = resolutionResult.Resolution;
            if (resolution is null)
            {
                await CompleteAsync(
                    context,
                    new ReturnCardToUserResult(false, Status: null, ErrorCode: "device_unavailable", ErrorMessage: resolutionResult.ErrorMessage ?? $"Kiosk collector slot '{slotNumber}' is not configured or available."),
                    ErrorOutcome);
                return;
            }

            await instructionService.ShowMessageAsync(
                session.Id,
                new KioskInstructionLayout(context.Get(Mode) ?? "default", context.Get(BackgroundAssetName), context.Get(ImageAssetName)),
                new KioskInstructionContent(context.Get(Title), context.Get(Message)),
                context.CancellationToken);
            clearInstruction = true;

            CollectorEjectResponse ejectResponse = await collectorService.EjectAsync(resolution.Device, context.CancellationToken);
            if (ejectResponse.Status != HardwareOperationStatus.Succeeded)
            {
                await CompleteAsync(
                    context,
                    new ReturnCardToUserResult(false, ejectResponse.Status, ejectResponse.Error?.Code, ejectResponse.Error?.Message),
                    ErrorOutcome);
                return;
            }

            CollectorRemovalResponse removalResponse = await collectorService.WaitForRemovalAsync(resolution.Device, TimeSpan.FromSeconds(timeoutSeconds), context.CancellationToken);
            string outcome = removalResponse.Status switch
            {
                HardwareOperationStatus.Succeeded => CardRemovedOutcome,
                HardwareOperationStatus.Timeout => CardInCollectorOutcome,
                _ => ErrorOutcome
            };

            await CompleteAsync(
                context,
                new ReturnCardToUserResult(removalResponse.Status == HardwareOperationStatus.Succeeded, removalResponse.Status, removalResponse.Error?.Code, removalResponse.Error?.Message),
                outcome);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await CompleteAsync(
                context,
                new ReturnCardToUserResult(false, Status: null, ErrorCode: "exception", ErrorMessage: ex.Message),
                ErrorOutcome);
        }
        finally
        {
            if (clearInstruction && sessionId.HasValue)
                await instructionService.ClearCurrentInstructionAsync(sessionId.Value, context.CancellationToken);
        }
    }

    private async ValueTask CompleteAsync(ActivityExecutionContext context, ReturnCardToUserResult result, string outcome)
    {
        context.Set(Result, result);
        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}

public sealed record ReturnCardToUserResult(
    bool Removed,
    HardwareOperationStatus? Status,
    string? ErrorCode,
    string? ErrorMessage);
