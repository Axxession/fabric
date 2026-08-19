using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Sagas.LearningRequirements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Sagas.Persistence.Configuration;

public sealed class LearningRequirementRuleConfiguration : IEntityTypeConfiguration<LearningRequirementRule>
{
    public void Configure(EntityTypeBuilder<LearningRequirementRule> builder)
    {
        builder.ToTable("learning_requirement_rules");
        builder.HasKey(item => item.Id).HasName("pk_learning_requirement_rules");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.RequirementDefinitionId).HasColumnName("requirement_definition_id").IsRequired();
        builder.Property(item => item.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(item => item.SatisfactionMode).HasColumnName("satisfaction_mode").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.MinimumScore).HasColumnName("minimum_score").HasPrecision(10, 2);
        builder.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex("TenantId", nameof(LearningRequirementRule.RequirementDefinitionId), nameof(LearningRequirementRule.CourseId)).IsUnique().HasDatabaseName("ix_learning_requirement_rules_tenant_id_requirement_definition_id_course_id");
    }
}
