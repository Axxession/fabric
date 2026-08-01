using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class EmployeeLeavePeriodConfiguration : IEntityTypeConfiguration<EmployeeLeavePeriod>
{
    public void Configure(EntityTypeBuilder<EmployeeLeavePeriod> builder)
    {
        builder.ToTable("employee_leave_periods");

        builder.HasKey(period => period.Id).HasName("pk_employee_leave_periods");

        builder.Property(period => period.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(period => period.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(period => period.From).HasColumnName("from_date").IsRequired();
        builder.Property(period => period.Until).HasColumnName("until_date").IsRequired();
        builder.Property(period => period.Reason).HasColumnName("reason").HasMaxLength(500);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeeLeavePeriod.EmployeeId), nameof(EmployeeLeavePeriod.From), nameof(EmployeeLeavePeriod.Until))
            .HasDatabaseName("ix_employee_leave_periods_tenant_id_employee_id_dates");
    }
}
