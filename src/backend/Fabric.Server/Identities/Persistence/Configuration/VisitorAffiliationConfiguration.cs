using Fabric.Server.Identities.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Identities.Persistence.Configuration;

public sealed class VisitorAffiliationConfiguration : IEntityTypeConfiguration<VisitorAffiliation>
{
    public void Configure(EntityTypeBuilder<VisitorAffiliation> builder)
    {
        builder.ToTable("visitor_affiliations");

        builder.HasKey(affiliation => affiliation.Id).HasName("pk_visitor_affiliations");

        builder.Property(affiliation => affiliation.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(affiliation => affiliation.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(affiliation => affiliation.VisitorId).HasColumnName("visitor_id").IsRequired();
        builder.Property(affiliation => affiliation.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(affiliation => affiliation.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(affiliation => affiliation.EffectiveUntil).HasColumnName("effective_until");

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(VisitorAffiliation.IdentityId)).HasDatabaseName("ix_visitor_affiliations_tenant_id_identity_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(VisitorAffiliation.VisitorId)).IsUnique().HasDatabaseName("ix_visitor_affiliations_tenant_id_visitor_id");
    }
}
