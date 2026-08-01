using Fabric.Server.Sagas.AccessGrantProvisioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Sagas.Persistence.Configuration;

public sealed class AccessGrantProvisioningSagaConfiguration : IEntityTypeConfiguration<AccessGrantProvisioningSaga>
{
    public void Configure(EntityTypeBuilder<AccessGrantProvisioningSaga> builder)
    {
        builder.ToTable("access_grant_provisioning_sagas");
        builder.HasKey(item => item.Id).HasName("pk_access_grant_provisioning_sagas");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AccessGrantId).HasColumnName("access_grant_id").IsRequired();
        builder.Property(item => item.State).HasColumnName("state").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.FailureReason).HasColumnName("failure_reason").HasMaxLength(2_000);
        builder.Property(item => item.RetryCount).HasColumnName("retry_count").IsRequired();
        builder.Property(item => item.NextRetryAt).HasColumnName("next_retry_at");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasIndex(item => item.AccessGrantId).HasDatabaseName("ix_access_grant_provisioning_sagas_access_grant_id").IsUnique();
        builder.HasIndex(item => item.NextRetryAt).HasDatabaseName("ix_access_grant_provisioning_sagas_next_retry_at");
    }
}
