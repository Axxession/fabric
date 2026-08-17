using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class LocationJobRequirementPolicyConfiguration : IEntityTypeConfiguration<LocationJobRequirementPolicy>
{
    public void Configure(EntityTypeBuilder<LocationJobRequirementPolicy> builder)
    {
        builder.ToTable("location_job_requirement_policies");
        builder.HasKey(item => item.Id).HasName("pk_location_job_requirement_policies");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.LocationId).HasColumnName("location_id").IsRequired();
        builder.Property(item => item.JobTypeId).HasColumnName("job_type_id").IsRequired();
        builder.Property(item => item.RequirementDefinitionId).HasColumnName("requirement_definition_id").IsRequired();
        builder.Property(item => item.IsBlocking).HasColumnName("is_blocking").IsRequired();
        builder.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(LocationJobRequirementPolicy.LocationId), nameof(LocationJobRequirementPolicy.JobTypeId), nameof(LocationJobRequirementPolicy.RequirementDefinitionId))
            .HasDatabaseName("ix_location_job_requirement_policies_tenant_id_location_id_job_type_id_requirement_definition_id");
    }
}
