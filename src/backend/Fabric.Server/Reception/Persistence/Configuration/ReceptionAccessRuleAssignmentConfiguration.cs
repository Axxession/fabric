using Fabric.Server.Reception.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Reception.Persistence.Configuration;

public sealed class ReceptionAccessRuleAssignmentConfiguration : IEntityTypeConfiguration<ReceptionAccessRuleAssignment>
{
    public void Configure(EntityTypeBuilder<ReceptionAccessRuleAssignment> builder)
    {
        builder.ToTable("access_rule_assignments");

        builder.HasKey(assignment => assignment.Id).HasName("pk_access_rule_assignments");

        builder.Property(assignment => assignment.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(assignment => assignment.PackageId).HasColumnName("package_id").IsRequired();
        builder.Property(assignment => assignment.GracePeriodMinutes).HasColumnName("grace_period_minutes").IsRequired();
        builder.Property(assignment => assignment.Trigger)
            .HasColumnName("trigger")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(assignment => assignment.PackageId).HasDatabaseName("ix_access_rule_assignments_package_id");
        builder.HasIndex(assignment => assignment.Trigger).HasDatabaseName("ix_access_rule_assignments_trigger");
    }
}
