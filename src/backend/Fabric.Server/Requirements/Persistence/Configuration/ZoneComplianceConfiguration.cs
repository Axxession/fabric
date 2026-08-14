using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class ZoneComplianceConfiguration : IEntityTypeConfiguration<ZoneCompliance>
{
    public void Configure(EntityTypeBuilder<ZoneCompliance> builder)
    {
        builder.ToTable("zone_compliances");
        builder.HasKey(item => item.Id).HasName("pk_zone_compliances");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.EnforcementZoneId).HasColumnName("enforcement_zone_id").IsRequired();
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.SubjectKind).HasColumnName("subject_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.CalculatedStatus).HasColumnName("calculated_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ValidFrom).HasColumnName("valid_from").IsRequired();
        builder.Property(item => item.ValidUntil).HasColumnName("valid_until");
        builder.Property(item => item.LastEvaluatedAt).HasColumnName("last_evaluated_at").IsRequired();
        builder.Property(item => item.ReasonSummary).HasColumnName("reason_summary").HasMaxLength(1_000).IsRequired();

        builder.HasMany(item => item.RequirementResults)
            .WithOne()
            .HasForeignKey(item => item.ZoneComplianceId)
            .HasConstraintName("fk_zone_compliance_requirement_results_zone_compliances_zone_compliance_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EnforcementZone>()
            .WithMany()
            .HasForeignKey(item => item.EnforcementZoneId)
            .HasConstraintName("fk_zone_compliances_enforcement_zones_enforcement_zone_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ZoneCompliance.IdentityId), nameof(ZoneCompliance.EnforcementZoneId)).IsUnique().HasDatabaseName("ix_zone_compliances_tenant_id_identity_zone");
    }
}
