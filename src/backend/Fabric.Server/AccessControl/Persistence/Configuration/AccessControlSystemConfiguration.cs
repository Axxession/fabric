using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessControl.Persistence.Configuration;

public sealed class AccessControlSystemConfiguration : IEntityTypeConfiguration<AccessControlSystem>
{
    public void Configure(EntityTypeBuilder<AccessControlSystem> builder)
    {
        builder.ToTable("access_control_systems");

        builder.HasKey(system => system.Id).HasName("pk_access_control_systems");

        builder.Property(system => system.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(system => system.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(system => system.ProviderKind).HasColumnName("provider_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(system => system.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.OwnsOne(system => system.UnipassConfig, config =>
        {
            config.Property(item => item!.Endpoint).HasColumnName("unipass_endpoint").HasMaxLength(1_000);
            config.Property(item => item!.SslValidation).HasColumnName("unipass_ssl_validation");
            config.Property(item => item!.Username).HasColumnName("unipass_username").HasMaxLength(200);
            config.Property(item => item!.Password).HasColumnName("unipass_password").HasMaxLength(2_000);
        });

        TenantDbContext.ConfigureTenantProperty(builder);

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessControlSystem.Name))
            .IsUnique()
            .HasDatabaseName("ix_access_control_systems_tenant_id_name");
    }
}
