using Fabric.Server.Core;

namespace Fabric.Server.AccessControl.Domain;

public abstract class AccessLevelTarget
{
    private protected AccessLevelTarget() { }

    public Guid Id { get; protected set; }
    public Guid AccessItemId { get; protected set; }
    public Guid AccessControlSystemId { get; protected set; }
    public string Name { get; protected set; } = null!;
    public bool IsEnabled { get; protected set; }
    public ProvisioningTiming ProvisioningTiming { get; protected set; }

    public void UpdateName(string name) => Name = name;

    public void SetEnabled(bool enabled) => IsEnabled = enabled;

    public abstract bool MatchesNativeTarget(AccessLevelTarget other);
}

public sealed class UnipassAccessLevelTarget : AccessLevelTarget
{
    private UnipassAccessLevelTarget() { }

    public int AccessRuleId { get; private set; }
    public int SiteId { get; private set; }
    public string AccessRuleName { get; private set; } = null!;
    public string SiteName { get; private set; } = null!;

    public static UnipassAccessLevelTarget Create(
        Guid accessItemId,
        Guid accessControlSystemId,
        string name,
        int accessRuleId,
        int siteId,
        string accessRuleName,
        string siteName,
        ProvisioningTiming provisioningTiming) =>
        new()
        {
            Id = Guid.NewGuid(),
            AccessItemId = accessItemId,
            AccessControlSystemId = accessControlSystemId,
            Name = name,
            IsEnabled = true,
            ProvisioningTiming = provisioningTiming,
            AccessRuleId = accessRuleId,
            SiteId = siteId,
            AccessRuleName = accessRuleName,
            SiteName = siteName
        };

    public Result<AccessControlErrors> Update(
        string name,
        int accessRuleId,
        int siteId,
        string accessRuleName,
        string siteName,
        ProvisioningTiming provisioningTiming)
    {
        Name = name;
        AccessRuleId = accessRuleId;
        SiteId = siteId;
        AccessRuleName = accessRuleName;
        SiteName = siteName;
        ProvisioningTiming = provisioningTiming;
        return Result.Success<AccessControlErrors>();
    }

    public override bool MatchesNativeTarget(AccessLevelTarget other) =>
        other is UnipassAccessLevelTarget target &&
        AccessControlSystemId == target.AccessControlSystemId &&
        AccessRuleId == target.AccessRuleId &&
        SiteId == target.SiteId;
}
