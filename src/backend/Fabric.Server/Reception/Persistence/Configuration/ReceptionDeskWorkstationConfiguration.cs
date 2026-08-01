using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Reception.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Reception.Persistence.Configuration;

public sealed class ReceptionDeskWorkstationConfiguration : IEntityTypeConfiguration<ReceptionDeskWorkstation>
{
    public void Configure(EntityTypeBuilder<ReceptionDeskWorkstation> builder)
    {
        builder.ToTable("reception_desk_workstations");

        builder.HasKey(workstation => workstation.Id).HasName("pk_reception_desk_workstations");

        builder.Property(workstation => workstation.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(workstation => workstation.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(workstation => workstation.LocationId).HasColumnName("location_id").IsRequired();
        builder.Property(workstation => workstation.ApiKeyHash).HasColumnName("api_key_hash").IsRequired().HasMaxLength(200);
        builder.Property(workstation => workstation.ApiKeySalt).HasColumnName("api_key_salt").IsRequired().HasMaxLength(200);
        builder.Property(workstation => workstation.Enabled).HasColumnName("enabled").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ReceptionDeskWorkstation.LocationId))
            .HasDatabaseName("ix_reception_desk_workstations_tenant_id_location_id");
    }
}
