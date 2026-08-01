using Fabric.Server.Infrastructure.Storage;

namespace Fabric.Server.Kiosk.Domain;

public sealed class KioskAsset
{
    private KioskAsset() { }

    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? LanguageCode { get; private set; }
    public KioskAssetKind Kind { get; private set; }
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long Size { get; private set; }
    public string RelativePath { get; private set; } = default!;
    public StoredFileVisibility Visibility { get; private set; }
    public string? UploadedByOid { get; private set; }
    public string? UploadedByEmail { get; private set; }
    public string? UploadedByDisplayName { get; private set; }
    public string? AltTextKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static KioskAsset Create(Guid id, Guid profileId, string name, string? languageCode, KioskAssetKind kind, string fileName, string contentType, long size, string relativePath, StoredFileVisibility visibility, string? uploadedByOid, string? uploadedByEmail, string? uploadedByDisplayName, string? altTextKey, DateTimeOffset now) => new()
    {
        Id = id,
        ProfileId = profileId,
        Name = name.Trim(),
        LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? null : KioskProfile.NormalizeLanguage(languageCode),
        Kind = kind,
        FileName = fileName.Trim(),
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
        Size = size,
        RelativePath = relativePath,
        Visibility = visibility,
        UploadedByOid = string.IsNullOrWhiteSpace(uploadedByOid) ? null : uploadedByOid.Trim(),
        UploadedByEmail = string.IsNullOrWhiteSpace(uploadedByEmail) ? null : uploadedByEmail.Trim(),
        UploadedByDisplayName = string.IsNullOrWhiteSpace(uploadedByDisplayName) ? null : uploadedByDisplayName.Trim(),
        AltTextKey = string.IsNullOrWhiteSpace(altTextKey) ? null : altTextKey.Trim(),
        CreatedAt = now
    };
}

public sealed class KioskDeviceAssignment
{
    private KioskDeviceAssignment() { }

    public Guid Id { get; private set; }
    public Guid KioskId { get; private set; }
    public string BindingKey { get; private set; } = default!;
    public string AgentId { get; private set; } = default!;
    public string DeviceId { get; private set; } = default!;
    public bool Enabled { get; private set; }
    public int Priority { get; private set; }

    public static KioskDeviceAssignment Create(Guid kioskId, string bindingKey, string agentId, string deviceId, bool enabled, int priority) => new()
    {
        Id = Guid.NewGuid(),
        KioskId = kioskId,
        BindingKey = KioskHardwareBinding.NormalizeBindingKey(bindingKey),
        AgentId = agentId.Trim().ToLowerInvariant(),
        DeviceId = deviceId.Trim().ToLowerInvariant(),
        Enabled = enabled,
        Priority = priority
    };
}

public sealed class KioskDevice
{
    private KioskDevice() { }

    public Guid Id { get; private set; }
    public Guid KioskId { get; private set; }
    public string Name { get; private set; } = default!;
    public KioskDeviceType Type { get; private set; }
    public int SlotNumber { get; private set; }
    public string AgentId { get; private set; } = default!;
    public string DeviceId { get; private set; } = default!;
    public bool Enabled { get; private set; }
    public bool CleanupOnSessionEnd { get; private set; }
    public int SortOrder { get; private set; }

    public static KioskDevice Create(Guid kioskId, string name, KioskDeviceType type, int slotNumber, string agentId, string deviceId, bool enabled, bool cleanupOnSessionEnd, int sortOrder) => new()
    {
        Id = Guid.NewGuid(),
        KioskId = kioskId,
        Name = name.Trim(),
        Type = type,
        SlotNumber = slotNumber,
        AgentId = agentId.Trim().ToLowerInvariant(),
        DeviceId = deviceId.Trim().ToLowerInvariant(),
        Enabled = enabled,
        CleanupOnSessionEnd = cleanupOnSessionEnd,
        SortOrder = sortOrder
    };
}

public sealed class KioskSession
{
    private KioskSession() { }

    public Guid Id { get; private set; }
    public Guid KioskId { get; private set; }
    public string? WorkflowInstanceId { get; private set; }
    public KioskSessionStatus Status { get; private set; }
    public string LanguageCode { get; private set; } = default!;
    public string? CurrentInstructionJson { get; private set; }
    public int CurrentInstructionVersion { get; private set; }
    public string? CurrentInstructionId { get; private set; }
    public string? TerminalTitle { get; private set; }
    public string? TerminalMessage { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset LastInteractionAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static KioskSession Start(Guid kioskId, string languageCode, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        KioskId = kioskId,
        Status = KioskSessionStatus.Starting,
        LanguageCode = KioskProfile.NormalizeLanguage(languageCode),
        CurrentInstructionVersion = 0,
        StartedAt = now,
        LastInteractionAt = now
    };

    public void MarkRunning(DateTimeOffset now)
    {
        Status = KioskSessionStatus.Running;
        LastInteractionAt = now;
    }

    public void ChangeLanguage(string languageCode, DateTimeOffset now)
    {
        LanguageCode = KioskProfile.NormalizeLanguage(languageCode);
        LastInteractionAt = now;
        CurrentInstructionVersion++;
    }

    public void AssignWorkflowInstance(string workflowInstanceId, DateTimeOffset now)
    {
        WorkflowInstanceId = workflowInstanceId.Trim();
        LastInteractionAt = now;
    }

    public void SetInstruction(string instructionId, string instructionJson, DateTimeOffset now)
    {
        CurrentInstructionId = instructionId;
        CurrentInstructionJson = instructionJson;
        CurrentInstructionVersion++;
        LastInteractionAt = now;
    }

    public void ClearInstruction(DateTimeOffset now)
    {
        CurrentInstructionId = null;
        CurrentInstructionJson = null;
        CurrentInstructionVersion++;
        LastInteractionAt = now;
    }

    public void MarkCompleted(DateTimeOffset now, string? terminalTitle = null, string? terminalMessage = null) => SetTerminalStatus(KioskSessionStatus.Completed, now, terminalTitle, terminalMessage);
    public void Cancel(DateTimeOffset now, string? terminalTitle = null, string? terminalMessage = null) => SetTerminalStatus(KioskSessionStatus.Cancelled, now, terminalTitle, terminalMessage);
    public void Fail(DateTimeOffset now, string? terminalTitle = null, string? terminalMessage = null) => SetTerminalStatus(KioskSessionStatus.Failed, now, terminalTitle, terminalMessage);
    public void Timeout(DateTimeOffset now, string? terminalTitle = null, string? terminalMessage = null) => SetTerminalStatus(KioskSessionStatus.TimedOut, now, terminalTitle, terminalMessage);

    private void SetTerminalStatus(KioskSessionStatus status, DateTimeOffset now, string? terminalTitle, string? terminalMessage)
    {
        Status = status;
        TerminalTitle = string.IsNullOrWhiteSpace(terminalTitle) ? null : terminalTitle.Trim();
        TerminalMessage = string.IsNullOrWhiteSpace(terminalMessage) ? null : terminalMessage.Trim();
        LastInteractionAt = now;
        CompletedAt = now;
    }
}
