using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class GrantRequirementConfiguration : IEntityTypeConfiguration<GrantRequirement>
{
    public void Configure(EntityTypeBuilder<GrantRequirement> builder)
    {
        builder.ToTable("grant_requirements");
        builder.HasKey(item => item.Id).HasName("pk_grant_requirements");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AccessGrantId).HasColumnName("access_grant_id").IsRequired();
        builder.Property(item => item.RequirementDefinitionId).HasColumnName("requirement_definition_id").IsRequired();
        builder.Property(item => item.SourcePolicyKind).HasColumnName("source_policy_kind").HasMaxLength(100).IsRequired();
        builder.Property(item => item.SourcePolicyId).HasColumnName("source_policy_id").IsRequired();
        builder.Property(item => item.IsBlocking).HasColumnName("is_blocking").IsRequired();
        builder.Property(item => item.DerivedAt).HasColumnName("derived_at").IsRequired();

        builder.HasOne<AccessGrant>()
            .WithMany()
            .HasForeignKey(item => item.AccessGrantId)
            .HasConstraintName("fk_grant_requirements_access_grants_access_grant_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(GrantRequirement.AccessGrantId))
            .HasDatabaseName("ix_grant_requirements_tenant_id_access_grant_id");
    }
}
