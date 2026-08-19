using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Learning.Persistence.Configuration;

public sealed class CourseScoConfiguration : IEntityTypeConfiguration<CourseSco>
{
    public void Configure(EntityTypeBuilder<CourseSco> builder)
    {
        builder.ToTable("course_scos");
        builder.HasKey(item => item.Id).HasName("pk_course_scos");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.CourseVersionId).HasColumnName("course_version_id").IsRequired();
        builder.Property(item => item.ScoIdentifier).HasColumnName("sco_identifier").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(item => item.LaunchUrl).HasColumnName("launch_url").HasMaxLength(1_000).IsRequired();
        builder.Property(item => item.ResourcePath).HasColumnName("resource_path").HasMaxLength(1_000).IsRequired();
        builder.Property(item => item.ManifestOrder).HasColumnName("manifest_order").IsRequired();
        builder.Property(item => item.MasteryScore).HasColumnName("mastery_score").HasPrecision(10, 2);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CourseSco.CourseVersionId), nameof(CourseSco.ManifestOrder)).HasDatabaseName("ix_course_scos_tenant_id_course_version_id_manifest_order");
    }
}
