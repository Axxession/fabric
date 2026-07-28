using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Reception.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Reception.Persistence.Configuration;

public sealed class ExpectedArrivalConfiguration : IEntityTypeConfiguration<ExpectedArrival>
{
    public void Configure(EntityTypeBuilder<ExpectedArrival> builder)
    {
        builder.ToTable("expected_arrivals");

        builder.HasKey(a => a.Id).HasName("pk_expected_arrivals");

        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(a => a.Type).HasColumnName("type").IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.ExpectedArrivalTime).HasColumnName("expected_arrival_time").IsRequired();
        builder.Property(a => a.ExpectedOffboardTime).HasColumnName("expected_offboard_time").IsRequired();
        builder.Property(a => a.ArrivalCode).HasColumnName("arrival_code").HasMaxLength(200);
        builder.Property(a => a.Status).HasColumnName("status").IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.OnboardedAt).HasColumnName("onboarded_at");
        builder.Property(a => a.OffboardedAt).HasColumnName("offboarded_at");
        builder.Property(a => a.CheckedIn).HasColumnName("checked_in").IsRequired();
        builder.Property(a => a.LocationId).HasColumnName("location_id");
        builder.Property(a => a.IdentityId).HasColumnName("identity_id");

        builder.Property(a => a.Confirmed).HasColumnName("confirmed");
        builder.Property(a => a.VisitorId).HasColumnName("visitor_id");
        builder.Property(a => a.InvitationId).HasColumnName("invitation_id");

        builder.Property(a => a.ContractorId).HasColumnName("contractor_id");
        builder.Property(a => a.JobAssignmentId).HasColumnName("job_assignment_id");

        builder.Property(a => a.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(200);
        builder.Property(a => a.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(200);
        builder.Property(a => a.Company).HasColumnName("company").HasMaxLength(200);

        ConfigureActor(builder.OwnsOne(a => a.OnboardedBy), "onboarded_by");
        ConfigureActor(builder.OwnsOne(a => a.OffboardedBy), "offboarded_by");

        builder.HasMany(a => a.Entries).WithOne().HasForeignKey("expected_arrival_id").HasConstraintName("fk_arrival_entries_expected_arrivals_expected_arrival_id").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(a => a.Documents).WithOne().HasForeignKey("expected_arrival_id").HasConstraintName("fk_check_in_documents_expected_arrivals_expected_arrival_id").OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ExpectedArrival.ArrivalCode))
            .HasDatabaseName("ix_expected_arrivals_tenant_id_arrival_code");
        builder.HasIndex(a => a.IdentityId).HasDatabaseName("ix_expected_arrivals_identity_id");
        builder.HasIndex(a => a.VisitorId).HasDatabaseName("ix_expected_arrivals_visitor_id");
        builder.HasIndex(a => a.ContractorId).HasDatabaseName("ix_expected_arrivals_contractor_id");
        builder.HasIndex(a => a.LocationId).HasDatabaseName("ix_expected_arrivals_location_id");
    }

    private static void ConfigureActor(OwnedNavigationBuilder<ExpectedArrival, ReceptionActor> builder, string prefix)
    {
        builder.Property(actor => actor.Type).HasColumnName($"{prefix}_type").HasConversion<string>().HasMaxLength(20);
        builder.Property(actor => actor.Identifier).HasColumnName($"{prefix}_identifier").HasMaxLength(320);
        builder.Property(actor => actor.DisplayName).HasColumnName($"{prefix}_display_name").HasMaxLength(200);
    }
}
