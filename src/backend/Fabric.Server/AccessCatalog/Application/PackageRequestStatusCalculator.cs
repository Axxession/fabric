using Fabric.Server.AccessCatalog.Domain;

namespace Fabric.Server.AccessCatalog.Application;

internal static class PackageRequestStatusCalculator
{
    public static void ApplySummary(PackageRequest request, IReadOnlyList<ApprovalFlow> flows, DateTimeOffset now)
    {
        if (flows.Count == 0 || flows.Any(item => item.Status == ApprovalFlowStatus.InProgress))
            return;

        bool anyApproved = flows.Any(item => item.Status is ApprovalFlowStatus.Approved or ApprovalFlowStatus.SystemApproved);
        bool anyRejected = flows.Any(item => item.Status == ApprovalFlowStatus.Rejected);
        bool anyExpired = flows.Any(item => item.Status == ApprovalFlowStatus.Expired);

        PackageRequestSubStatus subStatus = anyApproved
            ? (anyRejected || anyExpired ? PackageRequestSubStatus.PartiallyApproved : PackageRequestSubStatus.Approved)
            : anyExpired
                ? PackageRequestSubStatus.Expired
                : PackageRequestSubStatus.Rejected;

        request.MarkCompleted(subStatus, now);
    }
}
