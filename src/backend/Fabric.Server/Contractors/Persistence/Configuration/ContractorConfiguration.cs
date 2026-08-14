using Fabric.Server.Contractors.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Contractors.Persistence.Configuration;

public sealed class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.ToTable("contractors");

        builder.HasKey(contractor => contractor.Id).HasName("pk_contractors");

        builder.Property(contractor => contractor.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(contractor => contractor.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(contractor => contractor.FirstName).HasColumnName("first_name").HasMaxLength(200).IsRequired();
        builder.Property(contractor => contractor.LastName).HasColumnName("last_name").HasMaxLength(200).IsRequired();
        builder.Property(contractor => contractor.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(contractor => contractor.ArchivedAt).HasColumnName("archived_at");
        builder.Property(contractor => contractor.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(contractor => contractor.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(contractor => contractor.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Contractor.CompanyId))
            .HasDatabaseName("ix_contractors_tenant_id_company_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Contractor.Email))
            .HasDatabaseName("ix_contractors_tenant_id_email");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Contractor.ArchivedAt))
            .HasDatabaseName("ix_contractors_tenant_id_archived_at");
    }
}
