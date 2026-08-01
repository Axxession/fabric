using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class EmployeePersonaConfiguration : IEntityTypeConfiguration<EmployeePersona>
{
    public void Configure(EntityTypeBuilder<EmployeePersona> builder)
    {
        builder.ToTable("employee_personas");

        builder.HasKey(link => new { link.EmployeeId, link.PersonaId }).HasName("pk_employee_personas");

        builder.Property(link => link.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(link => link.PersonaId).HasColumnName("persona_id").IsRequired();

        builder.HasOne<Persona>()
            .WithMany()
            .HasForeignKey(link => link.PersonaId)
            .OnDelete(DeleteBehavior.Restrict);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EmployeePersona.PersonaId))
            .HasDatabaseName("ix_employee_personas_tenant_id_persona_id");
    }
}
