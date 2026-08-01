using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class EmployeeSuspensionPeriodConfiguration : IEntityTypeConfiguration<EmployeeSuspensionPeriod>
{
    public void Configure(EntityTypeBuilder<EmployeeSuspensionPeriod> builder)
    {
        builder.ToTable("employee_suspension_periods");

        builder.HasKey(period => period.Id).HasName("pk_employee_suspension_periods");

        builder.Property(period => period.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(period => period.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(period => period.From).HasColumnName("from_date").IsRequired();
        builder.Property(period => period.Until).HasColumnName("until_date").IsRequired();
        builder.Property(period => period.Reason).HasColumnName("reason").HasMaxLength(500);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeeSuspensionPeriod.EmployeeId), nameof(EmployeeSuspensionPeriod.From), nameof(EmployeeSuspensionPeriod.Until))
            .HasDatabaseName("ix_employee_suspension_periods_tenant_id_employee_id_dates");
    }
}
