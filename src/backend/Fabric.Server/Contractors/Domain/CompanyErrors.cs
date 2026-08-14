namespace Fabric.Server.Contractors.Domain;

public enum CompanyErrors
{
    CompanyNotFound,
    CodeRequired,
    NameRequired,
    CompanyCodeAlreadyExists,
    CompanyNumberAlreadyExists,
    CompanyAlreadyActive,
    CompanyAlreadyInactive,
}
