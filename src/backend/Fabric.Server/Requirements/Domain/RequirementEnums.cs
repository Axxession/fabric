namespace Fabric.Server.Requirements.Domain;

public enum RequirementEvaluatorKind
{
    UploadedDocument,
    Escort
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
    Onboarded,
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
