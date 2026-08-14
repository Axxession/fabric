namespace Fabric.Server.Requirements.Domain;

public sealed class ZoneCompliance
{
    private ZoneCompliance() { }

    public Guid Id { get; private set; }
    public Guid EnforcementZoneId { get; private set; }
    public Guid IdentityId { get; private set; }
    public RequirementSubjectKind SubjectKind { get; private set; }
    public ZoneComplianceStatus CalculatedStatus { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public DateTimeOffset LastEvaluatedAt { get; private set; }
    public string ReasonSummary { get; private set; } = null!;
    public List<ZoneComplianceRequirementResult> RequirementResults { get; private set; } = [];

    public static ZoneCompliance Create(
        Guid enforcementZoneId,
        Guid identityId,
        RequirementSubjectKind subjectKind,
        ZoneComplianceStatus calculatedStatus,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        DateTimeOffset lastEvaluatedAt,
        string reasonSummary,
        IReadOnlyCollection<ZoneComplianceRequirementResult> requirementResults)
    {
        ZoneCompliance compliance = new()
        {
            Id = Guid.NewGuid(),
            EnforcementZoneId = enforcementZoneId,
            IdentityId = identityId,
            SubjectKind = subjectKind,
            CalculatedStatus = calculatedStatus,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            LastEvaluatedAt = lastEvaluatedAt,
            ReasonSummary = reasonSummary,
        };

        compliance.ReplaceRequirementResults(requirementResults);
        return compliance;
    }

    public void Update(
        RequirementSubjectKind subjectKind,
        ZoneComplianceStatus calculatedStatus,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        DateTimeOffset lastEvaluatedAt,
        string reasonSummary,
        IReadOnlyCollection<ZoneComplianceRequirementResult> requirementResults)
    {
        SubjectKind = subjectKind;
        CalculatedStatus = calculatedStatus;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        LastEvaluatedAt = lastEvaluatedAt;
        ReasonSummary = reasonSummary;
        ReplaceRequirementResults(requirementResults);
    }

    private void ReplaceRequirementResults(IReadOnlyCollection<ZoneComplianceRequirementResult> requirementResults)
    {
        RequirementResults.Clear();
        foreach (ZoneComplianceRequirementResult result in requirementResults)
        {
            result.ZoneComplianceId = Id;
            RequirementResults.Add(result);
        }
    }
}
