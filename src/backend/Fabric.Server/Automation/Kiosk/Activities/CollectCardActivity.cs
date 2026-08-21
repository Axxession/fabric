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

[Activity("Fabric", "Kiosk", "Collect or capture a card using a kiosk-bound collector.", DisplayName = "Collect Card")]
[FlowNode(DoneOutcome, ErrorOutcome)]
public sealed class CollectCardActivity : Activity<CollectCardResult>
{
    public const string DoneOutcome = "Done";
    public const string ErrorOutcome = "Error";

    [Input(DisplayName = "Collector slot number")]
    public Input<int> SlotNumber { get; set; } = default!;

    [Input(DisplayName = "Action", Description = "Collect or Capture")]
    public Input<CollectorCollectAction> Action { get; set; } = new(CollectorCollectAction.Collect);

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var accessor = context.GetRequiredService<KioskWorkflowAccessor>();
        var collectorService = context.GetRequiredService<ICollectorService>();
        var deviceResolver = context.GetRequiredService<KioskDeviceResolver>();

        try
        {
            KioskSession session = await accessor.GetRequiredSessionAsync(context, context.CancellationToken);
            int slotNumber = context.Get(SlotNumber);
            if (slotNumber <= 0)
            {
                await CompleteAsync(
                    context,
                    new CollectCardResult(context.Get(Action), false, Status: null, ErrorCode: "invalid_slot", ErrorMessage: "Collector slot number must be greater than zero."),
                    ErrorOutcome);
                return;
            }

            KioskDeviceResolutionResult resolutionResult = await deviceResolver.ResolveDetailedAsync(session.KioskId, KioskDeviceType.Collector, slotNumber, context.CancellationToken);
            KioskDeviceResolution? resolution = resolutionResult.Resolution;
            if (resolution is null)
            {
                await CompleteAsync(
                    context,
                    new CollectCardResult(context.Get(Action), false, Status: null, ErrorCode: "device_unavailable", ErrorMessage: resolutionResult.ErrorMessage ?? $"Kiosk collector slot '{slotNumber}' is not configured or available."),
                    ErrorOutcome);
                return;
            }

            CollectorCollectAction action = context.Get(Action);
            CollectorCollectResponse response = await collectorService.CollectAsync(resolution.Device, action, context.CancellationToken);
            await CompleteAsync(
                context,
                new CollectCardResult(action, response.Status == HardwareOperationStatus.Succeeded, response.Status, response.Error?.Code, response.Error?.Message),
                response.Status == HardwareOperationStatus.Succeeded ? DoneOutcome : ErrorOutcome);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await CompleteAsync(
                context,
                new CollectCardResult(context.Get(Action), false, Status: null, ErrorCode: "exception", ErrorMessage: ex.Message),
                ErrorOutcome);
        }
    }

    private async ValueTask CompleteAsync(ActivityExecutionContext context, CollectCardResult result, string outcome)
    {
        context.Set(Result, result);
        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}

public sealed record CollectCardResult(
    CollectorCollectAction Action,
    bool Success,
    HardwareOperationStatus? Status,
    string? ErrorCode,
    string? ErrorMessage);
