using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Reception.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Reception.Persistence.Configuration;

public sealed class ReceptionKioskSessionConfiguration : IEntityTypeConfiguration<ReceptionKioskSession>
{
    public void Configure(EntityTypeBuilder<ReceptionKioskSession> builder)
    {
        builder.ToTable("reception_kiosk_sessions");

        builder.HasKey(session => session.Id).HasName("pk_reception_kiosk_sessions");

        builder.Property(session => session.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(session => session.KioskId).HasColumnName("kiosk_id").IsRequired();
        builder.Property(session => session.ArrivalId).HasColumnName("arrival_id").IsRequired();
        builder.Property(session => session.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(session => session.CurrentStep).HasColumnName("current_step").HasConversion<string>().HasMaxLength(50);
        builder.Property(session => session.StopReason).HasColumnName("stop_reason").HasConversion<string>().HasMaxLength(50);
        builder.Property(session => session.StopMessage).HasColumnName("stop_message").HasMaxLength(4000);
        builder.Property(session => session.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(session => session.LastInteractionAt).HasColumnName("last_interaction_at").IsRequired();
        builder.Property(session => session.CompletedAt).HasColumnName("completed_at");
        builder.Property(session => session.RetentionUntil).HasColumnName("retention_until").IsRequired();
        builder.Property(session => session.RequiresFacePicture).HasColumnName("requires_face_picture").IsRequired();
        builder.Property(session => session.RequiresIdentityDocumentCheck).HasColumnName("requires_identity_document_check").IsRequired();
        builder.Property(session => session.RequiresComplianceCheck).HasColumnName("requires_compliance_check").IsRequired();
        builder.Property(session => session.FacePictureStatus).HasColumnName("face_picture_status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(session => session.IdentityDocumentCheckStatus).HasColumnName("identity_document_check_status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(session => session.ComplianceCheckStatus).HasColumnName("compliance_check_status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(session => session.OnboardStatus).HasColumnName("onboard_status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(session => session.FacePictureStoragePath).HasColumnName("face_picture_storage_path").HasMaxLength(500);
        builder.Property(session => session.IdentityDocumentStoragePath).HasColumnName("identity_document_storage_path").HasMaxLength(500);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ReceptionKioskSession.KioskId), nameof(ReceptionKioskSession.StartedAt))
            .HasDatabaseName("ix_reception_kiosk_sessions_tenant_id_kiosk_id_started_at");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ReceptionKioskSession.ArrivalId), nameof(ReceptionKioskSession.StartedAt))
            .HasDatabaseName("ix_reception_kiosk_sessions_tenant_id_arrival_id_started_at");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ReceptionKioskSession.RetentionUntil))
            .HasDatabaseName("ix_reception_kiosk_sessions_tenant_id_retention_until");
    }
}
