using Fabric.Server.Identities.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Identities.Persistence.Configuration;

public sealed class IdentityConfiguration : IEntityTypeConfiguration<Identity>
{
    public void Configure(EntityTypeBuilder<Identity> builder)
    {
        builder.ToTable("identities");

        builder.HasKey(identity => identity.Id).HasName("pk_identities");

        builder.Property(identity => identity.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(identity => identity.FirstName).HasColumnName("first_name").HasMaxLength(200).IsRequired();
        builder.Property(identity => identity.MiddleName).HasColumnName("middle_name").HasMaxLength(200);
        builder.Property(identity => identity.LastName).HasColumnName("last_name").HasMaxLength(200).IsRequired();
        builder.Property(identity => identity.PreferredName).HasColumnName("preferred_name").HasMaxLength(200);
        builder.Property(identity => identity.DisplayName).HasColumnName("display_name").HasMaxLength(401).IsRequired();
        builder.Property(identity => identity.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(identity => identity.Phone).HasColumnName("phone").HasMaxLength(50);
        builder.Property(identity => identity.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(identity => identity.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(identity => identity.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasMany(identity => identity.EmployeeAffiliations)
            .WithOne()
            .HasForeignKey(affiliation => affiliation.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(identity => identity.ContractorAffiliations)
            .WithOne()
            .HasForeignKey(affiliation => affiliation.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(identity => identity.VisitorAffiliations)
            .WithOne()
            .HasForeignKey(affiliation => affiliation.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Identity.DisplayName)).HasDatabaseName("ix_identities_tenant_id_display_name");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Identity.Email)).HasDatabaseName("ix_identities_tenant_id_email");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Identity.Status)).HasDatabaseName("ix_identities_tenant_id_status");
    }
}
