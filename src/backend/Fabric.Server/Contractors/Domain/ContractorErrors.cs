namespace Fabric.Server.Contractors.Domain;

public enum ContractorErrors
{
    ContractorNotFound,
    CompanyNotFound,
    IdentityNotFound,
    ContractorAlreadyLinkedToDifferentIdentity,
    FirstNameRequired,
    LastNameRequired,
    ContractorAlreadyArchived,
    ContractorNotArchived,
}
