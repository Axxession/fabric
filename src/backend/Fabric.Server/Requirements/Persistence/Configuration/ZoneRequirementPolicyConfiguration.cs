using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class ZoneRequirementPolicyConfiguration : IEntityTypeConfiguration<ZoneRequirementPolicy>
{
    public void Configure(EntityTypeBuilder<ZoneRequirementPolicy> builder)
    {
        builder.ToTable("zone_requirement_policies");
        builder.HasKey(item => item.Id).HasName("pk_zone_requirement_policies");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.EnforcementZoneId).HasColumnName("enforcement_zone_id").IsRequired();
        builder.Property(item => item.RequirementDefinitionId).HasColumnName("requirement_definition_id").IsRequired();
        builder.Property(item => item.SubjectKind).HasColumnName("subject_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.IsBlocking).HasColumnName("is_blocking").IsRequired();
        builder.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<EnforcementZone>()
            .WithMany()
            .HasForeignKey(item => item.EnforcementZoneId)
            .HasConstraintName("fk_zone_requirement_policies_enforcement_zones_enforcement_zone_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RequirementDefinition>()
            .WithMany()
            .HasForeignKey(item => item.RequirementDefinitionId)
            .HasConstraintName("fk_zone_requirement_policies_requirement_definitions_requirement_definition_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ZoneRequirementPolicy.EnforcementZoneId), nameof(ZoneRequirementPolicy.RequirementDefinitionId), nameof(ZoneRequirementPolicy.SubjectKind)).IsUnique().HasDatabaseName("ix_zone_requirement_policies_tenant_id_zone_requirement_subject");
    }
}
