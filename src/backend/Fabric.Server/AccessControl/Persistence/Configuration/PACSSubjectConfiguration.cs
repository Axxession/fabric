using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessControl.Persistence.Configuration;

public sealed class PACSSubjectConfiguration : IEntityTypeConfiguration<PACSSubject>
{
    public void Configure(EntityTypeBuilder<PACSSubject> builder)
    {
        builder.ToTable("pacs_subjects");

        builder.HasKey(subject => subject.Id).HasName("pk_pacs_subjects");

        builder.Property(subject => subject.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(subject => subject.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(subject => subject.AccessControlSystemId).HasColumnName("access_control_system_id").IsRequired();
        builder.Property(subject => subject.NativeSubjectId).HasColumnName("native_subject_id").HasMaxLength(200).IsRequired();
        builder.Property(subject => subject.State).HasColumnName("state").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(subject => subject.FirstName).HasColumnName("first_name").HasMaxLength(200).IsRequired();
        builder.Property(subject => subject.LastName).HasColumnName("last_name").HasMaxLength(200).IsRequired();
        builder.Property(subject => subject.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(subject => subject.LastSynchronizedAt).HasColumnName("last_synchronized_at").IsRequired();

        builder.HasOne<AccessControlSystem>()
            .WithMany()
            .HasForeignKey(subject => subject.AccessControlSystemId)
            .HasConstraintName("fk_pacs_subjects_access_control_systems_access_control_system_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSSubject.AccessControlSystemId), nameof(PACSSubject.IdentityId))
            .IsUnique()
            .HasDatabaseName("ix_pacs_subjects_tenant_id_system_id_identity_id");

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSSubject.AccessControlSystemId), nameof(PACSSubject.NativeSubjectId))
            .IsUnique()
            .HasDatabaseName("ix_pacs_subjects_tenant_id_system_id_native_subject_id");
    }
}
