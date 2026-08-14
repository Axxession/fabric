using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Fabric.Server.Automation.Kiosk.Providers;
using Fabric.Server.Desfire;
using Fabric.Server.Desfire.Application;
using Fabric.Server.Desfire.Contracts;
using Fabric.Server.Desfire.Domain;
using Fabric.Server.Kiosk.Application;
using Fabric.Server.Kiosk.Domain;

namespace Fabric.Server.Automation.Kiosk.Activities;

[Activity("Fabric", "Kiosk", "Encode a card using a kiosk-bound encoder and DESFire transformation.", DisplayName = "Encode Card")]
[FlowNode(SucceededOutcome, EncoderUnavailableOutcome, EncodingFailedOutcome)]
public sealed class EncodeCardActivity : Activity<EncodeCardResult>
{
    public const string SucceededOutcome = "Succeeded";
    public const string EncoderUnavailableOutcome = "EncoderUnavailable";
    public const string EncodingFailedOutcome = "EncodingFailed";

    [Input(DisplayName = "Encoder slot number")]
    public Input<int> SlotNumber { get; set; } = default!;

    [Input(DisplayName = "Transformation", UIHandler = typeof(EncodingTransformationProvider), UIHint = InputUIHints.DropDown)]
    public Input<Guid?> TransformationId { get; set; } = default!;

    [Input(DisplayName = "Print design", UIHandler = typeof(CardPrintDesignProvider), UIHint = InputUIHints.DropDown)]
    public Input<Guid?> PrintDesignId { get; set; } = default!;

    [Input(DisplayName = "Variables", Description = "JSON object passed as encoding variables.")]
    public Input<JsonElement> Variables { get; set; } = default!;

    [Input(DisplayName = "Layout mode", Description = "default, split-left-visual, or split-right-visual")]
    public Input<string> LayoutMode { get; set; } = new("default");

    [Input(DisplayName = "Background asset")]
    public Input<string?> BackgroundAssetName { get; set; } = default!;

    [Input(DisplayName = "Title")]
    public Input<string?> Title { get; set; } = default!;

    [Input(DisplayName = "Present card message")]
    public Input<string?> MessagePresentCard { get; set; } = default!;

    [Input(DisplayName = "Encoding message")]
    public Input<string?> MessageEncoding { get; set; } = default!;

    [Input(DisplayName = "Remove card message")]
    public Input<string?> MessageRemoveCard { get; set; } = default!;

    [Input(DisplayName = "Present card image asset")]
    public Input<string?> PresentCardImageAssetName { get; set; } = default!;

    [Input(DisplayName = "Encoding image asset")]
    public Input<string?> EncodingImageAssetName { get; set; } = default!;

    [Input(DisplayName = "Remove card image asset")]
    public Input<string?> RemoveCardImageAssetName { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var accessor = context.GetRequiredService<KioskWorkflowAccessor>();
        var deviceResolver = context.GetRequiredService<KioskDeviceResolver>();
        var instructionService = context.GetRequiredService<KioskInstructionService>();
        var encodingService = context.GetRequiredService<DesfireEncodingService>();

        KioskSession session = await accessor.GetRequiredSessionAsync(context, context.CancellationToken);
        int slotNumber = context.Get(SlotNumber);
        if (slotNumber <= 0)
            throw new InvalidOperationException("Encoder slot number must be greater than zero.");

        KioskDeviceResolutionResult resolutionResult = await deviceResolver.ResolveDetailedAsync(session.KioskId, KioskDeviceType.Encoder, slotNumber, context.CancellationToken);
        KioskDeviceResolution? resolution = resolutionResult.Resolution;
        if (resolution is null)
            throw new InvalidOperationException(resolutionResult.ErrorMessage ?? $"Kiosk encoder slot '{slotNumber}' is not configured or available.");

        JsonElement variables = context.Get(Variables);
        if (variables.ValueKind == JsonValueKind.Undefined)
            variables = JsonSerializer.SerializeToElement(new Dictionary<string, string>(), DesfireJson.Options);

        CreateBadgeJobRequest request = new(
            EncoderId: null,
            TransformationId: context.Get(TransformationId),
            PrintDesignId: context.Get(PrintDesignId),
            AgentId: resolution.Device.AgentId,
            DeviceId: resolution.Device.DeviceId,
            Input: variables,
            Mode: BadgeJobMode.Queued,
            Priority: 0,
            Source: DesfireEncodingSources.Kiosk,
            KioskSessionId: session.Id);
        DesfireEncodingResult created = await encodingService.CreateSingleRunAsync(request, context.CancellationToken);
        if (created.Run is null)
            throw new InvalidOperationException("Transformation does not exist.");

        DesfireEncodingResult executed = await encodingService.ExecuteRunAsync(
            created.Run.Id,
            resolution.Device.AgentId,
            resolution.Device.DeviceId,
            requeueOnTransientFailure: false,
            onPhaseChanged: async (phase, cancellationToken) => await ShowPhaseMessageAsync(instructionService, session.Id, phase, cancellationToken),
            context.CancellationToken);

        BadgeJob run = executed.Run ?? created.Run;
        context.Set(Result, new EncodeCardResult(
            executed.Failure is null && run.Status == BadgeJobStatus.Succeeded,
            run.Id,
            run.CardUid,
            run.Status,
            run.ErrorMessage));
        await context.CompleteActivityWithOutcomesAsync(GetOutcome(run.Status));

        async Task ShowPhaseMessageAsync(KioskInstructionService service, Guid sessionId, DesfireEncodingPhase phase, CancellationToken cancellationToken)
        {
            string? message = phase switch
            {
                DesfireEncodingPhase.WaitingForCard => context.Get(MessagePresentCard),
                DesfireEncodingPhase.Encoding => context.Get(MessageEncoding),
                DesfireEncodingPhase.WaitingForRemoval => context.Get(MessageRemoveCard),
                _ => null
            };
            string? imageAssetName = phase switch
            {
                DesfireEncodingPhase.WaitingForCard => context.Get(PresentCardImageAssetName),
                DesfireEncodingPhase.Encoding => context.Get(EncodingImageAssetName),
                DesfireEncodingPhase.WaitingForRemoval => context.Get(RemoveCardImageAssetName),
                _ => null
            };

            await service.ShowMessageAsync(
                sessionId,
                new KioskInstructionLayout(context.Get(LayoutMode) ?? "default", context.Get(BackgroundAssetName), imageAssetName),
                new KioskInstructionContent(context.Get(Title), message),
                cancellationToken);
        }
    }

    private static string GetOutcome(BadgeJobStatus status) => status switch
    {
        BadgeJobStatus.Succeeded => SucceededOutcome,
        BadgeJobStatus.DeviceUnavailable or BadgeJobStatus.Timeout => EncoderUnavailableOutcome,
        _ => EncodingFailedOutcome
    };
}

public sealed record EncodeCardResult(bool Success, Guid RunId, string? CardUid, BadgeJobStatus Status, string? ErrorMessage);
