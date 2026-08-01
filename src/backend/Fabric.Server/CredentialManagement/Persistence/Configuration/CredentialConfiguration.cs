using Fabric.Server.CredentialManagement.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.CredentialManagement.Persistence.Configuration;

public sealed class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("credentials");
        builder.HasKey(credential => credential.Id).HasName("pk_credentials");

        builder.Property(credential => credential.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(credential => credential.CredentialTypeId).HasColumnName("credential_type_id").IsRequired();
        builder.Property(credential => credential.Identifier).HasColumnName("identifier").HasMaxLength(200).IsRequired();
        builder.Property(credential => credential.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(credential => credential.DurationKind).HasColumnName("duration_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(credential => credential.ValidFrom).HasColumnName("valid_from").IsRequired();
        builder.Property(credential => credential.ValidUntil).HasColumnName("valid_until");
        builder.Property(credential => credential.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(credential => credential.Purpose).HasColumnName("purpose").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(credential => credential.SourceKind).HasColumnName("source_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(credential => credential.SourceId).HasColumnName("source_id");
        builder.Property(credential => credential.RequestedByIdentityId).HasColumnName("requested_by_identity_id");
        builder.Property(credential => credential.ReasonText).HasColumnName("reason_text").HasMaxLength(1000).IsRequired();
        builder.Property(credential => credential.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(credential => credential.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<CredentialType>()
            .WithMany()
            .HasForeignKey(credential => credential.CredentialTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Credential.CredentialTypeId), nameof(Credential.Identifier))
            .HasDatabaseName("ix_credentials_tenant_id_credential_type_identifier");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Credential.IdentityId))
            .HasDatabaseName("ix_credentials_tenant_id_identity_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Credential.Status))
            .HasDatabaseName("ix_credentials_tenant_id_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Credential.SourceKind), nameof(Credential.SourceId))
            .HasDatabaseName("ix_credentials_tenant_id_source");
    }
}
