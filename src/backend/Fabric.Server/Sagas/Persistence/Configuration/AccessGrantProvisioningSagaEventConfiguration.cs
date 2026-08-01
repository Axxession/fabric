using Fabric.Server.Sagas.AccessGrantProvisioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Sagas.Persistence.Configuration;

public sealed class AccessGrantProvisioningSagaEventConfiguration : IEntityTypeConfiguration<AccessGrantProvisioningSagaEvent>
{
    public void Configure(EntityTypeBuilder<AccessGrantProvisioningSagaEvent> builder)
    {
        builder.ToTable("access_grant_provisioning_saga_events");
        builder.HasKey(item => item.Id).HasName("pk_access_grant_provisioning_saga_events");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.SagaId).HasColumnName("saga_id").IsRequired();
        builder.Property(item => item.AccessGrantId).HasColumnName("access_grant_id").IsRequired();
        builder.Property(item => item.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.NextRetryAt).HasColumnName("next_retry_at");
        builder.Property(item => item.RetryCount).HasColumnName("retry_count").IsRequired();
        builder.Property(item => item.ProcessedAt).HasColumnName("processed_at");
        builder.Property(item => item.FailureReason).HasColumnName("failure_reason").HasMaxLength(2_000);
        builder.HasIndex(item => item.ProcessedAt).HasDatabaseName("ix_access_grant_provisioning_saga_events_processed_at");
        builder.HasIndex(item => item.NextRetryAt).HasDatabaseName("ix_access_grant_provisioning_saga_events_next_retry_at");
        builder.HasIndex(item => item.CreatedAt).HasDatabaseName("ix_access_grant_provisioning_saga_events_created_at");
    }
}
