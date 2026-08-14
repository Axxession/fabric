namespace Fabric.Server.Contractors.Domain;

public enum ContractorJobErrors
{
    ContractorJobNotFound,
    AssignmentNotFound,
    CompanyNotFound,
    JobTypeNotFound,
    LocationNotFound,
    ContractorNotFound,
    ContractorCompanyMismatch,
    NameRequired,
    PlannedEndMustBeAfterStart,
    AssignmentUntilMustBeAfterFrom,
    AssignmentEndsAfterJobEnds,
    ContractorJobAlreadyActive,
    ContractorJobCompleted,
    ContractorJobCancelled,
    AssignmentAlreadyActive,
    AssignmentCompleted,
    AssignmentCancelled,
}
