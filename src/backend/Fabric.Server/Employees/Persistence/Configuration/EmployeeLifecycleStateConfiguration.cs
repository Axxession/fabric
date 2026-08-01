using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class EmployeeLifecycleStateConfiguration : IEntityTypeConfiguration<EmployeeLifecycleState>
{
    public void Configure(EntityTypeBuilder<EmployeeLifecycleState> builder)
    {
        builder.ToTable("employee_lifecycle_states");

        builder.HasKey(state => state.EmployeeId).HasName("pk_employee_lifecycle_states");

        builder.Property(state => state.EmployeeId).HasColumnName("employee_id").ValueGeneratedNever();
        builder.Property(state => state.CurrentStatus).HasColumnName("current_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(state => state.EffectiveAt).HasColumnName("effective_at").IsRequired();
        builder.Property(state => state.LastEvaluatedAt).HasColumnName("last_evaluated_at").IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(state => state.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeeLifecycleState.CurrentStatus))
            .HasDatabaseName("ix_employee_lifecycle_states_tenant_id_current_status");
    }
}
