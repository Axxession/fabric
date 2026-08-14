using Fabric.Server.Contractors.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Contractors.Persistence.Configuration;

public sealed class ContractorJobAssignmentConfiguration : IEntityTypeConfiguration<ContractorJobAssignment>
{
    public void Configure(EntityTypeBuilder<ContractorJobAssignment> builder)
    {
        builder.ToTable("contractor_job_assignments");

        builder.HasKey(assignment => assignment.Id).HasName("pk_contractor_job_assignments");

        builder.Property(assignment => assignment.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(assignment => assignment.ContractorJobId).HasColumnName("contractor_job_id").IsRequired();
        builder.Property(assignment => assignment.ContractorId).HasColumnName("contractor_id").IsRequired();
        builder.Property(assignment => assignment.AssignedFrom).HasColumnName("assigned_from").IsRequired();
        builder.Property(assignment => assignment.AssignedUntil).HasColumnName("assigned_until").IsRequired();
        builder.Property(assignment => assignment.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasOne<Contractor>()
            .WithMany()
            .HasForeignKey(assignment => assignment.ContractorId)
            .OnDelete(DeleteBehavior.Restrict);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJobAssignment.ContractorJobId))
            .HasDatabaseName("ix_contractor_job_assignments_tenant_id_contractor_job_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJobAssignment.ContractorId))
            .HasDatabaseName("ix_contractor_job_assignments_tenant_id_contractor_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJobAssignment.Status))
            .HasDatabaseName("ix_contractor_job_assignments_tenant_id_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJobAssignment.AssignedFrom))
            .HasDatabaseName("ix_contractor_job_assignments_tenant_id_assigned_from");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ContractorJobAssignment.AssignedUntil))
            .HasDatabaseName("ix_contractor_job_assignments_tenant_id_assigned_until");
    }
}
