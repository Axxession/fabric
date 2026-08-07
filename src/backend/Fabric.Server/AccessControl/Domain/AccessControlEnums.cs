namespace Fabric.Server.AccessControl.Domain;

public enum AccessControlProviderKind
{
    Unipass
}

public enum AccessControlSystemStatus
{
    Active,
    Inactive
}

public enum AnomalyBlockMode
{
    WarnOnly,
    BlockProvisioning
}

public enum AccessItemStatus
{
    Active,
    Inactive
}

public enum PACSAssignmentStatus
{
    Pending,
    Provisioned,
    Failed,
    Revoked
}

public enum PACSAssignmentDurationKind
{
    Permanent,
    Temporary
}

public enum ProvisioningTiming
{
    Eager,
    AtValidFrom
}

public enum PACSProvisioningStatus
{
    Pending,
    Provisioned,
    Failed,
    Revoked
}

public enum CredentialPACSAssignmentStatus
{
    Pending,
    Provisioned,
    FailedRetryable,
    FailedTerminal,
    Revoked
}

public enum PACSSubjectState
{
    Active,
    Blocked,
    Archived
}

public enum PACSSubjectConformityStatus
{
    Unknown,
    Conform,
    Anomaly
}

public enum PACSSubjectProvisioningBlockStatus
{
    ProvisioningAllowed,
    BlockedManual,
    BlockedByAnomaly
}

public enum PACSSubjectProvisioningStatus
{
    Pending,
    InProgress,
    Failed
}

public enum PACSSubjectProvisioningReason
{
    ProfileChanged,
    EmployeeLeave,
    EmployeeSuspension,
    EmployeeLifecycleRestored,
    ArchiveRequested,
    Manual
}

public enum PACSSubjectProvisioningSourceKind
{
    EmployeeLifecycleSaga,
    ContractorLifecycleSaga,
    VisitorLifecycleSaga,
    Manual
}
