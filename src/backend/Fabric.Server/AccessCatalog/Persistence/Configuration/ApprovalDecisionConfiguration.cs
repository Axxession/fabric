using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class ApprovalDecisionConfiguration : IEntityTypeConfiguration<ApprovalDecision>
{
    public void Configure(EntityTypeBuilder<ApprovalDecision> builder)
    {
        builder.ToTable("approval_decisions");
        builder.HasKey(item => item.Id).HasName("pk_approval_decisions");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.RequestId).HasColumnName("request_id").IsRequired();
        builder.Property(item => item.ApprovalRequirementId).HasColumnName("approval_requirement_id").IsRequired();
        builder.Property(item => item.ApproverIdentityId).HasColumnName("approver_identity_id").IsRequired();
        builder.Property(item => item.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.DecisionKind).HasColumnName("decision_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.Note).HasColumnName("note").HasMaxLength(2_000);
        builder.Property(item => item.DecidedAt).HasColumnName("decided_at").IsRequired();

        builder.HasOne<PackageRequest>()
            .WithMany()
            .HasForeignKey(item => item.RequestId)
            .HasConstraintName("fk_approval_decisions_package_requests_request_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApprovalRequirement>()
            .WithMany()
            .HasForeignKey(item => item.ApprovalRequirementId)
            .HasConstraintName("fk_approval_decisions_approval_requirements_approval_requirement_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalDecision.RequestId))
            .HasDatabaseName("ix_approval_decisions_tenant_id_request_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalDecision.ApprovalRequirementId))
            .HasDatabaseName("ix_approval_decisions_tenant_id_approval_requirement_id");
    }
}
