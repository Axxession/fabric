namespace Fabric.Server.Requirements.Domain;

public enum RequirementEvaluatorKind
{
    UploadedDocument,
    ExternalCheck,
    Escort,
    Computed
}

public enum RequirementSubjectKind
{
    Employee,
    Visitor,
    Contractor,
    Any
}

public enum RequirementEvidenceKind
{
    UploadedDocument,
    ExternalCheck,
    Onboarded,
    Computed,
    Escort
}

public enum RequirementEvidenceStatus
{
    Valid,
    Invalid
}

public enum ZoneComplianceStatus
{
    Compliant,
    NonCompliant
}

public enum RequirementResultStatus
{
    Fulfilled,
    Missing,
    Failed,
    Expired
}
