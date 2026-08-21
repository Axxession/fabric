using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class RequirementDefinitionConfiguration : IEntityTypeConfiguration<RequirementDefinition>
{
    private static readonly ValueConverter<RequirementEvidenceKind[], string[]> AllowedEvidenceKindsConverter = new(
        value => value.Select(item => item.ToString()).ToArray(),
        value => value.Select(item => Enum.Parse<RequirementEvidenceKind>(item)).ToArray());

    private static readonly ValueComparer<RequirementEvidenceKind[]> AllowedEvidenceKindsComparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        value => value.Aggregate(0, (current, item) => HashCode.Combine(current, item)),
        value => value.ToArray());

    public void Configure(EntityTypeBuilder<RequirementDefinition> builder)
    {
        builder.ToTable("requirement_definitions");
        builder.HasKey(item => item.Id).HasName("pk_requirement_definitions");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(2_000);
        builder.Property(item => item.AllowedEvidenceKinds)
            .HasColumnName("allowed_evidence_kinds")
            .HasColumnType("text[]")
            .HasConversion(AllowedEvidenceKindsConverter, AllowedEvidenceKindsComparer)
            .IsRequired();
        builder.Property(item => item.IsSensitive).HasColumnName("is_sensitive").IsRequired();
        builder.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(RequirementDefinition.Code)).IsUnique().HasDatabaseName("ix_requirement_definitions_tenant_id_code");
    }
}
