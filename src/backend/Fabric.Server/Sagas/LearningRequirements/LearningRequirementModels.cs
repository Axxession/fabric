namespace Fabric.Server.Sagas.LearningRequirements;

public enum LearningRequirementSatisfactionMode
{
    Completion,
    MinimumScore,
}

public sealed class LearningRequirementRule
{
    public Guid Id { get; set; }
    public Guid RequirementDefinitionId { get; set; }
    public Guid CourseId { get; set; }
    public LearningRequirementSatisfactionMode SatisfactionMode { get; set; }
    public decimal? MinimumScore { get; set; }
    public bool IsEnabled { get; set; }
}
