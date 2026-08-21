namespace Fabric.Server.Requirements.Domain;

public enum RequirementSubjectKind
{
    Employee,
    Visitor,
    Contractor,
    Any
}

public enum RequirementEvidenceKind
{
    Document,
    CourseCompletion,
    RequirementWaiver,
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

public enum ContextComplianceStatus
{
    Compliant,
    TemporarilyCompliant,
    NonCompliant,
}
