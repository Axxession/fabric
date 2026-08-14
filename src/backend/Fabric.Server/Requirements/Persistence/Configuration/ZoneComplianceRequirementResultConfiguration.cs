using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class ZoneComplianceRequirementResultConfiguration : IEntityTypeConfiguration<ZoneComplianceRequirementResult>
{
    public void Configure(EntityTypeBuilder<ZoneComplianceRequirementResult> builder)
    {
        builder.ToTable("zone_compliance_requirement_results");
        builder.HasKey(item => item.Id).HasName("pk_zone_compliance_requirement_results");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.ZoneComplianceId).HasColumnName("zone_compliance_id").IsRequired();
        builder.Property(item => item.RequirementDefinitionId).HasColumnName("requirement_definition_id").IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.EvidenceKind).HasColumnName("evidence_kind").HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.EvidenceReference).HasColumnName("evidence_reference").HasMaxLength(250);
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(1_000).IsRequired();
        builder.Property(item => item.ValidUntil).HasColumnName("valid_until");

        builder.HasOne<RequirementDefinition>()
            .WithMany()
            .HasForeignKey(item => item.RequirementDefinitionId)
            .HasConstraintName("fk_zone_compliance_requirement_results_requirement_definitions_requirement_definition_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => item.ZoneComplianceId).HasDatabaseName("ix_zone_compliance_requirement_results_zone_compliance_id");
    }
}
