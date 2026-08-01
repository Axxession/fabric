using Fabric.Server.CredentialManagement.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.CredentialManagement.Persistence.Configuration;

public sealed class CredentialTypeConfiguration : IEntityTypeConfiguration<CredentialType>
{
    public void Configure(EntityTypeBuilder<CredentialType> builder)
    {
        builder.ToTable("credential_types");
        builder.HasKey(type => type.Id).HasName("pk_credential_types");

        builder.Property(type => type.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(type => type.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(type => type.Technology).HasColumnName("technology").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(type => type.AllocationMode).HasColumnName("allocation_mode").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(type => type.RecyclePolicy).HasColumnName("recycle_policy").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(type => type.RecycleGracePeriod).HasColumnName("recycle_grace_period").IsRequired();
        builder.Property(type => type.RequiresConfirmedPacsRevocation).HasColumnName("requires_confirmed_pacs_revocation").IsRequired();
        builder.Property(type => type.NearLimitThreshold).HasColumnName("near_limit_threshold");
        builder.Property(type => type.IdentifierPrefix).HasColumnName("identifier_prefix").HasMaxLength(100);
        builder.Property(type => type.IdentifierSuffix).HasColumnName("identifier_suffix").HasMaxLength(100);
        builder.Property(type => type.IdentifierNumberLength).HasColumnName("identifier_number_length");
        builder.Property(type => type.IdentifierPaddingDirection).HasColumnName("identifier_padding_direction").HasConversion<string>().HasMaxLength(20);
        builder.Property(type => type.IdentifierPaddingCharacter).HasColumnName("identifier_padding_character").HasMaxLength(1);
        builder.Property(type => type.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(type => type.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(type => type.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasMany(type => type.Ranges)
            .WithOne()
            .HasForeignKey(range => range.CredentialTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(CredentialType.Ranges))!.SetPropertyAccessMode(PropertyAccessMode.Field);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialType.Name))
            .IsUnique()
            .HasDatabaseName("ix_credential_types_tenant_id_name");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialType.Status))
            .HasDatabaseName("ix_credential_types_tenant_id_status");
    }
}

public sealed class CredentialRangeConfiguration : IEntityTypeConfiguration<CredentialRange>
{
    public void Configure(EntityTypeBuilder<CredentialRange> builder)
    {
        builder.ToTable("credential_ranges");
        builder.HasKey(range => range.Id).HasName("pk_credential_ranges");

        builder.Property(range => range.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(range => range.CredentialTypeId).HasColumnName("credential_type_id").IsRequired();
        builder.Property(range => range.RangeStart).HasColumnName("range_start").IsRequired();
        builder.Property(range => range.RangeStop).HasColumnName("range_stop").IsRequired();
        builder.Property(range => range.NextCandidateNumber).HasColumnName("next_candidate_number").IsRequired();
        builder.Property(range => range.IsActive).HasColumnName("is_active").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialRange.CredentialTypeId))
            .HasDatabaseName("ix_credential_ranges_tenant_id_credential_type_id");
    }
}

public sealed class CredentialSlotConfiguration : IEntityTypeConfiguration<CredentialSlot>
{
    public void Configure(EntityTypeBuilder<CredentialSlot> builder)
    {
        builder.ToTable("credential_slots");
        builder.HasKey(slot => slot.Id).HasName("pk_credential_slots");

        builder.Property(slot => slot.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(slot => slot.CredentialRangeId).HasColumnName("credential_range_id").IsRequired();
        builder.Property(slot => slot.Number).HasColumnName("number").IsRequired();
        builder.Property(slot => slot.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(slot => slot.CredentialId).HasColumnName("credential_id");
        builder.Property(slot => slot.ReservationExpiresAt).HasColumnName("reservation_expires_at");
        builder.Property(slot => slot.ReusableFrom).HasColumnName("reusable_from");
        builder.Property(slot => slot.LastStateChangedAt).HasColumnName("last_state_changed_at").IsRequired();

        builder.HasOne<CredentialRange>()
            .WithMany()
            .HasForeignKey(slot => slot.CredentialRangeId)
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialSlot.CredentialRangeId), nameof(CredentialSlot.Number))
            .IsUnique()
            .HasDatabaseName("ix_credential_slots_tenant_id_range_number");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialSlot.Status))
            .HasDatabaseName("ix_credential_slots_tenant_id_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialSlot.CredentialId))
            .HasDatabaseName("ix_credential_slots_tenant_id_credential_id");
    }
}
