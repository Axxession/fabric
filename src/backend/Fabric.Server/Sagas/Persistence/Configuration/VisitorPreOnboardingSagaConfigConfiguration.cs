using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Sagas.VisitorPreOnboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Sagas.Persistence.Configuration;

public sealed class VisitorPreOnboardingSagaConfigConfiguration : IEntityTypeConfiguration<VisitorPreOnboardingSagaConfig>
{
    public void Configure(EntityTypeBuilder<VisitorPreOnboardingSagaConfig> builder)
    {
        builder.ToTable("visitor_pre_onboarding_saga_configs");

        builder.HasKey(x => x.Id).HasName("pk_visitor_pre_onboarding_saga_configs");

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.UseCustomInviteNotification).HasColumnName("use_custom_invite_notification").IsRequired();
        ConfigureCustomNotification(builder, x => x.CustomInviteNotification, "custom_invite_notification");
        builder.Property(x => x.QrCredentialTypeId).HasColumnName("qr_credential_type_id");
        builder.Property(x => x.GraceStartMinutes).HasColumnName("grace_start_minutes").IsRequired();
        builder.Property(x => x.GraceEndMinutes).HasColumnName("grace_end_minutes").IsRequired();
        builder.Property(x => x.SendConfirmNotificationToHost).HasColumnName("send_confirm_notification_to_host").IsRequired();
        builder.Property(x => x.UseCustomConfirmNotification).HasColumnName("use_custom_confirm_notification").IsRequired();
        ConfigureCustomNotification(builder, x => x.CustomConfirmNotification, "custom_confirm_notification");
        builder.Property(x => x.SendCancellationNotification).HasColumnName("send_cancellation_notification").IsRequired();
        builder.Property(x => x.UseCustomCancellationNotification).HasColumnName("use_custom_cancellation_notification").IsRequired();
        ConfigureCustomNotification(builder, x => x.CustomCancellationNotification, "custom_cancellation_notification");
        builder.Property(x => x.SendRescheduleNotification).HasColumnName("send_reschedule_notification").IsRequired();
        builder.Property(x => x.UseCustomRescheduleNotification).HasColumnName("use_custom_reschedule_notification").IsRequired();
        ConfigureCustomNotification(builder, x => x.CustomRescheduleNotification, "custom_reschedule_notification");
        builder.Property(x => x.SendRelocationNotification).HasColumnName("send_relocation_notification").IsRequired();
        builder.Property(x => x.UseCustomRelocationNotification).HasColumnName("use_custom_relocation_notification").IsRequired();
        ConfigureCustomNotification(builder, x => x.CustomRelocationNotification, "custom_relocation_notification");
        builder.Property(x => x.SendArrivalNotificationToHost).HasColumnName("send_arrival_notification_to_host").IsRequired();
        builder.Property(x => x.UseCustomArrivalNotification).HasColumnName("use_custom_arrival_notification").IsRequired();
        ConfigureCustomNotification(builder, x => x.CustomArrivalNotification, "custom_arrival_notification");

        builder.ToTable(x =>
        {
            AddCustomNotificationCheckConstraint(x, "invite", "custom_invite_notification");
            AddCustomNotificationCheckConstraint(x, "confirm", "custom_confirm_notification");
            AddCustomNotificationCheckConstraint(x, "cancellation", "custom_cancellation_notification");
            AddCustomNotificationCheckConstraint(x, "reschedule", "custom_reschedule_notification");
            AddCustomNotificationCheckConstraint(x, "relocation", "custom_relocation_notification");
            AddCustomNotificationCheckConstraint(x, "arrival", "custom_arrival_notification");
        });

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName)
            .IsUnique()
            .HasDatabaseName("ix_visitor_pre_onboarding_saga_configs_tenant_id");
    }

    private static void ConfigureCustomNotification(
        EntityTypeBuilder<VisitorPreOnboardingSagaConfig> builder,
        System.Linq.Expressions.Expression<Func<VisitorPreOnboardingSagaConfig, CustomNotification?>> navigationExpression,
        string columnPrefix)
    {
        builder.OwnsOne(navigationExpression, notification =>
        {
            notification.Property(x => x.Subject).HasColumnName($"{columnPrefix}_subject");
            notification.Property(x => x.Body).HasColumnName($"{columnPrefix}_body");
        });
    }

    private static void AddCustomNotificationCheckConstraint(TableBuilder<VisitorPreOnboardingSagaConfig> table, string notificationName, string columnPrefix)
    {
        table.HasCheckConstraint(
            $"ck_vpo_config_{notificationName}_notification_all_or_null",
            $"({columnPrefix}_subject IS NULL) = ({columnPrefix}_body IS NULL)");
    }
}
