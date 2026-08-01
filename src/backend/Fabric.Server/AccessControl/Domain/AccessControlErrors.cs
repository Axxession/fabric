namespace Fabric.Server.AccessControl.Domain;

public enum AccessControlErrors
{
    SystemNotFound,
    SystemNameAlreadyExists,
    SystemProviderNotSupported,
    ConfigInvalid,
    AccessItemNotFound,
    AccessItemNameAlreadyExists,
    AccessLevelTargetNotFound,
    AccessLevelTargetAlreadyExists,
    LocationNotFound,
    LocationAlreadyLinked,
    SystemLocationNotFound,
    SiteNotFoundInMetadata,
    AccessRuleNotFoundInMetadata,
    AccessLevelTargetSystemMismatch,
    IdentityNotFound,
    PACSAssignmentNotFound,
    NoAccessLevelTargetsResolved,
    AccessControlSystemInactive,
    PACSSubjectNotFound,
    PACSSubjectProvisioningNotFound,
    CredentialTypeTargetNotFound,
    CredentialTypeTargetAlreadyExists
}
