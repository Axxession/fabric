using Fabric.Server.Identities.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Identities.Persistence.Configuration;

public sealed class ContractorAffiliationConfiguration : IEntityTypeConfiguration<ContractorAffiliation>
{
    public void Configure(EntityTypeBuilder<ContractorAffiliation> builder)
    {
        builder.ToTable("contractor_affiliations");

        builder.HasKey(affiliation => affiliation.Id).HasName("pk_contractor_affiliations");

        builder.Property(affiliation => affiliation.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(affiliation => affiliation.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(affiliation => affiliation.ContractorId).HasColumnName("contractor_id").IsRequired();
        builder.Property(affiliation => affiliation.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(affiliation => affiliation.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(affiliation => affiliation.EffectiveUntil).HasColumnName("effective_until");

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorAffiliation.IdentityId)).HasDatabaseName("ix_contractor_affiliations_tenant_id_identity_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorAffiliation.ContractorId)).IsUnique().HasDatabaseName("ix_contractor_affiliations_tenant_id_contractor_id");
    }
}
