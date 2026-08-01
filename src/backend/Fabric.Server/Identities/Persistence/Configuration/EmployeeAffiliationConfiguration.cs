using Fabric.Server.Identities.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Identities.Persistence.Configuration;

public sealed class EmployeeAffiliationConfiguration : IEntityTypeConfiguration<EmployeeAffiliation>
{
    public void Configure(EntityTypeBuilder<EmployeeAffiliation> builder)
    {
        builder.ToTable("employee_affiliations");

        builder.HasKey(affiliation => affiliation.Id).HasName("pk_employee_affiliations");

        builder.Property(affiliation => affiliation.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(affiliation => affiliation.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(affiliation => affiliation.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(affiliation => affiliation.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(affiliation => affiliation.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(affiliation => affiliation.EffectiveUntil).HasColumnName("effective_until");

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeeAffiliation.IdentityId)).HasDatabaseName("ix_employee_affiliations_tenant_id_identity_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeeAffiliation.EmployeeId)).IsUnique().HasDatabaseName("ix_employee_affiliations_tenant_id_employee_id");
    }
}
