using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Application;

public sealed class ApprovalConfigurationService(
    AccessCatalogDbContext db,
    AccessControlDbContext accessControlDb,
    IdentitiesDbContext identitiesDb,
    LocationsDbContext locationsDb)
{
    public async Task<Result<ApprovalGroup, AccessCatalogErrors>> CreateApprovalGroupAsync(string name, CancellationToken cancellationToken = default)
    {
        bool exists = await db.ApprovalGroups.AnyAsync(item => item.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<ApprovalGroup, AccessCatalogErrors>(AccessCatalogErrors.ApprovalGroupNameAlreadyExists);

        ApprovalGroup group = ApprovalGroup.Create(name);
        db.ApprovalGroups.Add(group);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ApprovalGroup, AccessCatalogErrors>(group);
    }

    public async Task<Result<ApprovalGroup, AccessCatalogErrors>> UpdateApprovalGroupAsync(Guid approvalGroupId, string name, ApprovalGroupStatus status, CancellationToken cancellationToken = default)
    {
        ApprovalGroup? group = await db.ApprovalGroups.SingleOrDefaultAsync(item => item.Id == approvalGroupId, cancellationToken);
        if (group is null)
            return Result.Failure<ApprovalGroup, AccessCatalogErrors>(AccessCatalogErrors.ApprovalGroupNotFound);

        bool exists = await db.ApprovalGroups.AnyAsync(item => item.Id != approvalGroupId && item.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<ApprovalGroup, AccessCatalogErrors>(AccessCatalogErrors.ApprovalGroupNameAlreadyExists);

        group.Update(name, status);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ApprovalGroup, AccessCatalogErrors>(group);
    }

    public async Task<Result<ApprovalGroupMember, AccessCatalogErrors>> AddApprovalGroupMemberAsync(Guid approvalGroupId, Guid identityId, Guid responsibleLocationId, CancellationToken cancellationToken = default)
    {
        if (!await db.ApprovalGroups.AnyAsync(item => item.Id == approvalGroupId, cancellationToken))
            return Result.Failure<ApprovalGroupMember, AccessCatalogErrors>(AccessCatalogErrors.ApprovalGroupNotFound);

        if (!await identitiesDb.Identities.AnyAsync(item => item.Id == identityId, cancellationToken))
            return Result.Failure<ApprovalGroupMember, AccessCatalogErrors>(AccessCatalogErrors.IdentityNotFound);

        LocationLookup? location = await locationsDb.LocationLookups.SingleOrDefaultAsync(item => item.Id == responsibleLocationId, cancellationToken);
        if (location is null)
            return Result.Failure<ApprovalGroupMember, AccessCatalogErrors>(AccessCatalogErrors.LocationRequired);

        if (location.Type != LocationType.Site)
            return Result.Failure<ApprovalGroupMember, AccessCatalogErrors>(AccessCatalogErrors.ApprovalGroupMemberLocationMustBeSite);

        bool exists = await db.ApprovalGroupMembers.AnyAsync(
            item => item.ApprovalGroupId == approvalGroupId && item.IdentityId == identityId && item.ResponsibleLocationId == responsibleLocationId,
            cancellationToken);
        if (exists)
            return Result.Failure<ApprovalGroupMember, AccessCatalogErrors>(AccessCatalogErrors.ApprovalGroupMemberAlreadyExists);

        ApprovalGroupMember member = ApprovalGroupMember.Create(approvalGroupId, identityId, responsibleLocationId);
        db.ApprovalGroupMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ApprovalGroupMember, AccessCatalogErrors>(member);
    }

    public async Task<Result<AccessCatalogErrors>> RemoveApprovalGroupMemberAsync(Guid approvalGroupId, Guid memberId, CancellationToken cancellationToken = default)
    {
        ApprovalGroupMember? member = await db.ApprovalGroupMembers.SingleOrDefaultAsync(item => item.Id == memberId && item.ApprovalGroupId == approvalGroupId, cancellationToken);
        if (member is null)
            return Result.Failure(AccessCatalogErrors.ApprovalGroupMemberNotFound);

        db.ApprovalGroupMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessCatalogErrors>();
    }

    public async Task<Result<ApprovalDefinition, AccessCatalogErrors>> CreateApprovalDefinitionAsync(Guid accessItemId, Guid? destinationApprovalGroupId, OrganizationalApprovalMode organizationalApprovalMode, int organizationalApprovalLevels, CancellationToken cancellationToken = default)
    {
        if (!await accessControlDb.AccessItems.AnyAsync(item => item.Id == accessItemId, cancellationToken))
            return Result.Failure<ApprovalDefinition, AccessCatalogErrors>(AccessCatalogErrors.AccessItemNotFound);

        if (destinationApprovalGroupId.HasValue && !await db.ApprovalGroups.AnyAsync(item => item.Id == destinationApprovalGroupId.Value, cancellationToken))
            return Result.Failure<ApprovalDefinition, AccessCatalogErrors>(AccessCatalogErrors.ApprovalGroupNotFound);

        bool exists = await db.ApprovalDefinitions.AnyAsync(item => item.AccessItemId == accessItemId, cancellationToken);
        if (exists)
            return Result.Failure<ApprovalDefinition, AccessCatalogErrors>(AccessCatalogErrors.ApprovalDefinitionAlreadyExists);

        Result<ApprovalDefinition, AccessCatalogErrors> create = ApprovalDefinition.Create(accessItemId, destinationApprovalGroupId, organizationalApprovalMode, organizationalApprovalLevels);
        if (create.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<ApprovalDefinition, AccessCatalogErrors>(error);

        create.IsSuccess(out ApprovalDefinition definition);
        db.ApprovalDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ApprovalDefinition, AccessCatalogErrors>(definition);
    }

    public async Task<Result<ApprovalDefinition, AccessCatalogErrors>> UpdateApprovalDefinitionAsync(Guid approvalDefinitionId, Guid? destinationApprovalGroupId, OrganizationalApprovalMode organizationalApprovalMode, int organizationalApprovalLevels, CancellationToken cancellationToken = default)
    {
        ApprovalDefinition? definition = await db.ApprovalDefinitions.SingleOrDefaultAsync(item => item.Id == approvalDefinitionId, cancellationToken);
        if (definition is null)
            return Result.Failure<ApprovalDefinition, AccessCatalogErrors>(AccessCatalogErrors.ApprovalDefinitionNotFound);

        if (destinationApprovalGroupId.HasValue && !await db.ApprovalGroups.AnyAsync(item => item.Id == destinationApprovalGroupId.Value, cancellationToken))
            return Result.Failure<ApprovalDefinition, AccessCatalogErrors>(AccessCatalogErrors.ApprovalGroupNotFound);

        Result<AccessCatalogErrors> update = definition.Update(destinationApprovalGroupId, organizationalApprovalMode, organizationalApprovalLevels);
        if (update.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<ApprovalDefinition, AccessCatalogErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ApprovalDefinition, AccessCatalogErrors>(definition);
    }
}
