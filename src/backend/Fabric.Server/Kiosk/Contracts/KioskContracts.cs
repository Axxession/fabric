using Fabric.Server.Kiosk.Domain;
using Fabric.Server.Infrastructure.Storage;

namespace Fabric.Server.Kiosk.Contracts;

public sealed record KioskProfileResponse(Guid Id, string Name, string DefaultLanguageCode, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateKioskProfileRequest(string Name, string DefaultLanguageCode);

public sealed record UpdateKioskProfileRequest(string Name, string DefaultLanguageCode);

public sealed record KioskResponse(Guid Id, Guid ProfileId, string Name, KioskMode Mode, bool ShowDetailedErrors, string? WorkflowDefinitionId, Guid? ActiveSessionId, KioskSessionStatus? ActiveSessionStatus, DateTimeOffset? ActiveSessionStartedAt, DateTimeOffset? LastSeenAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateKioskRequest(string Name, Guid ProfileId, bool ShowDetailedErrors = false);

public sealed record UpdateKioskRequest(string Name, Guid ProfileId, bool ShowDetailedErrors = false);

public sealed record KioskKeyResponse(KioskResponse Kiosk, string ApiKey);

public sealed record AssignKioskWorkflowRequest(string WorkflowDefinitionId);

public sealed record KioskProfileLanguageResponse(Guid Id, Guid ProfileId, string LanguageCode, string DisplayName, bool IsDefault, int SortOrder);

public sealed record UpsertKioskProfileLanguagesRequest(IReadOnlyList<UpsertKioskProfileLanguageRequest> Languages);

public sealed record UpsertKioskProfileLanguageRequest(string LanguageCode, string DisplayName, bool IsDefault, int SortOrder);

public sealed record KioskTranslationResponse(Guid Id, Guid ProfileId, string LanguageCode, string Key, string Value);

public sealed record UpsertKioskTranslationsRequest(IReadOnlyList<UpsertKioskTranslationRequest> Translations);

public sealed record UpsertKioskTranslationRequest(string LanguageCode, string Key, string Value);

public sealed record KioskThemeTokenResponse(Guid Id, Guid ProfileId, string Key, string Value);

public sealed record UpsertKioskThemeRequest(IReadOnlyList<UpsertKioskThemeTokenRequest> Tokens);

public sealed record UpsertKioskThemeTokenRequest(string Key, string Value);

public sealed record KioskWelcomeSettingsResponse(Guid ProfileId, string TitleKey, string? SubtitleKey, string StartButtonKey, string? BackgroundAssetName, string? LogoAssetName);

public sealed record UpsertKioskWelcomeSettingsRequest(string TitleKey, string? SubtitleKey, string StartButtonKey, string? BackgroundAssetName, string? LogoAssetName);

public sealed record KioskHardwareBindingResponse(Guid Id, Guid ProfileId, string BindingKey, string DisplayName, string RequiredCapability, bool Required, bool CleanupOnSessionEnd, int SortOrder);

public sealed record UpsertKioskHardwareBindingsRequest(IReadOnlyList<UpsertKioskHardwareBindingRequest> Bindings);

public sealed record UpsertKioskHardwareBindingRequest(string BindingKey, string DisplayName, string RequiredCapability, bool Required, bool CleanupOnSessionEnd, int SortOrder);

public sealed record KioskDeviceAssignmentResponse(Guid Id, Guid KioskId, string BindingKey, string AgentId, string DeviceId, bool Enabled, int Priority);

public sealed record UpsertKioskDeviceAssignmentsRequest(IReadOnlyList<UpsertKioskDeviceAssignmentRequest> Assignments);

public sealed record UpsertKioskDeviceAssignmentRequest(string BindingKey, string AgentId, string DeviceId, bool Enabled, int Priority);

public sealed record KioskDeviceResponse(Guid Id, Guid KioskId, string Name, KioskDeviceType Type, int SlotNumber, string AgentId, string DeviceId, bool Enabled, bool CleanupOnSessionEnd, int SortOrder);

public sealed record UpsertKioskDevicesRequest(IReadOnlyList<UpsertKioskDeviceRequest> Devices);

public sealed record UpsertKioskDeviceRequest(string Name, KioskDeviceType Type, int SlotNumber, string AgentId, string DeviceId, bool Enabled, bool CleanupOnSessionEnd, int SortOrder);

public sealed record KioskAssetResponse(Guid Id, Guid ProfileId, string Name, string? LanguageCode, KioskAssetKind Kind, string FileName, string ContentType, long Size, StoredFileVisibility Visibility, string? UploadedByOid, string? UploadedByEmail, string? UploadedByDisplayName, string? AltTextKey, DateTimeOffset CreatedAt);

public sealed record CreateKioskAssetRequest(string Name, string? LanguageCode, KioskAssetKind Kind, StoredFileVisibility Visibility, string? AltTextKey);

public sealed record KioskSessionResponse(Guid Id, Guid KioskId, KioskSessionStatus Status, string LanguageCode, int CurrentInstructionVersion, string? CurrentInstructionId, string? TerminalTitle, string? TerminalMessage, DateTimeOffset StartedAt, DateTimeOffset LastInteractionAt, DateTimeOffset? CompletedAt);

public sealed record KioskConfigResponse(KioskResponse Kiosk, KioskProfileResponse Profile, IReadOnlyList<KioskProfileLanguageResponse> Languages, KioskWelcomeSettingsResponse? Welcome, ResolvedKioskWelcomeResponse? ResolvedWelcome, IReadOnlyDictionary<string, string> Theme);

public sealed record ResolvedKioskWelcomeResponse(string Title, string? Subtitle, string StartButton, string? BackgroundUrl, string? LogoUrl);

public sealed record StartKioskSessionRequest(string? LanguageCode);

public sealed record ChangeKioskLanguageRequest(string LanguageCode);

public sealed record KioskHeartbeatRequest(DateTimeOffset ReportedAt);

public sealed record KioskInstructionResponse(Guid SessionId, KioskSessionStatus Status, int Version, string? InstructionId, string? InstructionJson, string? TerminalTitle, string? TerminalMessage);

public sealed record CancelCurrentKioskSessionRequest(KioskSessionCancellationSource Source);

public sealed record SubmitKioskInstructionResponseRequest(Dictionary<string, string> Values);

public static class KioskMapper
{
    public static KioskProfileResponse ToResponse(this KioskProfile profile) => new(profile.Id, profile.Name, profile.DefaultLanguageCode, profile.CreatedAt, profile.UpdatedAt);

    public static KioskResponse ToResponse(this Domain.Kiosk kiosk, KioskSession? activeSession = null) => new(kiosk.Id, kiosk.ProfileId, kiosk.Name, kiosk.Mode, kiosk.ShowDetailedErrors, kiosk.WorkflowDefinitionId, activeSession?.Id, activeSession?.Status, activeSession?.StartedAt, kiosk.LastSeenAt, kiosk.CreatedAt, kiosk.UpdatedAt);

    public static KioskProfileLanguageResponse ToResponse(this KioskProfileLanguage language) => new(language.Id, language.ProfileId, language.LanguageCode, language.DisplayName, language.IsDefault, language.SortOrder);

    public static KioskTranslationResponse ToResponse(this KioskTranslation translation) => new(translation.Id, translation.ProfileId, translation.LanguageCode, translation.Key, translation.Value);

    public static KioskThemeTokenResponse ToResponse(this KioskThemeToken token) => new(token.Id, token.ProfileId, token.Key, token.Value);

    public static KioskWelcomeSettingsResponse ToResponse(this KioskWelcomeSettings settings) => new(settings.ProfileId, settings.TitleKey, settings.SubtitleKey, settings.StartButtonKey, settings.BackgroundAssetName, settings.LogoAssetName);

    public static KioskHardwareBindingResponse ToResponse(this KioskHardwareBinding binding) => new(binding.Id, binding.ProfileId, binding.BindingKey, binding.DisplayName, binding.RequiredCapability, binding.Required, binding.CleanupOnSessionEnd, binding.SortOrder);

    public static KioskDeviceAssignmentResponse ToResponse(this KioskDeviceAssignment assignment) => new(assignment.Id, assignment.KioskId, assignment.BindingKey, assignment.AgentId, assignment.DeviceId, assignment.Enabled, assignment.Priority);

    public static KioskDeviceResponse ToResponse(this KioskDevice device) => new(device.Id, device.KioskId, device.Name, device.Type, device.SlotNumber, device.AgentId, device.DeviceId, device.Enabled, device.CleanupOnSessionEnd, device.SortOrder);

    public static KioskAssetResponse ToResponse(this KioskAsset asset) => new(asset.Id, asset.ProfileId, asset.Name, asset.LanguageCode, asset.Kind, asset.FileName, asset.ContentType, asset.Size, asset.Visibility, asset.UploadedByOid, asset.UploadedByEmail, asset.UploadedByDisplayName, asset.AltTextKey, asset.CreatedAt);

    public static KioskSessionResponse ToResponse(this KioskSession session) => new(session.Id, session.KioskId, session.Status, session.LanguageCode, session.CurrentInstructionVersion, session.CurrentInstructionId, session.TerminalTitle, session.TerminalMessage, session.StartedAt, session.LastInteractionAt, session.CompletedAt);
}
