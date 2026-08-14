using Fabric.Server.Core;

namespace Fabric.Server.Requirements.Domain;

public sealed class RequirementDefinition
{
    private RequirementDefinition() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public RequirementEvaluatorKind EvaluatorKind { get; private set; }
    public bool IsSensitive { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<RequirementDefinition, RequirementDefinitionErrors> Create(
        string code,
        string name,
        string? description,
        RequirementEvaluatorKind evaluatorKind,
        bool isSensitive,
        DateTimeOffset now)
    {
        Result<RequirementDefinitionErrors> validation = Validate(code, name);
        if (validation.IsFailure(out RequirementDefinitionErrors error))
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(error);

        return Result.Success<RequirementDefinition, RequirementDefinitionErrors>(new RequirementDefinition
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Description = NormalizeOptional(description),
            EvaluatorKind = evaluatorKind,
            IsSensitive = isSensitive,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    public Result<RequirementDefinitionErrors> Update(
        string code,
        string name,
        string? description,
        RequirementEvaluatorKind evaluatorKind,
        bool isSensitive,
        DateTimeOffset now)
    {
        Result<RequirementDefinitionErrors> validation = Validate(code, name);
        if (validation.IsFailure(out RequirementDefinitionErrors error))
            return Result.Failure(error);

        Code = code.Trim();
        Name = name.Trim();
        Description = NormalizeOptional(description);
        EvaluatorKind = evaluatorKind;
        IsSensitive = isSensitive;
        UpdatedAt = now;
        return Result.Success<RequirementDefinitionErrors>();
    }

    public Result<RequirementDefinitionErrors> Activate(DateTimeOffset now)
    {
        if (IsActive)
            return Result.Failure(RequirementDefinitionErrors.RequirementDefinitionAlreadyActive);

        IsActive = true;
        UpdatedAt = now;
        return Result.Success<RequirementDefinitionErrors>();
    }

    public Result<RequirementDefinitionErrors> Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
            return Result.Failure(RequirementDefinitionErrors.RequirementDefinitionAlreadyInactive);

        IsActive = false;
        UpdatedAt = now;
        return Result.Success<RequirementDefinitionErrors>();
    }

    private static Result<RequirementDefinitionErrors> Validate(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure(RequirementDefinitionErrors.CodeRequired);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(RequirementDefinitionErrors.NameRequired);

        return Result.Success<RequirementDefinitionErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
