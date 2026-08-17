using Fabric.Server.Contractors.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Contractors.Persistence.Configuration;

public sealed class ContractorJobConfiguration : IEntityTypeConfiguration<ContractorJob>
{
    public void Configure(EntityTypeBuilder<ContractorJob> builder)
    {
        builder.ToTable("contractor_jobs");

        builder.HasKey(job => job.Id).HasName("pk_contractor_jobs");

        builder.Property(job => job.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(job => job.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(job => job.JobTypeId).HasColumnName("job_type_id").IsRequired();
        builder.Property(job => job.LocationId).HasColumnName("location_id").IsRequired();
        builder.Property(job => job.CreatedByIdentityId).HasColumnName("created_by_identity_id").IsRequired();
        builder.Property(job => job.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(job => job.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(job => job.PlannedStart).HasColumnName("planned_start").IsRequired();
        builder.Property(job => job.PlannedEnd).HasColumnName("planned_end").IsRequired();
        builder.Property(job => job.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(job => job.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(job => job.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(job => job.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JobType>()
            .WithMany()
            .HasForeignKey(job => job.JobTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(job => job.Assignments)
            .WithOne()
            .HasForeignKey(assignment => assignment.ContractorJobId)
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJob.CompanyId))
            .HasDatabaseName("ix_contractor_jobs_tenant_id_company_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJob.JobTypeId))
            .HasDatabaseName("ix_contractor_jobs_tenant_id_job_type_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJob.LocationId))
            .HasDatabaseName("ix_contractor_jobs_tenant_id_location_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJob.CreatedByIdentityId))
            .HasDatabaseName("ix_contractor_jobs_tenant_id_created_by_identity_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJob.Status))
            .HasDatabaseName("ix_contractor_jobs_tenant_id_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJob.PlannedStart))
            .HasDatabaseName("ix_contractor_jobs_tenant_id_planned_start");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJob.PlannedEnd))
            .HasDatabaseName("ix_contractor_jobs_tenant_id_planned_end");
    }
}
