using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Sagas.AccessGrantProvisioning;

namespace Fabric.Server.AccessCatalog.Application;

public static class GrantProvisioningStatusResolver
{
    public static GrantProvisioningStatus Resolve(
        AccessGrant grant,
        bool isComplianceRequired,
        AccessGrantProvisioningSagaState? sagaState,
        IReadOnlyCollection<AccessGrantMaterializationOutcome> materializationOutcomes,
        DateTimeOffset now)
    {
        if (!AccessGrantComplianceService.IsProvisionable(grant, isComplianceRequired, now))
            return GrantProvisioningStatus.NonProvisionable;

        bool materiallyProvisioned = materializationOutcomes.Count > 0
            && materializationOutcomes.All(item => item.Status == AccessGrantMaterializationOutcomeStatus.Created);

        return sagaState == AccessGrantProvisioningSagaState.Provisioned && materiallyProvisioned
            ? GrantProvisioningStatus.Provisioned
            : GrantProvisioningStatus.Provisioning;
    }
}
