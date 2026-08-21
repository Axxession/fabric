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

[Activity("Fabric", "Kiosk", "Dispense, prepare, or drop a card using a kiosk-bound dispenser.", DisplayName = "Dispense Card")]
[FlowNode(KnownCardOutcome, UnknownCardOutcome, DroppedCardOutcome, ErrorOutcome)]
public sealed class DispenseCardActivity : Activity<DispenseCardResult>
{
    public const string KnownCardOutcome = "Known Card";
    public const string UnknownCardOutcome = "Unknown Card";
    public const string DroppedCardOutcome = "Dropped Card";
    public const string ErrorOutcome = "Error";

    [Input(DisplayName = "Dispenser slot number")]
    public Input<int> SlotNumber { get; set; } = default!;

    [Input(DisplayName = "Action", Description = "FullDispense, Prepare, or Drop")]
    public Input<DispenserAction> Action { get; set; } = new(DispenserAction.FullDispense);

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var accessor = context.GetRequiredService<KioskWorkflowAccessor>();
        var deviceResolver = context.GetRequiredService<KioskDeviceResolver>();
        var dispenserService = context.GetRequiredService<IDispenserService>();

        try
        {
            KioskSession session = await accessor.GetRequiredSessionAsync(context, context.CancellationToken);
            int slotNumber = context.Get(SlotNumber);
            if (slotNumber <= 0)
            {
                await CompleteAsync(
                    context,
                    new DispenseCardResult(context.Get(Action), false, Status: null, CardNumber: null, ErrorCode: "invalid_slot", ErrorMessage: "Dispenser slot number must be greater than zero."),
                    ErrorOutcome);
                return;
            }

            KioskDeviceResolutionResult resolutionResult = await deviceResolver.ResolveDetailedAsync(session.KioskId, KioskDeviceType.Dispenser, slotNumber, context.CancellationToken);
            KioskDeviceResolution? resolution = resolutionResult.Resolution;
            if (resolution is null)
            {
                await CompleteAsync(
                    context,
                    new DispenseCardResult(context.Get(Action), false, Status: null, CardNumber: null, ErrorCode: "device_unavailable", ErrorMessage: resolutionResult.ErrorMessage ?? $"Kiosk dispenser slot '{slotNumber}' is not configured or available."),
                    ErrorOutcome);
                return;
            }

            DispenserAction action = context.Get(Action);
            DispenserCommandResponse response = await dispenserService.ExecuteAsync(resolution.Device, action, context.CancellationToken);
            string outcome = GetOutcome(response);

            await CompleteAsync(
                context,
                new DispenseCardResult(action, response.Status == HardwareOperationStatus.Succeeded, response.Status, response.CardNumber, response.Error?.Code, response.Error?.Message),
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
                new DispenseCardResult(context.Get(Action), false, Status: null, CardNumber: null, ErrorCode: "exception", ErrorMessage: ex.Message),
                ErrorOutcome);
        }
    }

    private static string GetOutcome(DispenserCommandResponse response)
    {
        if (response.Status != HardwareOperationStatus.Succeeded)
            return ErrorOutcome;

        return response.Action switch
        {
            DispenserAction.Drop => DroppedCardOutcome,
            DispenserAction.Prepare or DispenserAction.FullDispense when !string.IsNullOrWhiteSpace(response.CardNumber) => KnownCardOutcome,
            DispenserAction.Prepare or DispenserAction.FullDispense => UnknownCardOutcome,
            _ => ErrorOutcome
        };
    }

    private async ValueTask CompleteAsync(ActivityExecutionContext context, DispenseCardResult result, string outcome)
    {
        context.Set(Result, result);
        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}

public sealed record DispenseCardResult(
    DispenserAction Action,
    bool Success,
    HardwareOperationStatus? Status,
    string? CardNumber,
    string? ErrorCode,
    string? ErrorMessage);
