using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class EmployeeWorkLocationConfiguration : IEntityTypeConfiguration<EmployeeWorkLocation>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkLocation> builder)
    {
        builder.ToTable("employee_work_locations");

        builder.HasKey(location => new { location.EmployeeId, location.LocationId }).HasName("pk_employee_work_locations");

        builder.Property(location => location.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(location => location.LocationId).HasColumnName("location_id").IsRequired();
        builder.Property(location => location.IsPrimary).HasColumnName("is_primary").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeeWorkLocation.LocationId))
            .HasDatabaseName("ix_employee_work_locations_tenant_id_location_id");
    }
}
