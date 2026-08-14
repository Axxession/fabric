namespace Fabric.Server.Contractors.Domain;

public enum JobTypeErrors
{
    JobTypeNotFound,
    CodeRequired,
    NameRequired,
    JobTypeCodeAlreadyExists,
    JobTypeAlreadyActive,
    JobTypeAlreadyInactive,
}
