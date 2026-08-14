namespace Fabric.Server.Desfire.Domain;

public enum BadgeJobKind
{
    Single,
    Batch
}

public static class DesfireEncodingSources
{
    public const string Kiosk = "kiosk";
    public const string BadgeBatch = "badge-batch";
}

public enum BadgeJobStatus
{
    Pending,
    Claimed,
    Running,
    Succeeded,
    Failed,
    Cancelled,
    Timeout,
    DeviceUnavailable
}

public enum BadgeJobMode
{
    Sync,
    Queued
}

public enum DesfireVariableProviderKind
{
    Provided,
    Fixed,
    Sequence
}

public enum DesfireVariableFormatKind
{
    Hex,
    Text,
    UInt,
    PaddedDecimal,
    PaddedHex,
    GenericWiegand
}

public enum WiegandParityKind
{
    Even,
    Odd
}

public enum WiegandFieldSourceKind
{
    Provided,
    Fixed,
    Sequence
}

public enum BadgeBatchStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum KeyGroupError
{
    AlreadyLocked,
    CannotEditLocked,
    CannotChangeKeyStructure,
    DiversifiedKeyRequiresStrategy,
    EmptyKeySets
}

public enum TransformationVariableKind
{
    UserProvided,
    SystemProvided
}

public enum SystemVariableProviderKind
{
    Fixed,
    Sequence
}
