using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class EmployeeLifecycleEventConfiguration : IEntityTypeConfiguration<EmployeeLifecycleEvent>
{
    public void Configure(EntityTypeBuilder<EmployeeLifecycleEvent> builder)
    {
        builder.ToTable("employee_lifecycle_events");

        builder.HasKey(item => item.Id).HasName("pk_employee_lifecycle_events");

        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.FromStatus).HasColumnName("from_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ToStatus).HasColumnName("to_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.EffectiveAt).HasColumnName("effective_at").IsRequired();
        builder.Property(item => item.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(item => item.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeeLifecycleEvent.EmployeeId), nameof(EmployeeLifecycleEvent.EffectiveAt))
            .HasDatabaseName("ix_employee_lifecycle_events_tenant_id_employee_id_effective_at");
    }
}
