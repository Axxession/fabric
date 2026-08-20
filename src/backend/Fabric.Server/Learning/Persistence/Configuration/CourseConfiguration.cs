using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Learning.Persistence.Configuration;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");
        builder.HasKey(item => item.Id).HasName("pk_courses");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(item => item.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(4_000);
        builder.Property(item => item.CurrentVersionId).HasColumnName("current_version_id");
        builder.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Course.Code)).IsUnique().HasDatabaseName("ix_courses_tenant_id_code");
    }
}
