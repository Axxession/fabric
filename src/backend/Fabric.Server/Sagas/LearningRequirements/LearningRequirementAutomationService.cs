using Fabric.Server.Core;
using Fabric.Server.Learning.Application;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.LearningRequirements;

public sealed class LearningRequirementAutomationService(
    SagasDbContext db,
    RequirementsDbContext requirementsDb,
    LearningDbContext learningDb,
    EnrollmentService enrollmentService,
    RequirementsService requirementsService,
    GrantRequirementsService grantRequirementsService)
{
    public async Task<Result<LearningRequirementRule, string>> CreateRuleAsync(Guid requirementDefinitionId, Guid courseId, LearningRequirementSatisfactionMode satisfactionMode, decimal? minimumScore, CancellationToken cancellationToken = default)
    {
        RequirementDefinition? requirement = await requirementsDb.RequirementDefinitions.SingleOrDefaultAsync(item => item.Id == requirementDefinitionId, cancellationToken);
        if (requirement is null)
            return Result.Failure<LearningRequirementRule, string>("Requirement definition not found.");
        if (!requirement.AllowsEvidenceKind(RequirementEvidenceKind.CourseCompletion))
            return Result.Failure<LearningRequirementRule, string>("Requirement definition is not learning-fulfillable.");
        if (!await learningDb.Courses.AnyAsync(item => item.Id == courseId, cancellationToken))
            return Result.Failure<LearningRequirementRule, string>("Course not found.");
        if (satisfactionMode == LearningRequirementSatisfactionMode.MinimumScore && !minimumScore.HasValue)
            return Result.Failure<LearningRequirementRule, string>("Minimum score is required for minimum-score satisfaction mode.");

        bool exists = await db.LearningRequirementRules.AnyAsync(item => item.RequirementDefinitionId == requirementDefinitionId && item.CourseId == courseId, cancellationToken);
        if (exists)
            return Result.Failure<LearningRequirementRule, string>("Rule already exists.");

        LearningRequirementRule rule = new()
        {
            Id = Guid.NewGuid(),
            RequirementDefinitionId = requirementDefinitionId,
            CourseId = courseId,
            SatisfactionMode = satisfactionMode,
            MinimumScore = minimumScore,
            IsEnabled = true,
        };

        db.LearningRequirementRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<LearningRequirementRule, string>(rule);
    }

    public async Task<Result<LearningRequirementRule, string>> UpdateRuleAsync(Guid id, Guid requirementDefinitionId, Guid courseId, LearningRequirementSatisfactionMode satisfactionMode, decimal? minimumScore, CancellationToken cancellationToken = default)
    {
        LearningRequirementRule? rule = await db.LearningRequirementRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null)
            return Result.Failure<LearningRequirementRule, string>("Rule not found.");

        RequirementDefinition? requirement = await requirementsDb.RequirementDefinitions.SingleOrDefaultAsync(item => item.Id == requirementDefinitionId, cancellationToken);
        if (requirement is null)
            return Result.Failure<LearningRequirementRule, string>("Requirement definition not found.");
        if (!requirement.AllowsEvidenceKind(RequirementEvidenceKind.CourseCompletion))
            return Result.Failure<LearningRequirementRule, string>("Requirement definition is not learning-fulfillable.");
        if (!await learningDb.Courses.AnyAsync(item => item.Id == courseId, cancellationToken))
            return Result.Failure<LearningRequirementRule, string>("Course not found.");
        if (satisfactionMode == LearningRequirementSatisfactionMode.MinimumScore && !minimumScore.HasValue)
            return Result.Failure<LearningRequirementRule, string>("Minimum score is required for minimum-score satisfaction mode.");

        bool duplicate = await db.LearningRequirementRules.AnyAsync(item => item.Id != id && item.RequirementDefinitionId == requirementDefinitionId && item.CourseId == courseId, cancellationToken);
        if (duplicate)
            return Result.Failure<LearningRequirementRule, string>("Rule already exists.");

        rule.RequirementDefinitionId = requirementDefinitionId;
        rule.CourseId = courseId;
        rule.SatisfactionMode = satisfactionMode;
        rule.MinimumScore = minimumScore;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<LearningRequirementRule, string>(rule);
    }

    public async Task<bool> DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        LearningRequirementRule? rule = await db.LearningRequirementRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null)
            return false;

        db.LearningRequirementRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ToggleRuleAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        LearningRequirementRule rule = await db.LearningRequirementRules.SingleAsync(item => item.Id == id, cancellationToken);
        if (rule.IsEnabled == isEnabled)
            return;

        rule.IsEnabled = isEnabled;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutstandingLearningCourseOption>> ListOutstandingLearningCoursesAsync(Guid identityId, IEnumerable<Guid> requirementDefinitionIds, CancellationToken cancellationToken = default)
    {
        Guid[] definitionIds = requirementDefinitionIds.Distinct().ToArray();
        if (definitionIds.Length == 0)
            return [];

        RequirementDefinition[] definitions = await requirementsDb.RequirementDefinitions
            .AsNoTracking()
            .Where(item => item.IsActive && item.AllowedEvidenceKinds.Contains(RequirementEvidenceKind.CourseCompletion) && definitionIds.Contains(item.Id))
            .ToArrayAsync(cancellationToken);
        if (definitions.Length == 0)
            return [];

        IReadOnlyList<EvaluatedGrantRequirement> evaluations = await grantRequirementsService.EvaluateGrantRequirementsAsync(identityId, definitions.Select(item => item.Id).ToArray(), cancellationToken);
        Guid[] outstandingIds = evaluations
            .Where(item => item.Status is RequirementResultStatus.Missing or RequirementResultStatus.Expired or RequirementResultStatus.Failed)
            .Select(item => item.RequirementDefinitionId)
            .Distinct()
            .ToArray();
        if (outstandingIds.Length == 0)
            return [];

        LearningRequirementRule[] rules = await db.LearningRequirementRules
            .AsNoTracking()
            .Where(item => item.IsEnabled && outstandingIds.Contains(item.RequirementDefinitionId))
            .ToArrayAsync(cancellationToken);
        if (rules.Length == 0)
            return [];

        Guid[] courseIds = rules.Select(item => item.CourseId).Distinct().ToArray();
        Dictionary<Guid, Course> coursesById = await learningDb.Courses
            .AsNoTracking()
            .Where(item => courseIds.Contains(item.Id) && item.IsActive)
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        return rules
            .Where(item => coursesById.ContainsKey(item.CourseId))
            .Select(item => new OutstandingLearningCourseOption(
                item.RequirementDefinitionId,
                definitions.Single(definition => definition.Id == item.RequirementDefinitionId).Code,
                definitions.Single(definition => definition.Id == item.RequirementDefinitionId).Name,
                item.CourseId,
                coursesById[item.CourseId].Code,
                coursesById[item.CourseId].Title,
                item.SatisfactionMode,
                item.MinimumScore))
            .ToArray();
    }

    public async Task<Result<Enrollment, EnrollmentErrors>> UpsertEnrollmentAsync(Guid identityId, Guid courseId, Guid assignedByIdentityId, CancellationToken cancellationToken = default)
        => await enrollmentService.UpsertEnrollmentAsync(new CreateEnrollmentRequest(courseId, identityId), assignedByIdentityId, cancellationToken);

    public async Task HandleCourseCompletionAsync(Guid identityId, Guid courseId, Guid attemptId, decimal? score, DateTimeOffset completedAt, CancellationToken cancellationToken = default)
    {
        LearningRequirementRule[] rules = await db.LearningRequirementRules
            .AsNoTracking()
            .Where(item => item.IsEnabled && item.CourseId == courseId)
            .ToArrayAsync(cancellationToken);
        if (rules.Length == 0)
            return;

        RequirementDefinition[] definitions = await requirementsDb.RequirementDefinitions
            .AsNoTracking()
            .Where(item => rules.Select(rule => rule.RequirementDefinitionId).Contains(item.Id) && item.AllowedEvidenceKinds.Contains(RequirementEvidenceKind.CourseCompletion))
            .ToArrayAsync(cancellationToken);
        if (definitions.Length == 0)
            return;

        Course? course = await learningDb.Courses.AsNoTracking().SingleOrDefaultAsync(item => item.Id == courseId, cancellationToken);
        string sourceReference = $"learning-attempt:{attemptId}";

        foreach (LearningRequirementRule rule in rules)
        {
            if (rule.SatisfactionMode == LearningRequirementSatisfactionMode.MinimumScore && (!score.HasValue || !rule.MinimumScore.HasValue || score.Value < rule.MinimumScore.Value))
                continue;

            if (!definitions.Any(item => item.Id == rule.RequirementDefinitionId))
                continue;

            bool exists = await requirementsDb.RequirementEvidence.AnyAsync(item => item.IdentityId == identityId && item.RequirementDefinitionId == rule.RequirementDefinitionId && item.SourceReference == sourceReference, cancellationToken);
            if (exists)
                continue;

            await requirementsService.CreateRequirementEvidenceAsync(new CreateRequirementEvidenceRequest(
                identityId,
                rule.RequirementDefinitionId,
                RequirementEvidenceKind.CourseCompletion,
                RequirementEvidenceStatus.Valid,
                completedAt,
                null,
                sourceReference,
                course is null ? $"Completed learning course {courseId}" : $"Completed learning course {course.Title}",
                false,
                completedAt,
                null,
                null), cancellationToken);
        }
    }
}

public sealed record OutstandingLearningCourseOption(
    Guid RequirementDefinitionId,
    string RequirementCode,
    string RequirementName,
    Guid CourseId,
    string CourseCode,
    string CourseTitle,
    LearningRequirementSatisfactionMode SatisfactionMode,
    decimal? MinimumScore);
