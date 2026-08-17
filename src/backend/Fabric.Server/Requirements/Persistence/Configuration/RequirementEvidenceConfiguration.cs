using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class RequirementEvidenceConfiguration : IEntityTypeConfiguration<RequirementEvidence>
{
    public void Configure(EntityTypeBuilder<RequirementEvidence> builder)
    {
        builder.ToTable("requirement_evidence");
        builder.HasKey(item => item.Id).HasName("pk_requirement_evidence");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.RequirementDefinitionId).HasColumnName("requirement_definition_id").IsRequired();
        builder.Property(item => item.EvidenceKind).HasColumnName("evidence_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ValidFrom).HasColumnName("valid_from");
        builder.Property(item => item.ValidUntil).HasColumnName("valid_until");
        builder.Property(item => item.SourceReference).HasColumnName("source_reference").HasMaxLength(250);
        builder.Property(item => item.Summary).HasColumnName("summary").HasMaxLength(500).IsRequired();
        builder.Property(item => item.IsSensitive).HasColumnName("is_sensitive").IsRequired();
        builder.Property(item => item.VerifiedAt).HasColumnName("verified_at").IsRequired();
        builder.Property(item => item.FileName).HasColumnName("file_name").HasMaxLength(250);
        builder.Property(item => item.Content).HasColumnName("content");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<RequirementDefinition>()
            .WithMany()
            .HasForeignKey(item => item.RequirementDefinitionId)
            .HasConstraintName("fk_requirement_evidence_requirement_definitions_requirement_definition_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(RequirementEvidence.IdentityId), nameof(RequirementEvidence.RequirementDefinitionId)).HasDatabaseName("ix_requirement_evidence_tenant_id_identity_requirement");
    }
}
