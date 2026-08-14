using Fabric.Server.Contractors.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Contractors.Persistence.Configuration;

public sealed class JobTypeConfiguration : IEntityTypeConfiguration<JobType>
{
    public void Configure(EntityTypeBuilder<JobType> builder)
    {
        builder.ToTable("job_types");

        builder.HasKey(jobType => jobType.Id).HasName("pk_job_types");

        builder.Property(jobType => jobType.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(jobType => jobType.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(jobType => jobType.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(jobType => jobType.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(jobType => jobType.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(jobType => jobType.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(jobType => jobType.UpdatedAt).HasColumnName("updated_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(JobType.Code))
            .IsUnique()
            .HasDatabaseName("ix_job_types_tenant_id_code");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(JobType.IsActive))
            .HasDatabaseName("ix_job_types_tenant_id_is_active");
    }
}
