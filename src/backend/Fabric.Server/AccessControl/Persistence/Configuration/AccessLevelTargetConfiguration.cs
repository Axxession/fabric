using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessControl.Persistence.Configuration;

public sealed class AccessLevelTargetConfiguration : IEntityTypeConfiguration<AccessLevelTarget>
{
    public void Configure(EntityTypeBuilder<AccessLevelTarget> builder)
    {
        builder.ToTable("access_level_targets");

        builder.HasKey(target => target.Id).HasName("pk_access_level_targets");

        builder.Property(target => target.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(target => target.AccessItemId).HasColumnName("access_item_id").IsRequired();
        builder.Property(target => target.AccessControlSystemId).HasColumnName("access_control_system_id").IsRequired();
        builder.Property(target => target.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(target => target.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(target => target.ProvisioningTiming).HasColumnName("provisioning_timing").HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasDiscriminator<string>("target_type")
            .HasValue<UnipassAccessLevelTarget>("unipass");

        builder.HasOne<AccessItem>()
            .WithMany()
            .HasForeignKey(target => target.AccessItemId)
            .HasConstraintName("fk_access_level_targets_access_items_access_item_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AccessControlSystem>()
            .WithMany()
            .HasForeignKey(target => target.AccessControlSystemId)
            .HasConstraintName("fk_access_level_targets_access_control_systems_access_control_system_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessLevelTarget.AccessItemId))
            .HasDatabaseName("ix_access_level_targets_tenant_id_access_item_id");

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessLevelTarget.AccessControlSystemId))
            .HasDatabaseName("ix_access_level_targets_tenant_id_access_control_system_id");
    }
}

public sealed class UnipassAccessLevelTargetConfiguration : IEntityTypeConfiguration<UnipassAccessLevelTarget>
{
    public void Configure(EntityTypeBuilder<UnipassAccessLevelTarget> builder)
    {
        builder.Property(target => target.AccessRuleId).HasColumnName("access_rule_id").IsRequired();
        builder.Property(target => target.SiteId).HasColumnName("site_id").IsRequired();
        builder.Property(target => target.AccessRuleName).HasColumnName("access_rule_name").HasMaxLength(200).IsRequired();
        builder.Property(target => target.SiteName).HasColumnName("site_name").HasMaxLength(200).IsRequired();

        builder.HasIndex(
                TenantDbContext.TenantIdPropertyName,
                nameof(AccessLevelTarget.AccessItemId),
                nameof(AccessLevelTarget.AccessControlSystemId),
                nameof(UnipassAccessLevelTarget.SiteId),
                nameof(UnipassAccessLevelTarget.AccessRuleId))
            .IsUnique()
            .HasDatabaseName("ix_access_level_targets_tenant_id_item_system_site_rule");
    }
}
