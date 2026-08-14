using Fabric.Server.Contractors.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Contractors.Persistence.Configuration;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(company => company.Id).HasName("pk_companies");

        builder.Property(company => company.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(company => company.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(company => company.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(company => company.CompanyNumber).HasColumnName("company_number").HasMaxLength(100);
        builder.Property(company => company.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(company => company.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(company => company.UpdatedAt).HasColumnName("updated_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Company.Code))
            .IsUnique()
            .HasDatabaseName("ix_companies_tenant_id_code");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Company.CompanyNumber))
            .IsUnique()
            .HasFilter("company_number IS NOT NULL")
            .HasDatabaseName("ix_companies_tenant_id_company_number");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Company.IsActive))
            .HasDatabaseName("ix_companies_tenant_id_is_active");
    }
}
