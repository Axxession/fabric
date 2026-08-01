using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("personas");

        builder.HasKey(persona => persona.Id).HasName("pk_personas");

        builder.Property(persona => persona.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(persona => persona.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(persona => persona.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(persona => persona.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(persona => persona.UpdatedAt).HasColumnName("updated_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Persona.Name))
            .IsUnique()
            .HasDatabaseName("ix_personas_tenant_id_name");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Persona.IsActive))
            .HasDatabaseName("ix_personas_tenant_id_is_active");
    }
}
