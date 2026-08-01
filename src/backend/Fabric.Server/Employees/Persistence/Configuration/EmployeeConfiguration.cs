using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(employee => employee.Id).HasName("pk_employees");

        builder.Property(employee => employee.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(employee => employee.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(employee => employee.FirstName).HasColumnName("first_name").HasMaxLength(200).IsRequired();
        builder.Property(employee => employee.LastName).HasColumnName("last_name").HasMaxLength(200).IsRequired();
        builder.Property(employee => employee.BirthDate).HasColumnName("birth_date");
        builder.Property(employee => employee.EmployeeNumber).HasColumnName("employee_number").HasMaxLength(100);
        builder.Property(employee => employee.DirectoryId).HasColumnName("directory_id").HasMaxLength(200);
        builder.Property(employee => employee.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(employee => employee.OrganizationUnitId).HasColumnName("organization_unit_id").IsRequired();
        builder.Property(employee => employee.ManagerEmployeeId).HasColumnName("manager_employee_id");
        builder.Property(employee => employee.JobTitle).HasColumnName("job_title").HasMaxLength(200);
        builder.Property(employee => employee.ContractStartDate).HasColumnName("contract_start_date");
        builder.Property(employee => employee.ContractEndDate).HasColumnName("contract_end_date");
        builder.Property(employee => employee.ArchivedAt).HasColumnName("archived_at");
        builder.Property(employee => employee.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(employee => employee.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(employee => employee.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(employee => employee.ManagerEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(employee => employee.WorkLocations)
            .WithOne()
            .HasForeignKey(location => location.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(employee => employee.Personas)
            .WithOne()
            .HasForeignKey(link => link.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(employee => employee.LeavePeriods)
            .WithOne()
            .HasForeignKey(period => period.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(employee => employee.SuspensionPeriods)
            .WithOne()
            .HasForeignKey(period => period.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Employee.IdentityId))
            .IsUnique()
            .HasDatabaseName("ix_employees_tenant_id_identity_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Employee.EmployeeNumber))
            .IsUnique()
            .HasFilter("employee_number IS NOT NULL")
            .HasDatabaseName("ix_employees_tenant_id_employee_number");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Employee.DirectoryId))
            .IsUnique()
            .HasFilter("directory_id IS NOT NULL")
            .HasDatabaseName("ix_employees_tenant_id_directory_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Employee.Email))
            .HasDatabaseName("ix_employees_tenant_id_email");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Employee.OrganizationUnitId))
            .HasDatabaseName("ix_employees_tenant_id_organization_unit_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Employee.ManagerEmployeeId))
            .HasDatabaseName("ix_employees_tenant_id_manager_employee_id");
    }
}
