namespace Fabric.Server.Desfire.Domain;

public sealed class BadgeBatch
{
    private BadgeBatch() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid? EncoderId { get; private set; }
    public Guid? TransformationId { get; private set; }
    public Guid? PrintDesignId { get; private set; }
    public BadgeBatchStatus Status { get; private set; }
    public string OriginalInputJson { get; private set; } = "{}";
    public string NormalizedRowsJson { get; private set; } = "[]";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static BadgeBatch Create(string name, Guid encoderId, Guid? transformationId, Guid? printDesignId, string originalInputJson, string normalizedRowsJson, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        EncoderId = encoderId,
        TransformationId = transformationId,
        PrintDesignId = printDesignId,
        Status = BadgeBatchStatus.Pending,
        OriginalInputJson = originalInputJson,
        NormalizedRowsJson = normalizedRowsJson,
        CreatedAt = now,
        UpdatedAt = now
    };
}

public sealed class BadgeJob
{
    private BadgeJob() { }

    public Guid Id { get; private set; }
    public Guid? TransformationId { get; private set; }
    public Guid? BatchId { get; private set; }
    public Guid? EncoderId { get; private set; }
    public Guid? PrintDesignId { get; private set; }
    public Guid? KioskSessionId { get; private set; }
    public BadgeJobKind Kind { get; private set; }
    public string? Source { get; private set; }
    public BadgeJobStatus Status { get; private set; }
    public string InputJson { get; private set; } = "{}";
    public string ResolvedVariablesJson { get; private set; } = "{}";
    public string PlanSummaryJson { get; private set; } = "{}";
    public string CommandAuditJson { get; private set; } = "[]";
    public string? CardUid { get; private set; }
    public string? HardwareAgentId { get; private set; }
    public string? DeviceId { get; private set; }
    public string? RequestedAgentId { get; private set; }
    public string? RequestedDeviceId { get; private set; }
    public string VariableConfigJson { get; private set; } = "[]";
    public int Priority { get; private set; }
    public int AttemptCount { get; private set; }
    public string? ClaimedBy { get; private set; }
    public DateTimeOffset? ClaimedAt { get; private set; }
    public DateTimeOffset? ClaimExpiresAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static BadgeJob Create(Guid? transformationId, Guid? batchId, Guid? encoderId, Guid? printDesignId, Guid? kioskSessionId, BadgeJobKind kind, string? source, string inputJson, string variableConfigJson, string? requestedAgentId, string? requestedDeviceId, int priority, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        TransformationId = transformationId,
        BatchId = batchId,
        EncoderId = encoderId,
        PrintDesignId = printDesignId,
        KioskSessionId = kioskSessionId,
        Kind = kind,
        Source = NormalizeOptional(source),
        Status = BadgeJobStatus.Pending,
        InputJson = inputJson,
        VariableConfigJson = variableConfigJson,
        RequestedAgentId = NormalizeOptional(requestedAgentId),
        RequestedDeviceId = NormalizeOptional(requestedDeviceId),
        Priority = priority,
        RequestedAt = now
    };

    public bool CanClaim(DateTimeOffset now) => Status == BadgeJobStatus.Pending || (Status == BadgeJobStatus.Claimed && ClaimExpiresAt <= now);

    public void Claim(string workerId, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        Status = BadgeJobStatus.Claimed;
        ClaimedBy = workerId;
        ClaimedAt = now;
        ClaimExpiresAt = expiresAt;
        AttemptCount++;
    }

    public void Start(string hardwareAgentId, string deviceId, DateTimeOffset now)
    {
        HardwareAgentId = hardwareAgentId.Trim().ToLowerInvariant();
        DeviceId = deviceId.Trim().ToLowerInvariant();
        Status = BadgeJobStatus.Running;
        StartedAt = now;
    }

    public void Complete(string? cardUid, string resolvedVariablesJson, string planSummaryJson, string commandAuditJson, DateTimeOffset now)
    {
        if (Status == BadgeJobStatus.Cancelled)
            return;

        CardUid = NormalizeCardUid(cardUid);
        ResolvedVariablesJson = resolvedVariablesJson;
        PlanSummaryJson = planSummaryJson;
        CommandAuditJson = commandAuditJson;
        Status = BadgeJobStatus.Succeeded;
        CompletedAt = now;
        ClaimExpiresAt = null;
    }

    public void Fail(BadgeJobStatus status, string errorMessage, string commandAuditJson, DateTimeOffset now, string? cardUid = null)
    {
        if (Status == BadgeJobStatus.Cancelled)
            return;

        Status = status;
        ErrorMessage = errorMessage;
        CardUid = NormalizeCardUid(cardUid) ?? CardUid;
        CommandAuditJson = commandAuditJson;
        CompletedAt = now;
        ClaimExpiresAt = null;
    }

    public void Requeue(string errorMessage, DateTimeOffset now)
    {
        if (Status == BadgeJobStatus.Cancelled)
            return;

        Status = BadgeJobStatus.Pending;
        ErrorMessage = errorMessage;
        ClaimedBy = null;
        ClaimExpiresAt = null;
        CompletedAt = null;
    }

    public void Cancel(string? errorMessage, string commandAuditJson, DateTimeOffset now)
    {
        Status = BadgeJobStatus.Cancelled;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Encoding run cancelled." : errorMessage.Trim();
        CommandAuditJson = commandAuditJson;
        CompletedAt = now;
        ClaimExpiresAt = null;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string? NormalizeCardUid(string? cardUid)
    {
        if (string.IsNullOrWhiteSpace(cardUid))
            return null;

        string normalized = cardUid.Trim();
        return normalized is "Unknown" or "Unkown" ? null : normalized;
    }
}

public sealed class DesfireEncoder
{
    private DesfireEncoder() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string AgentId { get; private set; } = default!;
    public string DeviceId { get; private set; } = default!;
    public bool SupportsEncoding { get; private set; }
    public bool SupportsPrinting { get; private set; }
    public bool Enabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static DesfireEncoder Create(string name, string agentId, string deviceId, bool supportsEncoding, bool supportsPrinting, bool enabled, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        AgentId = agentId.Trim().ToLowerInvariant(),
        DeviceId = deviceId.Trim().ToLowerInvariant(),
        SupportsEncoding = supportsEncoding,
        SupportsPrinting = supportsPrinting,
        Enabled = enabled,
        CreatedAt = now,
        UpdatedAt = now
    };

    public void Update(string name, string agentId, string deviceId, bool supportsEncoding, bool supportsPrinting, bool enabled, DateTimeOffset now)
    {
        Name = name.Trim();
        AgentId = agentId.Trim().ToLowerInvariant();
        DeviceId = deviceId.Trim().ToLowerInvariant();
        SupportsEncoding = supportsEncoding;
        SupportsPrinting = supportsPrinting;
        Enabled = enabled;
        UpdatedAt = now;
    }

    public void UpdateMetadata(string name, bool supportsEncoding, bool supportsPrinting, bool enabled, DateTimeOffset now)
    {
        Name = name.Trim();
        SupportsEncoding = supportsEncoding;
        SupportsPrinting = supportsPrinting;
        Enabled = enabled;
        UpdatedAt = now;
    }
}

public sealed class DesfireVariableSequence
{
    private DesfireVariableSequence() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public long NextValue { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static DesfireVariableSequence Create(string name, long nextValue, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        NextValue = nextValue,
        CreatedAt = now,
        UpdatedAt = now
    };

    public long TakeNext(DateTimeOffset now)
    {
        long value = NextValue;
        NextValue++;
        UpdatedAt = now;
        return value;
    }
}

public sealed class DesfireSystemProvider
{
    private DesfireSystemProvider() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public SystemVariableProviderKind ProviderType { get; private set; }
    public string? FixedValue { get; private set; }
    public long? InitialValue { get; private set; }
    public long? CurrentValue { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static DesfireSystemProvider Create(string name, SystemVariableProviderKind providerType, string? fixedValue, long? initialValue, DateTimeOffset now) => providerType switch
    {
        SystemVariableProviderKind.Fixed => new DesfireSystemProvider
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            ProviderType = providerType,
            FixedValue = fixedValue ?? string.Empty,
            CreatedAt = now
        },
        SystemVariableProviderKind.Sequence => new DesfireSystemProvider
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            ProviderType = providerType,
            InitialValue = initialValue ?? 1,
            CurrentValue = initialValue ?? 1,
            CreatedAt = now
        },
        _ => throw new InvalidOperationException($"Unsupported system provider type '{providerType}'.")
    };

    public long TakeNextValue()
    {
        if (ProviderType != SystemVariableProviderKind.Sequence || CurrentValue is null)
            throw new InvalidOperationException("Only sequence system providers can produce next values.");

        long value = CurrentValue.Value;
        CurrentValue++;
        return value;
    }
}

public sealed class DesfireDeviceLease
{
    private DesfireDeviceLease() { }

    public Guid Id { get; private set; }
    public string AgentId { get; private set; } = default!;
    public string DeviceId { get; private set; } = default!;
    public Guid BadgeJobId { get; private set; }
    public DateTimeOffset AcquiredAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }

    public static DesfireDeviceLease Create(string agentId, string deviceId, Guid badgeJobId, DateTimeOffset now, DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        AgentId = agentId.Trim().ToLowerInvariant(),
        DeviceId = deviceId.Trim().ToLowerInvariant(),
        BadgeJobId = badgeJobId,
        AcquiredAt = now,
        ExpiresAt = expiresAt
    };

    public void Release(DateTimeOffset now) => ReleasedAt = now;
}
