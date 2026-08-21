namespace Fabric.Server.Requirements.Domain;

public enum RequirementDefinitionErrors
{
    CodeRequired,
    NameRequired,
    AllowedEvidenceKindsRequired,
    RequirementDefinitionNotFound,
    RequirementDefinitionInUse,
    RequirementDefinitionAlreadyActive,
    RequirementDefinitionAlreadyInactive
}
