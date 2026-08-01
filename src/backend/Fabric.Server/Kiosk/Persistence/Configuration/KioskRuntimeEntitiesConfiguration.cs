using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Kiosk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Kiosk.Persistence.Configuration;

public sealed class KioskAssetConfiguration : IEntityTypeConfiguration<KioskAsset>
{
    public void Configure(EntityTypeBuilder<KioskAsset> builder)
    {
        builder.ToTable("assets");
        builder.HasKey(asset => asset.Id).HasName("pk_kiosk_assets");
        builder.Property(asset => asset.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(asset => asset.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(asset => asset.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(asset => asset.LanguageCode).HasColumnName("language_code").HasMaxLength(20);
        builder.Property(asset => asset.Kind).HasColumnName("kind").HasConversion<string>().IsRequired().HasMaxLength(40);
        builder.Property(asset => asset.FileName).HasColumnName("file_name").IsRequired().HasMaxLength(300);
        builder.Property(asset => asset.ContentType).HasColumnName("content_type").IsRequired().HasMaxLength(200);
        builder.Property(asset => asset.Size).HasColumnName("size").IsRequired();
        builder.Property(asset => asset.RelativePath).HasColumnName("relative_path").IsRequired().HasMaxLength(800);
        builder.Property(asset => asset.Visibility).HasColumnName("visibility").HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(asset => asset.UploadedByOid).HasColumnName("uploaded_by_oid").HasMaxLength(200);
        builder.Property(asset => asset.UploadedByEmail).HasColumnName("uploaded_by_email").HasMaxLength(320);
        builder.Property(asset => asset.UploadedByDisplayName).HasColumnName("uploaded_by_display_name").HasMaxLength(300);
        builder.Property(asset => asset.AltTextKey).HasColumnName("alt_text_key").HasMaxLength(300);
        builder.Property(asset => asset.CreatedAt).HasColumnName("created_at").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(KioskAsset.ProfileId), nameof(KioskAsset.Name), nameof(KioskAsset.LanguageCode)).HasDatabaseName("ix_kiosk_assets_tenant_profile_name_language");
    }
}

public sealed class KioskDeviceAssignmentConfiguration : IEntityTypeConfiguration<KioskDeviceAssignment>
{
    public void Configure(EntityTypeBuilder<KioskDeviceAssignment> builder)
    {
        builder.ToTable("device_assignments");
        builder.HasKey(assignment => assignment.Id).HasName("pk_kiosk_device_assignments");
        builder.Property(assignment => assignment.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(assignment => assignment.KioskId).HasColumnName("kiosk_id").IsRequired();
        builder.Property(assignment => assignment.BindingKey).HasColumnName("binding_key").IsRequired().HasMaxLength(100);
        builder.Property(assignment => assignment.AgentId).HasColumnName("agent_id").IsRequired().HasMaxLength(100);
        builder.Property(assignment => assignment.DeviceId).HasColumnName("device_id").IsRequired().HasMaxLength(100);
        builder.Property(assignment => assignment.Enabled).HasColumnName("enabled").IsRequired();
        builder.Property(assignment => assignment.Priority).HasColumnName("priority").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(KioskDeviceAssignment.KioskId), nameof(KioskDeviceAssignment.BindingKey), nameof(KioskDeviceAssignment.Priority)).HasDatabaseName("ix_kiosk_device_assignments_tenant_kiosk_binding_priority").IsUnique();
    }
}

public sealed class KioskDeviceConfiguration : IEntityTypeConfiguration<KioskDevice>
{
    public void Configure(EntityTypeBuilder<KioskDevice> builder)
    {
        builder.ToTable("devices");
        builder.HasKey(device => device.Id).HasName("pk_kiosk_devices");
        builder.Property(device => device.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(device => device.KioskId).HasColumnName("kiosk_id").IsRequired();
        builder.Property(device => device.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(device => device.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(40);
        builder.Property(device => device.SlotNumber).HasColumnName("slot_number").IsRequired();
        builder.Property(device => device.AgentId).HasColumnName("agent_id").IsRequired().HasMaxLength(100);
        builder.Property(device => device.DeviceId).HasColumnName("device_id").IsRequired().HasMaxLength(100);
        builder.Property(device => device.Enabled).HasColumnName("enabled").IsRequired();
        builder.Property(device => device.CleanupOnSessionEnd).HasColumnName("cleanup_on_session_end").IsRequired();
        builder.Property(device => device.SortOrder).HasColumnName("sort_order").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(KioskDevice.KioskId), nameof(KioskDevice.Type), nameof(KioskDevice.SlotNumber)).HasDatabaseName("ix_kiosk_devices_tenant_kiosk_type_slot").IsUnique();
    }
}

public sealed class KioskSessionConfiguration : IEntityTypeConfiguration<KioskSession>
{
    public void Configure(EntityTypeBuilder<KioskSession> builder)
    {
        builder.ToTable("sessions");
        builder.HasKey(session => session.Id).HasName("pk_kiosk_sessions");
        builder.Property(session => session.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(session => session.KioskId).HasColumnName("kiosk_id").IsRequired();
        builder.Property(session => session.WorkflowInstanceId).HasColumnName("workflow_instance_id").HasMaxLength(200);
        builder.Property(session => session.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(40);
        builder.Property(session => session.LanguageCode).HasColumnName("language_code").IsRequired().HasMaxLength(20);
        builder.Property(session => session.CurrentInstructionJson).HasColumnName("current_instruction_json").HasColumnType("jsonb");
        builder.Property(session => session.CurrentInstructionVersion).HasColumnName("current_instruction_version").IsRequired();
        builder.Property(session => session.CurrentInstructionId).HasColumnName("current_instruction_id").HasMaxLength(100);
        builder.Property(session => session.TerminalTitle).HasColumnName("terminal_title").HasMaxLength(200);
        builder.Property(session => session.TerminalMessage).HasColumnName("terminal_message").HasMaxLength(4000);
        builder.Property(session => session.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(session => session.LastInteractionAt).HasColumnName("last_interaction_at").IsRequired();
        builder.Property(session => session.CompletedAt).HasColumnName("completed_at");
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(KioskSession.KioskId), nameof(KioskSession.Status)).HasDatabaseName("ix_kiosk_sessions_tenant_kiosk_status");
    }
}
