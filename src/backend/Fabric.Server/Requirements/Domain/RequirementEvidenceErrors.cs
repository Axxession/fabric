namespace Fabric.Server.Requirements.Domain;

public enum RequirementEvidenceErrors
{
    RequirementEvidenceNotFound,
    SummaryRequired,
    ValidUntilMustBeAfterValidFrom,
    FileTooLarge
}
