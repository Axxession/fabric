using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Learning.Persistence.Configuration;

public sealed class CourseVersionConfiguration : IEntityTypeConfiguration<CourseVersion>
{
    public void Configure(EntityTypeBuilder<CourseVersion> builder)
    {
        builder.ToTable("course_versions");
        builder.HasKey(item => item.Id).HasName("pk_course_versions");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(item => item.CourseLanguageId).HasColumnName("course_language_id").IsRequired();
        builder.Property(item => item.VersionNumber).HasColumnName("version_number").IsRequired();
        builder.Property(item => item.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(item => item.ScormVersion).HasColumnName("scorm_version").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.EmitsScore).HasColumnName("emits_score").IsRequired();
        builder.Property(item => item.StoragePath).HasColumnName("storage_path").HasMaxLength(800).IsRequired();
        builder.Property(item => item.ManifestChecksum).HasColumnName("manifest_checksum").HasMaxLength(200);
        builder.Property(item => item.PublishedAt).HasColumnName("published_at").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CourseVersion.CourseLanguageId), nameof(CourseVersion.VersionNumber)).IsUnique().HasDatabaseName("ix_course_versions_tenant_id_course_language_id_version_number");
    }
}
