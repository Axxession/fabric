using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Core;

namespace Fabric.Server.Tests.AccessControl.Domain;

public sealed class PACSSubjectTests
{
    [Fact]
    public void GetProvisioningBlockStatus_WhenManualBlockExists_ReturnsBlockedManual()
    {
        PACSSubject subject = CreateSubject();

        Result<AccessControlErrors> block = subject.BlockProvisioningManually("Legacy PACS record must be cleaned.", DateTimeOffset.UtcNow);

        Assert.True(block.IsSuccess(out _));
        subject.ApplyConformityCheck(PACSSubjectConformityStatus.Anomaly, "Unexpected access rule site=1, rule=2", DateTimeOffset.UtcNow);

        Assert.Equal(PACSSubjectProvisioningBlockStatus.BlockedManual, subject.GetProvisioningBlockStatus(AnomalyBlockMode.BlockProvisioning));
        Assert.Equal("Legacy PACS record must be cleaned.", subject.GetProvisioningBlockedReason(AnomalyBlockMode.BlockProvisioning));
    }

    [Fact]
    public void GetProvisioningBlockStatus_WhenAnomalyPolicyBlocks_ReturnsBlockedByAnomaly()
    {
        PACSSubject subject = CreateSubject();

        subject.ApplyConformityCheck(PACSSubjectConformityStatus.Anomaly, "Unexpected credential 123456", DateTimeOffset.UtcNow);

        Assert.Equal(PACSSubjectProvisioningBlockStatus.BlockedByAnomaly, subject.GetProvisioningBlockStatus(AnomalyBlockMode.BlockProvisioning));
        Assert.Equal("Unexpected credential 123456", subject.GetProvisioningBlockedReason(AnomalyBlockMode.BlockProvisioning));
    }

    [Fact]
    public void GetProvisioningBlockStatus_WhenAnomalyPolicyWarns_ReturnsProvisioningAllowed()
    {
        PACSSubject subject = CreateSubject();

        subject.ApplyConformityCheck(PACSSubjectConformityStatus.Anomaly, "Unexpected credential 123456", DateTimeOffset.UtcNow);

        Assert.Equal(PACSSubjectProvisioningBlockStatus.ProvisioningAllowed, subject.GetProvisioningBlockStatus(AnomalyBlockMode.WarnOnly));
        Assert.Null(subject.GetProvisioningBlockedReason(AnomalyBlockMode.WarnOnly));
    }

    private static PACSSubject CreateSubject() =>
        PACSSubject.Create(Guid.NewGuid(), Guid.NewGuid(), "1001", PACSSubjectState.Active, "Ada", "Lovelace", "ada@example.com", DateTimeOffset.UtcNow);
}
