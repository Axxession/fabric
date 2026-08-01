using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class EmployeeLifecycleRecalculationConfiguration : IEntityTypeConfiguration<EmployeeLifecycleRecalculation>
{
    public void Configure(EntityTypeBuilder<EmployeeLifecycleRecalculation> builder)
    {
        builder.ToTable("employee_lifecycle_recalculations");

        builder.HasKey(item => item.Id).HasName("pk_employee_lifecycle_recalculations");

        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(item => item.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(100).IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ProcessingStartedAt).HasColumnName("processing_started_at");
        builder.Property(item => item.CompletedAt).HasColumnName("completed_at");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(item => item.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeeLifecycleRecalculation.Status), nameof(EmployeeLifecycleRecalculation.ScheduledFor))
            .HasDatabaseName("ix_employee_lifecycle_recalculations_tenant_id_status_scheduled_for");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeeLifecycleRecalculation.EmployeeId), nameof(EmployeeLifecycleRecalculation.ScheduledFor), nameof(EmployeeLifecycleRecalculation.Reason))
            .HasDatabaseName("ix_employee_lifecycle_recalculations_tenant_id_employee_id_schedule_reason");
    }
}
