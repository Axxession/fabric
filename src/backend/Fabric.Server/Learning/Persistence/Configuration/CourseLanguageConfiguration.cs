using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Learning.Persistence.Configuration;

public sealed class CourseLanguageConfiguration : IEntityTypeConfiguration<CourseLanguage>
{
    public void Configure(EntityTypeBuilder<CourseLanguage> builder)
    {
        builder.ToTable("course_languages");
        builder.HasKey(item => item.Id).HasName("pk_course_languages");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(item => item.LanguageCode).HasColumnName("language_code").HasMaxLength(32).IsRequired();
        builder.Property(item => item.DisplayLabel).HasColumnName("display_label").HasMaxLength(200).IsRequired();
        builder.Property(item => item.CurrentVersionId).HasColumnName("current_version_id");
        builder.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CourseLanguage.CourseId), nameof(CourseLanguage.LanguageCode)).IsUnique().HasDatabaseName("ix_course_languages_tenant_id_course_id_language_code");
    }
}
