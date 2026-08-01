using AccessControl.Unipass.Contracts;
using AccessControl.Unipass.Entities;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class AccessControlSystemService(
    AccessControlDbContext db,
    LocationsDbContext locationsDb,
    UnipassApiFactory unipassApiFactory)
{
    public async Task<Result<AccessControlSystem, AccessControlErrors>> CreateUnipassSystemAsync(
        string name,
        UnipassSystemConfig config,
        CancellationToken cancellationToken = default)
    {
        bool exists = await db.AccessControlSystems.AnyAsync(system => system.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<AccessControlSystem, AccessControlErrors>(AccessControlErrors.SystemNameAlreadyExists);

        Result<AccessControlSystem, AccessControlErrors> create = AccessControlSystem.CreateUnipass(name, config);
        if (create.IsFailure(out AccessControlErrors error))
            return Result.Failure<AccessControlSystem, AccessControlErrors>(error);

        create.IsSuccess(out AccessControlSystem system);
        db.AccessControlSystems.Add(system);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessControlSystem, AccessControlErrors>(system);
    }

    public async Task<Result<AccessControlSystem, AccessControlErrors>> UpdateUnipassSystemAsync(
        Guid systemId,
        string name,
        string endpoint,
        bool sslValidation,
        string username,
        string? password,
        AccessControlSystemStatus status,
        CancellationToken cancellationToken = default)
    {
        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == systemId, cancellationToken);
        if (system is null)
            return Result.Failure<AccessControlSystem, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        bool exists = await db.AccessControlSystems.AnyAsync(item => item.Id != systemId && item.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<AccessControlSystem, AccessControlErrors>(AccessControlErrors.SystemNameAlreadyExists);

        if (system.ProviderKind != AccessControlProviderKind.Unipass || system.UnipassConfig is null)
            return Result.Failure<AccessControlSystem, AccessControlErrors>(AccessControlErrors.SystemProviderNotSupported);

        Result<UnipassSystemConfig, AccessControlErrors> config = UnipassSystemConfig.Create(
            endpoint,
            sslValidation,
            username,
            string.IsNullOrWhiteSpace(password) ? system.UnipassConfig.Password : password);

        if (config.IsFailure(out AccessControlErrors configError))
            return Result.Failure<AccessControlSystem, AccessControlErrors>(configError);

        config.IsSuccess(out UnipassSystemConfig value);
        Result<AccessControlErrors> updateResult = system.UpdateUnipassConfig(value);
        if (updateResult.IsFailure(out AccessControlErrors updateError))
            return Result.Failure<AccessControlSystem, AccessControlErrors>(updateError);

        system.Rename(name);
        system.SetStatus(status);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessControlSystem, AccessControlErrors>(system);
    }

    public async Task<Result<SystemMetadata, AccessControlErrors>> FetchMetadataAsync(
        Guid systemId,
        CancellationToken cancellationToken = default)
    {
        AccessControlSystem? system = await db.AccessControlSystems.AsNoTracking().SingleOrDefaultAsync(item => item.Id == systemId, cancellationToken);
        if (system is null)
            return Result.Failure<SystemMetadata, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        if (system.ProviderKind != AccessControlProviderKind.Unipass || system.UnipassConfig is null)
            return Result.Failure<SystemMetadata, AccessControlErrors>(AccessControlErrors.SystemProviderNotSupported);

        using IUnipassApi api = unipassApiFactory.Create(system.UnipassConfig);
        List<UnipassSite> sites = await api.GetSites(ct: cancellationToken);
        List<AccessRuleDto> accessRules = await api.GetAccessRules(ct: cancellationToken);

        return Result.Success<SystemMetadata, AccessControlErrors>(new UnipassMetadata(
            [.. sites.Select(site => new SystemMetadataObject(site.Id.ToString(), site.Name))],
            [.. accessRules.Select(rule => new SystemMetadataObject(rule.Id.ToString(), GetAccessRuleName(rule)))]));
    }

    public async Task<Result<AccessControlSystemLocation, AccessControlErrors>> LinkLocationAsync(
        Guid systemId,
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        bool systemExists = await db.AccessControlSystems.AnyAsync(system => system.Id == systemId, cancellationToken);
        if (!systemExists)
            return Result.Failure<AccessControlSystemLocation, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        bool locationExists = await locationsDb.LocationLookups.AnyAsync(location => location.Id == locationId, cancellationToken);
        if (!locationExists)
            return Result.Failure<AccessControlSystemLocation, AccessControlErrors>(AccessControlErrors.LocationNotFound);

        bool locationLinked = await db.AccessControlSystemLocations.AnyAsync(link => link.LocationId == locationId, cancellationToken);
        if (locationLinked)
            return Result.Failure<AccessControlSystemLocation, AccessControlErrors>(AccessControlErrors.LocationAlreadyLinked);

        AccessControlSystemLocation link = AccessControlSystemLocation.Create(systemId, locationId);
        db.AccessControlSystemLocations.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessControlSystemLocation, AccessControlErrors>(link);
    }

    public async Task<Result<AccessControlErrors>> UnlinkLocationAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        AccessControlSystemLocation? link = await db.AccessControlSystemLocations.SingleOrDefaultAsync(item => item.Id == linkId, cancellationToken);
        if (link is null)
            return Result.Failure(AccessControlErrors.SystemLocationNotFound);

        db.AccessControlSystemLocations.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessControlErrors>();
    }

    private static string GetAccessRuleName(AccessRuleDto rule) =>
        new[] { rule.Name1, rule.Name2, rule.Name3 }.FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? rule.Id.ToString();
}
