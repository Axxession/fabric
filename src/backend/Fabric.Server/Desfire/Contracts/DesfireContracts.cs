using System.Text.Json;
using Fabric.Hardware.Desfire.Encoding.Models;
using Fabric.Hardware.Desfire.Encoding.Specifications;
using Fabric.Hardware.Desfire.Protocol;
using Fabric.Server.Hardware.Domain;
using Fabric.Server.Desfire.Application;
using Fabric.Server.Desfire.Domain;

namespace Fabric.Server.Desfire.Contracts;

public sealed record ChipDesignResponse(Guid Id, string Name, int Version, string? Description, TemplateSpecification Specification, DateTimeOffset CreatedAt);

public sealed record CreateChipDesignRequest(string Name, int? Version, string? Description, TemplateSpecification Specification);

public sealed record UpdateChipDesignRequest(string Name, int Version, string? Description, TemplateSpecification Specification);

public sealed record TransformationResponse(Guid Id, string Name, string? FromChipDesignName, bool FromBlank, string ToChipDesignName, bool AlwaysReadUid, IReadOnlyList<string> RequiredVariables, IReadOnlyList<string> RequiredKeyGroups, IReadOnlyList<TransformationVariableConfigRequest> Variables, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateTransformationRequest(string Name, string? FromChipDesignName, bool FromBlank, string ToChipDesignName, IReadOnlyList<TransformationVariableConfigRequest> Variables);

public sealed record UpdateTransformationRequest(string Name, string? FromChipDesignName, bool FromBlank, string ToChipDesignName, IReadOnlyList<TransformationVariableConfigRequest> Variables);

public sealed record TransformationPlanResponse(IReadOnlyList<string> RequiredVariables, IReadOnlyList<string> RequiredKeyGroups, IReadOnlyList<string> Errors, int OperationCount, IReadOnlyList<TransformationPlanOperationResponse> Operations);

public sealed record TransformationPlanOperationResponse(int Order, string Type, string Description);

public sealed record TransformationVariableConfigRequest(string Name, TransformationVariableKind Kind, VariableFormatRequest Format, string? Field = null, SystemVariableProviderKind? SystemProvider = null, string? Value = null, string? SequenceName = null, long? InitialValue = null, Guid? SystemProviderId = null);

public sealed record SystemProviderResponse(Guid Id, string Name, SystemVariableProviderKind ProviderType, string? FixedValue, long? InitialValue, long? CurrentValue, DateTimeOffset CreatedAt);

public sealed record CreateSystemProviderRequest(string Name, SystemVariableProviderKind ProviderType, string? FixedValue, long? InitialValue);

public sealed record EncoderResponse(Guid Id, string Name, string AgentId, string DeviceId, bool SupportsEncoding, bool SupportsPrinting, bool Enabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateEncoderRequest(string Name, string AgentId, string DeviceId, bool Enabled = true);

public sealed record UpdateEncoderRequest(string Name, string AgentId, string DeviceId, bool Enabled = true);

public sealed record KeyDiversificationStrategyResponse(Guid Id, string Name, KeyDiversificationAlgorithm Algorithm, IReadOnlyList<DiversificationInput> Inputs, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateKeyDiversificationStrategyRequest(string Name, KeyDiversificationAlgorithm Algorithm, IReadOnlyList<DiversificationInput> Inputs);

public sealed record UpdateKeyDiversificationStrategyRequest(string Name, KeyDiversificationAlgorithm Algorithm, IReadOnlyList<DiversificationInput> Inputs);

public sealed record KeyGroupResponse(Guid Id, string Name, KeyType KeyType, bool Locked, Guid? DiversificationStrategyId, IReadOnlyList<KeyGroupKeySetResponse> KeySets, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record KeyGroupKeySetResponse(int KeySetId, IReadOnlyList<KeyGroupKeyResponse> Keys);

public sealed record KeyGroupKeyResponse(int KeyId, bool IsDiversified, string? Value);

public sealed record CreateKeyGroupRequest(string Name, KeyType KeyType, int NumberOfKeySets, int NumberOfKeys);

public sealed record UpdateKeyGroupRequest(string Name, Guid? DiversificationStrategyId, IReadOnlyList<KeyGroupKeySetRequest> KeySets);

public sealed record KeyGroupKeySetRequest(int KeySetId, IReadOnlyList<KeyGroupKeyRequest> Keys);

public sealed record KeyGroupKeyRequest(int KeyId, string Value, bool IsDiversified);

public sealed record BadgeBatchResponse(Guid Id, string Name, Guid? EncoderId, Guid? TransformationId, Guid? PrintDesignId, BadgeBatchStatus Status, JsonElement OriginalInput, JsonElement NormalizedRows, int TotalJobs, int PendingJobs, int RunningJobs, int SucceededJobs, int FailedJobs, int CancelledJobs, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateBadgeBatchRequest(string Name, Guid EncoderId, Guid? TransformationId, Guid? PrintDesignId, JsonElement OriginalInput, JsonElement NormalizedRows, string? RequestedAgentId, string? RequestedDeviceId, int Priority = 0);

public sealed record BadgeBatchJobSummary(Guid BatchId, int TotalJobs, int PendingJobs, int RunningJobs, int SucceededJobs, int FailedJobs, int CancelledJobs)
{
    public BadgeBatchStatus Status => TotalJobs switch
    {
        0 => BadgeBatchStatus.Pending,
        _ when RunningJobs > 0 => BadgeBatchStatus.Running,
        _ when PendingJobs > 0 => SucceededJobs > 0 || FailedJobs > 0 || CancelledJobs > 0 ? BadgeBatchStatus.Running : BadgeBatchStatus.Pending,
        _ when FailedJobs > 0 => BadgeBatchStatus.Failed,
        _ when CancelledJobs > 0 && SucceededJobs == 0 => BadgeBatchStatus.Cancelled,
        _ => BadgeBatchStatus.Completed
    };
}

public sealed record BadgeJobResponse(Guid Id, Guid? TransformationId, Guid? BatchId, Guid? EncoderId, Guid? PrintDesignId, BadgeJobKind Kind, string? Source, BadgeJobStatus Status, JsonElement Input, JsonElement ResolvedVariables, JsonElement PlanSummary, JsonElement CommandAudit, string? CardUid, string? HardwareAgentId, string? DeviceId, string? ErrorMessage, DateTimeOffset RequestedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt);

public sealed record CreateBadgeJobRequest(Guid? EncoderId, Guid? TransformationId, Guid? PrintDesignId, string? AgentId, string? DeviceId, JsonElement Input, BadgeJobMode Mode = BadgeJobMode.Sync, int Priority = 0, string? Source = null, Guid? KioskSessionId = null);

public sealed record EncodingVariableRequest(string Name, VariableProviderRequest Provider, VariableFormatRequest Format);

public sealed record VariableProviderRequest(DesfireVariableProviderKind Type, string? Field = null, string? Value = null, string? SequenceName = null, long? InitialValue = null, Guid? SystemProviderId = null);

public sealed record VariableFormatRequest(DesfireVariableFormatKind Type, int? Length = null, string? Encoding = null, GenericWiegandFormatRequest? Wiegand = null);

public sealed record GenericWiegandFormatRequest(int BitLength, IReadOnlyList<WiegandFieldRequest> Fields, IReadOnlyList<WiegandParityRequest> Parity, string Output = "hex");

public sealed record WiegandFieldRequest(string Name, int Offset, int Length, WiegandFieldSourceKind Source, string? Field = null, string? Value = null, string? SequenceName = null, long? InitialValue = null);

public sealed record WiegandParityRequest(int Offset, WiegandParityKind Kind, int CoversOffset, int CoversLength);

public static class DesfireMapper
{
    public static ChipDesignResponse ToResponse(this ChipDesign design) => new(
        design.Id,
        design.Name,
        design.Version,
        design.Description,
        JsonSerializer.Deserialize<TemplateSpecification>(design.SpecificationJson, DesfireJson.Options)!,
        design.CreatedAt);

    public static TransformationResponse ToResponse(this Transformation transformation) => new(
        transformation.Id,
        transformation.Name,
        transformation.FromChipDesignName,
        transformation.FromBlank,
        transformation.ToChipDesignName,
        transformation.AlwaysReadUid,
        JsonSerializer.Deserialize<string[]>(transformation.RequiredVariablesJson, DesfireJson.Options) ?? [],
        JsonSerializer.Deserialize<string[]>(transformation.RequiredKeyGroupsJson, DesfireJson.Options) ?? [],
        JsonSerializer.Deserialize<TransformationVariableConfigRequest[]>(transformation.VariableConfigsJson, DesfireJson.Options) ?? [],
        transformation.CreatedAt,
        transformation.UpdatedAt);

    public static KeyDiversificationStrategyResponse ToResponse(this KeyDiversificationStrategyEntity strategy) => new(
        strategy.Id,
        strategy.Name,
        strategy.Algorithm,
        JsonSerializer.Deserialize<DiversificationInput[]>(strategy.InputsJson, DesfireJson.Options) ?? [],
        strategy.CreatedAt,
        strategy.UpdatedAt);

    public static KeyGroupResponse ToResponse(this KeyGroup group, IDesfireKeyProtector? keyProtector = null) => new(
        group.Id,
        group.Name,
        group.KeyType,
        group.Locked,
        group.DiversificationStrategyId,
        group.KeySets.OrderBy(keySet => keySet.KeySetId).Select(keySet => new KeyGroupKeySetResponse(
            keySet.KeySetId,
            keySet.Keys.OrderBy(key => key.KeyId).Select(key => new KeyGroupKeyResponse(
                key.KeyId,
                key.IsDiversified,
                keyProtector is null || group.Locked ? null : keyProtector.Unprotect(key.ProtectedValue))).ToArray())).ToArray(),
        group.CreatedAt,
        group.UpdatedAt);

    public static SystemProviderResponse ToResponse(this DesfireSystemProvider provider) => new(
        provider.Id,
        provider.Name,
        provider.ProviderType,
        provider.FixedValue,
        provider.InitialValue,
        provider.CurrentValue,
        provider.CreatedAt);

    public static EncoderResponse ToResponse(this DesfireEncoder encoder) => new(
        encoder.Id,
        encoder.Name,
        encoder.AgentId,
        encoder.DeviceId,
        encoder.SupportsEncoding,
        encoder.SupportsPrinting,
        encoder.Enabled,
        encoder.CreatedAt,
        encoder.UpdatedAt);

    public static EncoderResponse ToResponse(this DesfireEncoder encoder, HardwareDevice? device)
    {
        bool supportsEncoding = device is null ? encoder.SupportsEncoding : SupportsFullEncodingWorkflow(device.Capabilities);
        bool supportsPrinting = device is null ? encoder.SupportsPrinting : device.Capabilities.Contains("card.print", StringComparer.OrdinalIgnoreCase);

        return new EncoderResponse(
            encoder.Id,
            encoder.Name,
            encoder.AgentId,
            encoder.DeviceId,
            supportsEncoding,
            supportsPrinting,
            encoder.Enabled,
            encoder.CreatedAt,
            encoder.UpdatedAt);
    }

    public static BadgeBatchResponse ToResponse(this BadgeBatch batch, BadgeBatchJobSummary? summary = null) => new(
        batch.Id,
        batch.Name,
        batch.EncoderId,
        batch.TransformationId,
        batch.PrintDesignId,
        summary?.Status ?? batch.Status,
        JsonSerializer.Deserialize<JsonElement>(batch.OriginalInputJson, DesfireJson.Options),
        JsonSerializer.Deserialize<JsonElement>(batch.NormalizedRowsJson, DesfireJson.Options),
        summary?.TotalJobs ?? 0,
        summary?.PendingJobs ?? 0,
        summary?.RunningJobs ?? 0,
        summary?.SucceededJobs ?? 0,
        summary?.FailedJobs ?? 0,
        summary?.CancelledJobs ?? 0,
        batch.CreatedAt,
        batch.UpdatedAt);

    public static BadgeJobResponse ToResponse(this BadgeJob job) => new(
        job.Id,
        job.TransformationId,
        job.BatchId,
        job.EncoderId,
        job.PrintDesignId,
        job.Kind,
        job.Source,
        job.Status,
        JsonSerializer.Deserialize<JsonElement>(job.InputJson, DesfireJson.Options),
        JsonSerializer.Deserialize<JsonElement>(job.ResolvedVariablesJson, DesfireJson.Options),
        JsonSerializer.Deserialize<JsonElement>(job.PlanSummaryJson, DesfireJson.Options),
        JsonSerializer.Deserialize<JsonElement>(job.CommandAuditJson, DesfireJson.Options),
        job.CardUid,
        job.HardwareAgentId,
        job.DeviceId,
        job.ErrorMessage,
        job.RequestedAt,
        job.StartedAt,
        job.CompletedAt);

    private static bool SupportsFullEncodingWorkflow(IReadOnlyList<string> capabilities) =>
        capabilities.Contains("card.present", StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains("rfid.apdu.exchange", StringComparer.OrdinalIgnoreCase)
        && capabilities.Contains("card.eject", StringComparer.OrdinalIgnoreCase);
}
