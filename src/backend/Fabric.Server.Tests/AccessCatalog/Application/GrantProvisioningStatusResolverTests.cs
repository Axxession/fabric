using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Core;
using Fabric.Server.Sagas.AccessGrantProvisioning;

namespace Fabric.Server.Tests.AccessCatalog.Application;

public sealed class GrantProvisioningStatusResolverTests
{
    [Fact]
    public void Resolve_WhenComplianceRequiredAndNonCompliant_ReturnsNonProvisionable()
    {
        AccessGrant grant = CreateGrant();

        Assert.True(grant.UpdateCompliance(GrantComplianceStatus.NonCompliant, null, DateTimeOffset.UtcNow).IsSuccess(out _));

        GrantProvisioningStatus status = GrantProvisioningStatusResolver.Resolve(
            grant,
            true,
            null,
            [],
            DateTimeOffset.UtcNow);

        Assert.Equal(GrantProvisioningStatus.NonProvisionable, status);
    }

    [Fact]
    public void Resolve_WhenComplianceNotRequiredAndNotYetConverged_ReturnsProvisioning()
    {
        AccessGrant grant = CreateGrant();

        Assert.True(grant.UpdateCompliance(GrantComplianceStatus.NonCompliant, null, DateTimeOffset.UtcNow).IsSuccess(out _));

        GrantProvisioningStatus status = GrantProvisioningStatusResolver.Resolve(
            grant,
            false,
            AccessGrantProvisioningSagaState.PendingProvision,
            [],
            DateTimeOffset.UtcNow);

        Assert.Equal(GrantProvisioningStatus.Provisioning, status);
    }

    [Fact]
    public void Resolve_WhenGrantProvisionedAndMaterialized_ReturnsProvisioned()
    {
        AccessGrant grant = CreateGrant();

        Assert.True(grant.UpdateCompliance(GrantComplianceStatus.Compliant, null, DateTimeOffset.UtcNow).IsSuccess(out _));

        GrantProvisioningStatus status = GrantProvisioningStatusResolver.Resolve(
            grant,
            true,
            AccessGrantProvisioningSagaState.Provisioned,
            [new AccessGrantMaterializationOutcome
            {
                Id = Guid.NewGuid(),
                AccessGrantId = grant.Id,
                AccessItemId = grant.AccessItemId!.Value,
                LocationId = grant.LocationId,
                Status = AccessGrantMaterializationOutcomeStatus.Created
            }],
            DateTimeOffset.UtcNow);

        Assert.Equal(GrantProvisioningStatus.Provisioned, status);
    }

    private static AccessGrant CreateGrant()
    {
        Result<AccessGrant, AccessCatalogErrors> create = AccessGrant.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AssignmentChannel.Manual,
            AssignmentSourceKind.Manual,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid(),
            AccessDurationKind.Permanent,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            null,
            GrantApprovalStatus.NotRequired,
            "Manual grant");

        Assert.True(create.IsSuccess(out AccessGrant? grant));
        return grant!;
    }
}
