using Fabric.Server.Reception.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Reception.Persistence.Configuration;

public sealed class ReceptionAssignedAccessPolicyConfiguration : IEntityTypeConfiguration<ReceptionAssignedAccessPolicy>
{
    public void Configure(EntityTypeBuilder<ReceptionAssignedAccessPolicy> builder)
    {
        builder.ToTable("assigned_access_policies");

        builder.HasKey(assignedPolicy => assignedPolicy.Id).HasName("pk_assigned_access_policies");

        builder.Property(assignedPolicy => assignedPolicy.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(assignedPolicy => assignedPolicy.ArrivalId).HasColumnName("arrival_id").IsRequired();
        builder.Property(assignedPolicy => assignedPolicy.RuleAssignmentId).HasColumnName("rule_assignment_id").IsRequired();
        builder.Property(assignedPolicy => assignedPolicy.AccessGrantId).HasColumnName("access_grant_id").IsRequired();
        builder.Property(assignedPolicy => assignedPolicy.PackageId).HasColumnName("package_id").IsRequired();

        builder.HasIndex(assignedPolicy => assignedPolicy.ArrivalId).HasDatabaseName("ix_assigned_access_policies_arrival_id");
        builder.HasIndex(assignedPolicy => assignedPolicy.RuleAssignmentId).HasDatabaseName("ix_assigned_access_policies_rule_assignment_id");
        builder.HasIndex(assignedPolicy => assignedPolicy.AccessGrantId).HasDatabaseName("ix_assigned_access_policies_access_grant_id");
        builder.HasIndex(assignedPolicy => new { assignedPolicy.ArrivalId, assignedPolicy.RuleAssignmentId })
            .IsUnique()
            .HasDatabaseName("ix_assigned_access_policies_arrival_id_rule_assignment_id");
    }
}
