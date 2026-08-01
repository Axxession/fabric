namespace Fabric.Server.Reception.Domain;

public enum ReceptionErrors
{
    AlreadyOnboarded,
    ArrivalNotFound,
    ArrivalCodeConflictAcrossSubjects,
    NotYetOnboarded,
    AlreadyOffboarded,
    ArrivalOutsideKioskOnboardingWindow,
    InvalidStatus,
    MissingRequiredDocuments,
    InvalidIdentityVerificationMethod,
    NotAVisitor,
    SubjectAlreadyHasOnboardedArrival,
    ArrivalAssignedToDifferentLocation,
    ExpectedArrivalMustBeBeforeExpectedOffboard,
    GracePeriodMustNotBeNegative,
    AccessRuleAssignmentNotFound
}
