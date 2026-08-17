using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Application;

public sealed class AccessGrantService(
    AccessCatalogDbContext db,
    LocationsDbContext locationsDb,
    IdentitiesDbContext identitiesDb,
    GrantRequirementsService grantRequirementsService,
    AccessGrantComplianceService complianceService,
    AccessGrantProvisioningSagaService provisioningSagaService,
    TimeProvider timeProvider)
{
    public async Task<Result<IReadOnlyList<AccessGrant>, AccessCatalogErrors>> CreateAsync(
        Guid packageId,
        Guid identityId,
        Guid locationId,
        AssignmentChannel assignmentChannel,
        AssignmentSourceKind sourceKind,
        Guid sourceId,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        string reasonText,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Packages.AnyAsync(item => item.Id == packageId, cancellationToken))
            return Result.Failure<IReadOnlyList<AccessGrant>, AccessCatalogErrors>(AccessCatalogErrors.PackageNotFound);

        Guid[] accessItemIds = await db.PackageAccessItems
            .Where(item => item.PackageId == packageId)
            .Select(item => item.AccessItemId)
            .ToArrayAsync(cancellationToken);
        if (accessItemIds.Length == 0)
            return Result.Failure<IReadOnlyList<AccessGrant>, AccessCatalogErrors>(AccessCatalogErrors.PackageMustContainAccessItems);

        if (!await locationsDb.LocationLookups.AnyAsync(item => item.Id == locationId, cancellationToken))
            return Result.Failure<IReadOnlyList<AccessGrant>, AccessCatalogErrors>(AccessCatalogErrors.LocationRequired);

        List<AccessGrant> grants = [];
        GrantApprovalStatus approvalStatus = assignmentChannel == AssignmentChannel.CatalogRequest
            ? GrantApprovalStatus.Approved
            : GrantApprovalStatus.NotRequired;

        foreach (Guid accessItemId in accessItemIds)
        {
            Result<AccessGrant, AccessCatalogErrors> create = await CreateInternalAsync(
                packageId,
                accessItemId,
                identityId,
                locationId,
                assignmentChannel,
                sourceKind,
                sourceId,
                null,
                null,
                durationKind,
                validFrom,
                validUntil,
                approvalStatus,
                reasonText,
                cancellationToken);

            if (create.IsFailure(out AccessCatalogErrors error))
                return Result.Failure<IReadOnlyList<AccessGrant>, AccessCatalogErrors>(error);

            create.IsSuccess(out AccessGrant grant);
            grants.Add(grant);
        }

        return Result.Success<IReadOnlyList<AccessGrant>, AccessCatalogErrors>(grants);
    }

    public async Task<Result<AccessGrant, AccessCatalogErrors>> CreateForRequestScopeAsync(
        Guid packageId,
        Guid accessItemId,
        Guid identityId,
        Guid locationId,
        Guid requestId,
        Guid approvalFlowId,
        Guid requestScopeId,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        string reasonText,
        CancellationToken cancellationToken = default)
    {
        return await CreateInternalAsync(
            packageId,
            accessItemId,
            identityId,
            locationId,
            AssignmentChannel.CatalogRequest,
            AssignmentSourceKind.CatalogRequest,
            requestId,
            approvalFlowId,
            requestScopeId,
            durationKind,
            validFrom,
            validUntil,
            GrantApprovalStatus.Approved,
            reasonText,
            cancellationToken);
    }

    public async Task<Result<AccessGrant, AccessCatalogErrors>> RevokeAsync(
        Guid accessGrantId,
        AccessGrantRevokeCause revokeCause,
        string? revokedBy,
        CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.SingleOrDefaultAsync(item => item.Id == accessGrantId, cancellationToken);
        if (grant is null)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.AccessGrantNotFound);

        Result<AccessCatalogErrors> revoke = grant.Revoke(revokedBy, revokeCause);
        if (revoke.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<AccessGrant, AccessCatalogErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await provisioningSagaService.EnqueueAccessGrantRevokedAsync(grant.Id, cancellationToken);
        return Result.Success<AccessGrant, AccessCatalogErrors>(grant);
    }

    public async Task<Result<AccessGrant, AccessCatalogErrors>> ReplaceAsync(
        Guid oldGrantId,
        Guid newGrantId,
        CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.SingleOrDefaultAsync(item => item.Id == oldGrantId, cancellationToken);
        if (grant is null)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.AccessGrantNotFound);

        Result<AccessCatalogErrors> replace = grant.Replace(newGrantId);
        if (replace.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<AccessGrant, AccessCatalogErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessGrant, AccessCatalogErrors>(grant);
    }

    public async Task<Result<AccessGrant, AccessCatalogErrors>> UpdateValidityAsync(
        Guid accessGrantId,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.SingleOrDefaultAsync(item => item.Id == accessGrantId, cancellationToken);
        if (grant is null)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.AccessGrantNotFound);

        Result<AccessCatalogErrors> update = grant.UpdateValidity(validFrom, validUntil);
        if (update.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<AccessGrant, AccessCatalogErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await complianceService.EvaluateGrantAsync(grant.Id, cancellationToken);
        return Result.Success<AccessGrant, AccessCatalogErrors>(grant);
    }

    public async Task<int> RecalculateRequirementsAsync(bool futureOnly, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        AccessGrant[] grants = await db.AccessGrants
            .Where(item => item.Status == AccessGrantStatus.Active)
            .Where(item => !futureOnly || item.ValidFrom > now)
            .ToArrayAsync(cancellationToken);

        foreach (AccessGrant grant in grants)
        {
            RequirementSubjectKind subjectKind = await ResolveSubjectKindAsync(grant.IdentityId, cancellationToken);
            Result<IReadOnlyList<DerivedGrantRequirement>, RequirementsEvaluationErrors> derivation = await grantRequirementsService.DeriveForGrantAsync(grant.IdentityId, subjectKind, grant.LocationId, cancellationToken: cancellationToken);
            if (derivation.IsFailure(out _))
                continue;

            derivation.IsSuccess(out IReadOnlyList<DerivedGrantRequirement> derivedRequirements);
            GrantRequirement[] existingRequirements = await db.GrantRequirements.Where(item => item.AccessGrantId == grant.Id).ToArrayAsync(cancellationToken);
            GrantRequirementResult[] existingResults = await db.GrantRequirementResults.Where(item => item.AccessGrantId == grant.Id).ToArrayAsync(cancellationToken);
            db.GrantRequirements.RemoveRange(existingRequirements);
            db.GrantRequirementResults.RemoveRange(existingResults);

            foreach (DerivedGrantRequirement requirement in derivedRequirements)
            {
                db.GrantRequirements.Add(GrantRequirement.Create(
                    grant.Id,
                    requirement.RequirementDefinitionId,
                    requirement.SourcePolicyKind,
                    requirement.SourcePolicyId,
                    requirement.IsBlocking,
                    now));
            }

            await db.SaveChangesAsync(cancellationToken);
            await complianceService.EvaluateGrantAsync(grant.Id, cancellationToken);
        }

        return grants.Length;
    }

    private async Task<Result<AccessGrant, AccessCatalogErrors>> CreateInternalAsync(
        Guid packageId,
        Guid accessItemId,
        Guid identityId,
        Guid locationId,
        AssignmentChannel assignmentChannel,
        AssignmentSourceKind sourceKind,
        Guid sourceId,
        Guid? approvalFlowId,
        Guid? requestScopeId,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        GrantApprovalStatus approvalStatus,
        string reasonText,
        CancellationToken cancellationToken)
    {
        if (!await locationsDb.LocationLookups.AnyAsync(item => item.Id == locationId, cancellationToken))
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.LocationRequired);

        Result<AccessGrant, AccessCatalogErrors> create = AccessGrant.Create(
            packageId,
            identityId,
            assignmentChannel,
            sourceKind,
            sourceId,
            accessItemId,
            approvalFlowId,
            requestScopeId,
            locationId,
            durationKind,
            validFrom,
            validUntil,
            approvalStatus,
            reasonText);

        if (create.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<AccessGrant, AccessCatalogErrors>(error);

        create.IsSuccess(out AccessGrant grant);
        db.AccessGrants.Add(grant);

        try
        {
            await db.SaveChangesAsync(cancellationToken);

            RequirementSubjectKind subjectKind = await ResolveSubjectKindAsync(identityId, cancellationToken);
            Result<IReadOnlyList<DerivedGrantRequirement>, RequirementsEvaluationErrors> derivation = await grantRequirementsService.DeriveForGrantAsync(identityId, subjectKind, locationId, cancellationToken: cancellationToken);
            if (derivation.IsSuccess(out IReadOnlyList<DerivedGrantRequirement> derivedRequirements))
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                foreach (DerivedGrantRequirement requirement in derivedRequirements)
                {
                    db.GrantRequirements.Add(GrantRequirement.Create(
                        grant.Id,
                        requirement.RequirementDefinitionId,
                        requirement.SourcePolicyKind,
                        requirement.SourcePolicyId,
                        requirement.IsBlocking,
                        now));
                }

                await db.SaveChangesAsync(cancellationToken);
            }

            await complianceService.EvaluateGrantAsync(grant.Id, cancellationToken);
            return Result.Success<AccessGrant, AccessCatalogErrors>(grant);
        }
        catch
        {
            GrantRequirement[] grantRequirements = await db.GrantRequirements.Where(item => item.AccessGrantId == grant.Id).ToArrayAsync(cancellationToken);
            GrantRequirementResult[] grantRequirementResults = await db.GrantRequirementResults.Where(item => item.AccessGrantId == grant.Id).ToArrayAsync(cancellationToken);
            db.GrantRequirementResults.RemoveRange(grantRequirementResults);
            db.GrantRequirements.RemoveRange(grantRequirements);
            db.AccessGrants.Remove(grant);
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<RequirementSubjectKind> ResolveSubjectKindAsync(Guid identityId, CancellationToken cancellationToken)
    {
        if (await identitiesDb.ContractorAffiliations.AnyAsync(item => item.IdentityId == identityId, cancellationToken))
            return RequirementSubjectKind.Contractor;
        if (await identitiesDb.VisitorAffiliations.AnyAsync(item => item.IdentityId == identityId, cancellationToken))
            return RequirementSubjectKind.Visitor;
        return RequirementSubjectKind.Employee;
    }
}
