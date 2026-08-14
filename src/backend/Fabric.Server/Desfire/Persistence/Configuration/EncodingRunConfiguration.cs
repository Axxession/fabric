using Fabric.Server.Desfire.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Desfire.Persistence.Configuration;

public sealed class BadgeJobConfiguration : IEntityTypeConfiguration<BadgeJob>
{
    public void Configure(EntityTypeBuilder<BadgeJob> builder)
    {
        builder.ToTable("badge_jobs");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).ValueGeneratedNever();
        builder.Property(run => run.TransformationId);
        builder.Property(run => run.BatchId);
        builder.Property(run => run.EncoderId);
        builder.Property(run => run.PrintDesignId);
        builder.Property(run => run.KioskSessionId);
        builder.Property(run => run.Kind).HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(run => run.Source).HasMaxLength(100);
        builder.Property(run => run.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(run => run.InputJson).IsRequired().HasColumnType("jsonb");
        builder.Property(run => run.ResolvedVariablesJson).IsRequired().HasColumnType("jsonb");
        builder.Property(run => run.PlanSummaryJson).IsRequired().HasColumnType("jsonb");
        builder.Property(run => run.CommandAuditJson).IsRequired().HasColumnType("jsonb");
        builder.Property(run => run.CardUid).HasMaxLength(64);
        builder.Property(run => run.HardwareAgentId).HasMaxLength(100);
        builder.Property(run => run.DeviceId).HasMaxLength(100);
        builder.Property(run => run.RequestedAgentId).HasMaxLength(100);
        builder.Property(run => run.RequestedDeviceId).HasMaxLength(100);
        builder.Property(run => run.VariableConfigJson).IsRequired().HasColumnType("jsonb");
        builder.Property(run => run.Priority).IsRequired();
        builder.Property(run => run.AttemptCount).IsRequired();
        builder.Property(run => run.ClaimedBy).HasMaxLength(200);
        builder.Property(run => run.ClaimedAt);
        builder.Property(run => run.ClaimExpiresAt);
        builder.Property(run => run.ErrorMessage).HasMaxLength(4000);
        builder.Property(run => run.RequestedAt).IsRequired();
        builder.HasIndex(run => run.TransformationId);
        builder.HasIndex(run => run.BatchId);
        builder.HasIndex(run => run.EncoderId);
        builder.HasIndex(run => run.PrintDesignId);
        builder.HasIndex(run => run.KioskSessionId);
        builder.HasIndex(run => run.Source);
        builder.HasIndex(run => run.CardUid);
        builder.HasIndex(run => new { run.Status, run.Priority, run.RequestedAt });
        builder.HasIndex(run => new { run.RequestedAgentId, run.RequestedDeviceId });
    }
}
