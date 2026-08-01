using Fabric.Server.Employees.Domain;
using Fabric.Server.Employees.Persistence.Configuration;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Employees.Persistence;

public sealed class EmployeesDbContext : TenantDbContext
{
    public const string Schema = "employees";

    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<EmployeeLifecycleEvent> EmployeeLifecycleEvents { get; set; } = null!;
    public DbSet<EmployeeLifecycleRecalculation> EmployeeLifecycleRecalculations { get; set; } = null!;
    public DbSet<EmployeeLifecycleState> EmployeeLifecycleStates { get; set; } = null!;
    public DbSet<EmployeeLeavePeriod> EmployeeLeavePeriods { get; set; } = null!;
    public DbSet<EmployeePersona> EmployeePersonas { get; set; } = null!;
    public DbSet<EmployeeSuspensionPeriod> EmployeeSuspensionPeriods { get; set; } = null!;
    public DbSet<EmployeeWorkLocation> EmployeeWorkLocations { get; set; } = null!;
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; } = null!;
    public DbSet<OrganizationUnitClosure> OrganizationUnitClosures { get; set; } = null!;
    public DbSet<Persona> Personas { get; set; } = null!;

    public EmployeesDbContext(DbContextOptions<EmployeesDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public EmployeesDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeLifecycleEventConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeLifecycleRecalculationConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeLifecycleStateConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeLeavePeriodConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeePersonaConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSuspensionPeriodConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeWorkLocationConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationUnitConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationUnitClosureConfiguration());
        modelBuilder.ApplyConfiguration(new PersonaConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
