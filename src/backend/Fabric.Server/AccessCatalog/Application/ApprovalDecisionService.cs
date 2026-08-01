using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Application;

public sealed class ApprovalDecisionService(
    AccessCatalogDbContext db,
    AccessGrantService accessGrantService,
    TimeProvider timeProvider)
{
    public async Task<Result<ApprovalDecision, AccessCatalogErrors>> DecideAsync(
        Guid approvalRequirementId,
        Guid approverIdentityId,
        ApprovalDecisionKind decisionKind,
        string? note,
        CancellationToken cancellationToken = default)
    {
        ApprovalRequirement? requirement = await db.ApprovalRequirements.SingleOrDefaultAsync(item => item.Id == approvalRequirementId, cancellationToken);
        if (requirement is null)
            return Result.Failure<ApprovalDecision, AccessCatalogErrors>(AccessCatalogErrors.ApprovalRequirementNotFound);

        if (requirement.Status != ApprovalStatus.Pending)
            return Result.Failure<ApprovalDecision, AccessCatalogErrors>(AccessCatalogErrors.ApprovalRequirementAlreadyCompleted);

        ApprovalFlow? flow = await db.ApprovalFlows.SingleOrDefaultAsync(item => item.Id == requirement.ApprovalFlowId, cancellationToken);
        if (flow is null)
            return Result.Failure<ApprovalDecision, AccessCatalogErrors>(AccessCatalogErrors.ApprovalRequirementNotFound);

        PackageRequest? request = await db.PackageRequests.SingleOrDefaultAsync(item => item.Id == requirement.RequestId, cancellationToken);
        if (request is null)
            return Result.Failure<ApprovalDecision, AccessCatalogErrors>(AccessCatalogErrors.PackageRequestNotFound);

        if (request.Status != PackageRequestStatus.InProgress || flow.Status != ApprovalFlowStatus.InProgress)
            return Result.Failure<ApprovalDecision, AccessCatalogErrors>(AccessCatalogErrors.ApprovalDecisionNotAllowed);

        if (!await CanApproveAsync(requirement, approverIdentityId, cancellationToken))
            return Result.Failure<ApprovalDecision, AccessCatalogErrors>(AccessCatalogErrors.ApprovalDecisionNotAllowed);

        DateTimeOffset now = timeProvider.GetUtcNow();
        ApprovalDecision decision = ApprovalDecision.Create(
            request.Id,
            requirement.Id,
            approverIdentityId,
            requirement.Role,
            decisionKind,
            note,
            now);

        db.ApprovalDecisions.Add(decision);

        switch (decisionKind)
        {
            case ApprovalDecisionKind.Approve:
                requirement.MarkApproved(now);
                break;
            case ApprovalDecisionKind.Reject:
                requirement.MarkRejected(now);
                break;
        }

        List<ApprovalRequirement> flowRequirements = await db.ApprovalRequirements
            .Where(item => item.ApprovalFlowId == flow.Id)
            .ToListAsync(cancellationToken);

        bool anyRejected = flowRequirements.Any(item => item.Status == ApprovalStatus.Rejected);
        bool allApproved = flowRequirements.All(item => item.Status == ApprovalStatus.Approved || item.Status == ApprovalStatus.SystemApproved);

        if (anyRejected)
            flow.MarkRejected(now);
        else if (allApproved)
            flow.MarkApproved(now);

        if (flow.Status == ApprovalFlowStatus.Approved)
        {
            List<PackageRequestScope> scopes = await db.PackageRequestScopes
                .Where(item => item.ApprovalFlowId == flow.Id)
                .ToListAsync(cancellationToken);

            foreach (PackageRequestScope scope in scopes)
            {
                bool grantExists = await db.AccessGrants.AnyAsync(item => item.RequestScopeId == scope.Id, cancellationToken);
                if (grantExists)
                    continue;

                Result<AccessGrant, AccessCatalogErrors> grantResult = await accessGrantService.CreateForRequestScopeAsync(
                    request.PackageId,
                    flow.AccessItemId,
                    request.BeneficiaryIdentityId,
                    scope.RequestedLocationId,
                    request.Id,
                    flow.Id,
                    scope.Id,
                    request.DurationKind,
                    request.ValidFrom,
                    request.ValidUntil,
                    request.RequestReason,
                    cancellationToken);

                if (grantResult.IsFailure(out AccessCatalogErrors grantError))
                    return Result.Failure<ApprovalDecision, AccessCatalogErrors>(grantError);
            }
        }

        List<ApprovalFlow> flows = await db.ApprovalFlows
            .Where(item => item.RequestId == request.Id)
            .ToListAsync(cancellationToken);
        PackageRequestStatusCalculator.ApplySummary(request, flows, now);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ApprovalDecision, AccessCatalogErrors>(decision);
    }

    private async Task<bool> CanApproveAsync(ApprovalRequirement requirement, Guid approverIdentityId, CancellationToken cancellationToken)
    {
        return requirement.Type switch
        {
            ApprovalRequirementType.Organizational => requirement.RequiredApproverIdentityId == approverIdentityId,
            ApprovalRequirementType.Destination when requirement.ApprovalGroupId.HasValue => await db.ApprovalGroupMembers
                .AnyAsync(item => item.ApprovalGroupId == requirement.ApprovalGroupId.Value && item.IdentityId == approverIdentityId && item.ResponsibleLocationId == requirement.LocationId, cancellationToken),
            _ => false
        };
    }
}
