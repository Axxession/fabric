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

[Activity("Fabric", "Kiosk", "Wait for a card in a kiosk-bound collector and read its card number.", DisplayName = "Wait for Card In Collector")]
[FlowNode(CollectedCardOutcome, TimedOutOutcome, ErrorOutcome)]
public sealed class WaitForCardInCollectorActivity : Activity<string?>
{
    public const string CollectedCardOutcome = "Collected Card";
    public const string TimedOutOutcome = "Timed Out";
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
                await CompleteAsync(context, cardNumber: null, ErrorOutcome);
                return;
            }

            int timeoutSeconds = context.Get(TimeoutSeconds);
            if (timeoutSeconds <= 0)
            {
                await CompleteAsync(context, cardNumber: null, ErrorOutcome);
                return;
            }

            KioskDeviceResolutionResult resolutionResult = await deviceResolver.ResolveDetailedAsync(session.KioskId, KioskDeviceType.Collector, slotNumber, context.CancellationToken);
            KioskDeviceResolution? resolution = resolutionResult.Resolution;
            if (resolution is null)
            {
                await CompleteAsync(context, cardNumber: null, ErrorOutcome);
                return;
            }

            await instructionService.ShowMessageAsync(
                session.Id,
                new KioskInstructionLayout(context.Get(Mode) ?? "default", context.Get(BackgroundAssetName), context.Get(ImageAssetName)),
                new KioskInstructionContent(context.Get(Title), context.Get(Message)),
                context.CancellationToken);
            clearInstruction = true;

            CollectorCardResponse response = await collectorService.WaitForCardAsync(resolution.Device, TimeSpan.FromSeconds(timeoutSeconds), context.CancellationToken);
            string outcome = response.Status switch
            {
                HardwareOperationStatus.Succeeded => CollectedCardOutcome,
                HardwareOperationStatus.Timeout => TimedOutOutcome,
                _ => ErrorOutcome
            };

            await CompleteAsync(context, response.CardNumber, outcome);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            await CompleteAsync(context, cardNumber: null, ErrorOutcome);
        }
        finally
        {
            if (clearInstruction && sessionId.HasValue)
                await instructionService.ClearCurrentInstructionAsync(sessionId.Value, context.CancellationToken);
        }
    }

    private async ValueTask CompleteAsync(ActivityExecutionContext context, string? cardNumber, string outcome)
    {
        context.Set(Result, cardNumber);
        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}
