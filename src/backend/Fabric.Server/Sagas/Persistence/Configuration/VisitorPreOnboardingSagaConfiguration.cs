using Fabric.Server.Sagas.VisitorPreOnboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Sagas.Persistence.Configuration;

public sealed class VisitorPreOnboardingSagaConfiguration : IEntityTypeConfiguration<VisitorPreOnboardingSaga>
{
    public void Configure(EntityTypeBuilder<VisitorPreOnboardingSaga> builder)
    {
        builder.ToTable("visitor_pre_onboarding_sagas");

        builder.HasKey(x => x.Id).HasName("pk_visitor_pre_onboarding_sagas");

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.VisitId).HasColumnName("visit_id").IsRequired();
        builder.Property(x => x.InvitationId).HasColumnName("invitation_id").IsRequired();
        builder.Property(x => x.ArrivalId).HasColumnName("arrival_id");
        builder.Property(x => x.AccessPolicyId).HasColumnName("access_policy_id");
        builder.Property(x => x.CredentialId).HasColumnName("credential_id");
        builder.Property(x => x.QrCode).HasColumnName("qr_code").HasMaxLength(2000);
        builder.Property(x => x.ArrivalNotificationSentAt).HasColumnName("arrival_notification_sent_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.NextRetryAt).HasColumnName("next_retry_at");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count").IsRequired();
        builder.Property(x => x.InvitationSentAt).HasColumnName("invitation_sent_at");
        builder.Property(x => x.State).HasColumnName("state").IsRequired().HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(x => new { x.VisitId, x.InvitationId }).HasDatabaseName("ix_visitor_pre_onboarding_sagas_visit_invitation");
        builder.HasIndex(x => x.State).HasDatabaseName("ix_visitor_pre_onboarding_sagas_state");
        builder.HasIndex(x => x.NextRetryAt).HasDatabaseName("ix_visitor_pre_onboarding_sagas_next_retry_at");
    }
}
