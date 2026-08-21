namespace Fabric.Server.Requirements.Domain;

public enum RequirementEvidenceErrors
{
    RequirementDefinitionNotFound,
    RequirementEvidenceNotFound,
    EvidenceKindNotAllowed,
    SummaryRequired,
    ValidUntilMustBeAfterValidFrom,
    FileTooLarge
}
