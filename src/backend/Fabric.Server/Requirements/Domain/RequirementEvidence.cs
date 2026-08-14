using Fabric.Server.Core;

namespace Fabric.Server.Requirements.Domain;

public sealed class RequirementEvidence
{
    private RequirementEvidence() { }

    public Guid Id { get; private set; }
    public Guid IdentityId { get; private set; }
    public Guid RequirementDefinitionId { get; private set; }
    public RequirementEvidenceKind EvidenceKind { get; private set; }
    public RequirementEvidenceStatus Status { get; private set; }
    public DateTimeOffset? ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public string? SourceReference { get; private set; }
    public string Summary { get; private set; } = null!;
    public bool IsSensitive { get; private set; }
    public DateTimeOffset VerifiedAt { get; private set; }
    public string? FileName { get; private set; }
    public byte[]? Content { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<RequirementEvidence, RequirementEvidenceErrors> Create(
        Guid identityId,
        Guid requirementDefinitionId,
        RequirementEvidenceKind evidenceKind,
        RequirementEvidenceStatus status,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        string? sourceReference,
        string summary,
        bool isSensitive,
        DateTimeOffset verifiedAt,
        string? fileName,
        byte[]? content,
        DateTimeOffset now)
    {
        Result<RequirementEvidenceErrors> validation = Validate(validFrom, validUntil, summary);
        if (validation.IsFailure(out RequirementEvidenceErrors error))
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(error);

        return Result.Success<RequirementEvidence, RequirementEvidenceErrors>(new RequirementEvidence
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            RequirementDefinitionId = requirementDefinitionId,
            EvidenceKind = evidenceKind,
            Status = status,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            SourceReference = NormalizeOptional(sourceReference),
            Summary = summary.Trim(),
            IsSensitive = isSensitive,
            VerifiedAt = verifiedAt,
            FileName = NormalizeOptional(fileName),
            Content = content,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    public Result<RequirementEvidenceErrors> Update(
        RequirementEvidenceStatus status,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        string? sourceReference,
        string summary,
        bool isSensitive,
        DateTimeOffset verifiedAt,
        string? fileName,
        byte[]? content,
        DateTimeOffset now)
    {
        Result<RequirementEvidenceErrors> validation = Validate(validFrom, validUntil, summary);
        if (validation.IsFailure(out RequirementEvidenceErrors error))
            return Result.Failure(error);

        Status = status;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        SourceReference = NormalizeOptional(sourceReference);
        Summary = summary.Trim();
        IsSensitive = isSensitive;
        VerifiedAt = verifiedAt;
        FileName = NormalizeOptional(fileName);
        Content = content;
        UpdatedAt = now;
        return Result.Success<RequirementEvidenceErrors>();
    }

    private static Result<RequirementEvidenceErrors> Validate(DateTimeOffset? validFrom, DateTimeOffset? validUntil, string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return Result.Failure(RequirementEvidenceErrors.SummaryRequired);

        if (validFrom.HasValue && validUntil.HasValue && validUntil.Value <= validFrom.Value)
            return Result.Failure(RequirementEvidenceErrors.ValidUntilMustBeAfterValidFrom);

        return Result.Success<RequirementEvidenceErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
