using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class GrantRequirementResultConfiguration : IEntityTypeConfiguration<GrantRequirementResult>
{
    public void Configure(EntityTypeBuilder<GrantRequirementResult> builder)
    {
        builder.ToTable("grant_requirement_results");
        builder.HasKey(item => item.Id).HasName("pk_grant_requirement_results");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AccessGrantId).HasColumnName("access_grant_id").IsRequired();
        builder.Property(item => item.RequirementDefinitionId).HasColumnName("requirement_definition_id").IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.EvidenceKind).HasColumnName("evidence_kind").HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.EvidenceReference).HasColumnName("evidence_reference").HasMaxLength(250);
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(2_000).IsRequired();
        builder.Property(item => item.ValidUntil).HasColumnName("valid_until");
        builder.Property(item => item.LastEvaluatedAt).HasColumnName("last_evaluated_at").IsRequired();

        builder.HasOne<AccessGrant>()
            .WithMany()
            .HasForeignKey(item => item.AccessGrantId)
            .HasConstraintName("fk_grant_requirement_results_access_grants_access_grant_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(GrantRequirementResult.AccessGrantId), nameof(GrantRequirementResult.RequirementDefinitionId))
            .HasDatabaseName("ix_grant_requirement_results_tenant_id_access_grant_id_requirement_definition_id");
    }
}
