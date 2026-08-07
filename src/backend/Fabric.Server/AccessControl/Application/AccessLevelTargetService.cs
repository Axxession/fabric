using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class AccessLevelTargetService(
    AccessControlDbContext db,
    AccessControlSystemService systemService,
    LocationsDbContext locationsDb,
    SagasDbContext sagasDb,
    AccessGrantProvisioningSagaService sagaService)
{
    public async Task<Result<UnipassAccessLevelTarget, AccessControlErrors>> CreateUnipassTargetAsync(
        Guid accessItemId,
        Guid accessControlSystemId,
        Guid? locationId,
        string name,
        int siteId,
        int accessRuleId,
        ProvisioningTiming provisioningTiming,
        CancellationToken cancellationToken = default)
    {
        AccessItem? accessItem = await db.AccessItems.SingleOrDefaultAsync(item => item.Id == accessItemId, cancellationToken);
        if (accessItem is null)
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(AccessControlErrors.AccessItemNotFound);

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == accessControlSystemId, cancellationToken);
        if (system is null)
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        if (system.ProviderKind != AccessControlProviderKind.Unipass)
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(AccessControlErrors.SystemProviderNotSupported);

        if (locationId.HasValue && !await locationsDb.LocationLookups.AnyAsync(item => item.Id == locationId.Value, cancellationToken))
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(AccessControlErrors.LocationNotFound);

        Result<UnipassMetadata, AccessControlErrors> metadataResult = await GetUnipassMetadataAsync(accessControlSystemId, cancellationToken);
        if (metadataResult.IsFailure(out AccessControlErrors metadataError))
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(metadataError);

        metadataResult.IsSuccess(out UnipassMetadata metadata);
        Result<(string SiteName, string AccessRuleName), AccessControlErrors> nativeValues = ValidateMetadata(metadata, siteId, accessRuleId);
        if (nativeValues.IsFailure(out AccessControlErrors nativeError))
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(nativeError);

        bool exists = await db.AccessLevelTargets
            .OfType<UnipassAccessLevelTarget>()
            .AnyAsync(target =>
                target.AccessItemId == accessItemId &&
                target.AccessControlSystemId == accessControlSystemId &&
                target.LocationId == locationId &&
                target.SiteId == siteId &&
                target.AccessRuleId == accessRuleId,
                cancellationToken);

        if (exists)
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(AccessControlErrors.AccessLevelTargetAlreadyExists);

        nativeValues.IsSuccess(out (string SiteName, string AccessRuleName) resolved);
        UnipassAccessLevelTarget target = UnipassAccessLevelTarget.Create(
            accessItemId,
            accessControlSystemId,
            locationId,
            name,
            accessRuleId,
            siteId,
            resolved.AccessRuleName,
            resolved.SiteName,
            provisioningTiming);

        db.AccessLevelTargets.Add(target);
        await db.SaveChangesAsync(cancellationToken);
        await RequeueSkippedAccessGrantsAsync(accessItemId, cancellationToken);
        return Result.Success<UnipassAccessLevelTarget, AccessControlErrors>(target);
    }

    public async Task<Result<UnipassAccessLevelTarget, AccessControlErrors>> UpdateUnipassTargetAsync(
        Guid targetId,
        Guid? locationId,
        string name,
        int siteId,
        int accessRuleId,
        bool isEnabled,
        ProvisioningTiming provisioningTiming,
        CancellationToken cancellationToken = default)
    {
        UnipassAccessLevelTarget? target = await db.AccessLevelTargets
            .OfType<UnipassAccessLevelTarget>()
            .SingleOrDefaultAsync(item => item.Id == targetId, cancellationToken);

        if (target is null)
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(AccessControlErrors.AccessLevelTargetNotFound);

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == target.AccessControlSystemId, cancellationToken);
        if (system is null)
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        if (locationId.HasValue && !await locationsDb.LocationLookups.AnyAsync(item => item.Id == locationId.Value, cancellationToken))
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(AccessControlErrors.LocationNotFound);

        Result<UnipassMetadata, AccessControlErrors> metadataResult = await GetUnipassMetadataAsync(target.AccessControlSystemId, cancellationToken);
        if (metadataResult.IsFailure(out AccessControlErrors metadataError))
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(metadataError);

        metadataResult.IsSuccess(out UnipassMetadata metadata);
        Result<(string SiteName, string AccessRuleName), AccessControlErrors> nativeValues = ValidateMetadata(metadata, siteId, accessRuleId);
        if (nativeValues.IsFailure(out AccessControlErrors nativeError))
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(nativeError);

        bool exists = await db.AccessLevelTargets
            .OfType<UnipassAccessLevelTarget>()
            .AnyAsync(item =>
                item.Id != targetId &&
                item.AccessItemId == target.AccessItemId &&
                item.AccessControlSystemId == target.AccessControlSystemId &&
                item.LocationId == locationId &&
                item.SiteId == siteId &&
                item.AccessRuleId == accessRuleId,
                cancellationToken);

        if (exists)
            return Result.Failure<UnipassAccessLevelTarget, AccessControlErrors>(AccessControlErrors.AccessLevelTargetAlreadyExists);

        nativeValues.IsSuccess(out (string SiteName, string AccessRuleName) resolved);
        _ = target.Update(locationId, name, accessRuleId, siteId, resolved.AccessRuleName, resolved.SiteName, provisioningTiming);
        target.SetEnabled(isEnabled);

        await db.SaveChangesAsync(cancellationToken);
        if (isEnabled)
            await RequeueSkippedAccessGrantsAsync(target.AccessItemId, cancellationToken);

        return Result.Success<UnipassAccessLevelTarget, AccessControlErrors>(target);
    }

    private async Task RequeueSkippedAccessGrantsAsync(Guid accessItemId, CancellationToken cancellationToken)
    {
        Guid[] accessGrantIds = await sagasDb.AccessGrantMaterializationOutcomes
            .AsNoTracking()
            .Where(item => item.AccessItemId == accessItemId)
            .Where(item => item.Status == AccessGrantMaterializationOutcomeStatus.SkippedNoTarget)
            .Select(item => item.AccessGrantId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        foreach (Guid accessGrantId in accessGrantIds)
            await sagaService.EnqueueAccessGrantCreatedAsync(accessGrantId, cancellationToken);
    }

    private async Task<Result<UnipassMetadata, AccessControlErrors>> GetUnipassMetadataAsync(Guid systemId, CancellationToken cancellationToken)
    {
        Result<SystemMetadata, AccessControlErrors> result = await systemService.FetchMetadataAsync(systemId, cancellationToken);
        if (result.IsFailure(out AccessControlErrors error))
            return Result.Failure<UnipassMetadata, AccessControlErrors>(error);

        result.IsSuccess(out SystemMetadata metadata);
        return metadata is UnipassMetadata unipass
            ? Result.Success<UnipassMetadata, AccessControlErrors>(unipass)
            : Result.Failure<UnipassMetadata, AccessControlErrors>(AccessControlErrors.SystemProviderNotSupported);
    }

    private static Result<(string SiteName, string AccessRuleName), AccessControlErrors> ValidateMetadata(
        UnipassMetadata metadata,
        int siteId,
        int accessRuleId)
    {
        SystemMetadataObject? site = metadata.Sites.SingleOrDefault(item => int.TryParse(item.Id, out int id) && id == siteId);
        if (site is null)
            return Result.Failure<(string SiteName, string AccessRuleName), AccessControlErrors>(AccessControlErrors.SiteNotFoundInMetadata);

        SystemMetadataObject? accessRule = metadata.AccessRules.SingleOrDefault(item => int.TryParse(item.Id, out int id) && id == accessRuleId);
        if (accessRule is null)
            return Result.Failure<(string SiteName, string AccessRuleName), AccessControlErrors>(AccessControlErrors.AccessRuleNotFoundInMetadata);

        return Result.Success<(string SiteName, string AccessRuleName), AccessControlErrors>((site.Name, accessRule.Name));
    }
}
