using Fabric.Server.AccessControl.Domain;

namespace Fabric.Server.Tests.AccessControl.Domain;

public sealed class AccessItemTests
{
    [Fact]
    public void Create_DefaultsComplianceRequirementToTrue()
    {
        AccessItem item = AccessItem.Create("Parking", null);

        Assert.True(item.IsComplianceRequired);
    }

    [Fact]
    public void Update_ChangesComplianceRequirement()
    {
        AccessItem item = AccessItem.Create("Parking", null);

        item.Update("Parking", "Visitor parking", false);

        Assert.False(item.IsComplianceRequired);
    }
}
