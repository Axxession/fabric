using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Sagas.Persistence.Configuration;

public sealed class AccessGrantMaterializationOutcomeConfiguration : IEntityTypeConfiguration<AccessGrantMaterializationOutcome>
{
    public void Configure(EntityTypeBuilder<AccessGrantMaterializationOutcome> builder)
    {
        builder.ToTable("access_grant_materialization_outcomes");
        builder.HasKey(item => item.Id).HasName("pk_access_grant_materialization_outcomes");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AccessGrantId).HasColumnName("access_grant_id").IsRequired();
        builder.Property(item => item.AccessItemId).HasColumnName("access_item_id").IsRequired();
        builder.Property(item => item.LocationId).HasColumnName("location_id").IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.FailureReason).HasColumnName("failure_reason").HasMaxLength(2_000);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessGrantMaterializationOutcome.AccessGrantId), nameof(AccessGrantMaterializationOutcome.AccessItemId), nameof(AccessGrantMaterializationOutcome.LocationId))
            .HasDatabaseName("ix_access_grant_materialization_outcomes_tenant_grant_item_location")
            .IsUnique();
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessGrantMaterializationOutcome.AccessGrantId))
            .HasDatabaseName("ix_access_grant_materialization_outcomes_tenant_grant_id");
    }
}
