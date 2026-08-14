using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class ProjectedZoneAccessAssignmentConfiguration : IEntityTypeConfiguration<ProjectedZoneAccessAssignment>
{
    public void Configure(EntityTypeBuilder<ProjectedZoneAccessAssignment> builder)
    {
        builder.ToTable("projected_zone_access_assignments");
        builder.HasKey(item => item.Id).HasName("pk_projected_zone_access_assignments");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.ZoneComplianceId).HasColumnName("zone_compliance_id").IsRequired();
        builder.Property(item => item.EnforcementZoneAccessPolicyId).HasColumnName("enforcement_zone_access_policy_id").IsRequired();
        builder.Property(item => item.AccessItemId).HasColumnName("access_item_id").IsRequired();
        builder.Property(item => item.LocationId).HasColumnName("location_id").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<ZoneCompliance>()
            .WithMany()
            .HasForeignKey(item => item.ZoneComplianceId)
            .HasConstraintName("fk_projected_zone_access_assignments_zone_compliances_zone_compliance_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EnforcementZoneAccessPolicy>()
            .WithMany()
            .HasForeignKey(item => item.EnforcementZoneAccessPolicyId)
            .HasConstraintName("fk_projected_zone_access_assignments_enforcement_zone_access_policies_policy_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ProjectedZoneAccessAssignment.ZoneComplianceId), nameof(ProjectedZoneAccessAssignment.EnforcementZoneAccessPolicyId), nameof(ProjectedZoneAccessAssignment.LocationId)).IsUnique().HasDatabaseName("ix_projected_zone_access_assignments_tenant_id_zone_compliance_policy_location");
    }
}
