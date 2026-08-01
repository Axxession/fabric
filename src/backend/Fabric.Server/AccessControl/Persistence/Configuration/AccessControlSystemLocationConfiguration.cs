using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessControl.Persistence.Configuration;

public sealed class AccessControlSystemLocationConfiguration : IEntityTypeConfiguration<AccessControlSystemLocation>
{
    public void Configure(EntityTypeBuilder<AccessControlSystemLocation> builder)
    {
        builder.ToTable("access_control_system_locations");

        builder.HasKey(link => link.Id).HasName("pk_access_control_system_locations");

        builder.Property(link => link.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(link => link.AccessControlSystemId).HasColumnName("access_control_system_id").IsRequired();
        builder.Property(link => link.LocationId).HasColumnName("location_id").IsRequired();

        builder.HasOne<AccessControlSystem>()
            .WithMany()
            .HasForeignKey(link => link.AccessControlSystemId)
            .HasConstraintName("fk_access_control_system_locations_access_control_systems_access_control_system_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessControlSystemLocation.LocationId))
            .IsUnique()
            .HasDatabaseName("ix_access_control_system_locations_tenant_id_location_id");

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessControlSystemLocation.AccessControlSystemId))
            .HasDatabaseName("ix_access_control_system_locations_tenant_id_access_control_system_id");
    }
}
