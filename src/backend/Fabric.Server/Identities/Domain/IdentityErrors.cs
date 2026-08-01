namespace Fabric.Server.Identities.Domain;

public enum IdentityErrors
{
    IdentityNotFound,
    FirstNameRequired,
    LastNameRequired,
    AffiliationEffectiveUntilMustBeAfterEffectiveFrom,
    VisitorAffiliationAlreadyExists,
    EmployeeAffiliationAlreadyExists,
    ContractorAffiliationAlreadyExists,
    VisitorAlreadyLinkedToDifferentIdentity,
    EmployeeAlreadyLinkedToDifferentIdentity,
    ContractorAlreadyLinkedToDifferentIdentity,
    AffiliationAlreadyEnded,
}
