namespace Fabric.Server.Requirements.Domain;

public enum RequirementFulfillmentKind
{
    Document,
    Learning,
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
    Escort,
    LearningCourseCompletion,
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
