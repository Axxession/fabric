using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class LocationRequirementPolicyConfiguration : IEntityTypeConfiguration<LocationRequirementPolicy>
{
    public void Configure(EntityTypeBuilder<LocationRequirementPolicy> builder)
    {
        builder.ToTable("location_requirement_policies");
        builder.HasKey(item => item.Id).HasName("pk_location_requirement_policies");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.LocationId).HasColumnName("location_id").IsRequired();
        builder.Property(item => item.RequirementDefinitionId).HasColumnName("requirement_definition_id").IsRequired();
        builder.Property(item => item.SubjectKind).HasColumnName("subject_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.IsBlocking).HasColumnName("is_blocking").IsRequired();
        builder.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(LocationRequirementPolicy.LocationId), nameof(LocationRequirementPolicy.RequirementDefinitionId), nameof(LocationRequirementPolicy.SubjectKind))
            .HasDatabaseName("ix_location_requirement_policies_tenant_id_location_id_requirement_definition_id_subject_kind");
    }
}
