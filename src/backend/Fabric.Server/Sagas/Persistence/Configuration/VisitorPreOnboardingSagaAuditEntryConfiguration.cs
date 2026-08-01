using Fabric.Server.Sagas.VisitorPreOnboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Sagas.Persistence.Configuration;

public sealed class VisitorPreOnboardingSagaAuditEntryConfiguration : IEntityTypeConfiguration<VisitorPreOnboardingSagaAuditEntry>
{
    public void Configure(EntityTypeBuilder<VisitorPreOnboardingSagaAuditEntry> builder)
    {
        builder.ToTable("visitor_pre_onboarding_saga_audit_entries");

        builder.HasKey(x => x.Id).HasName("pk_visitor_pre_onboarding_saga_audit_entries");

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SagaId).HasColumnName("saga_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").IsRequired().HasConversion<string>().HasMaxLength(100);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(x => x.DetailsJson).HasColumnName("details_json").HasMaxLength(4000);

        builder.HasIndex(x => x.SagaId).HasDatabaseName("ix_vpo_saga_audit_entries_saga_id");
        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_vpo_saga_audit_entries_occurred_at");
    }
}
