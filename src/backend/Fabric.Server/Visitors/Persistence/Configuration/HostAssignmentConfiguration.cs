using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Visitors.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Visitors.Persistence.Configuration;

public sealed class HostAssignmentConfiguration : IEntityTypeConfiguration<HostAssignment>
{
    public void Configure(EntityTypeBuilder<HostAssignment> builder)
    {
        builder.ToTable("host_assignments");

        builder.HasKey(hostAssignment => hostAssignment.Id).HasName("pk_host_assignments");
        builder.Property(hostAssignment => hostAssignment.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(hostAssignment => hostAssignment.EmployeeId).HasColumnName("employee_id").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(HostAssignment.EmployeeId))
            .IsUnique()
            .HasDatabaseName("ix_host_assignments_tenant_id_employee_id");
    }
}
