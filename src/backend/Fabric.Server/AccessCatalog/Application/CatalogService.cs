using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Application;

public sealed class CatalogService(AccessCatalogDbContext db)
{
    public async Task<Result<Catalog, AccessCatalogErrors>> CreateAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        bool exists = await db.Catalogs.AnyAsync(item => item.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<Catalog, AccessCatalogErrors>(AccessCatalogErrors.CatalogNameAlreadyExists);

        Catalog catalog = Catalog.Create(name, description);
        db.Catalogs.Add(catalog);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Catalog, AccessCatalogErrors>(catalog);
    }

    public async Task<Result<Catalog, AccessCatalogErrors>> UpdateAsync(Guid catalogId, string name, string? description, CatalogStatus status, CancellationToken cancellationToken = default)
    {
        Catalog? catalog = await db.Catalogs.SingleOrDefaultAsync(item => item.Id == catalogId, cancellationToken);
        if (catalog is null)
            return Result.Failure<Catalog, AccessCatalogErrors>(AccessCatalogErrors.CatalogNotFound);

        bool exists = await db.Catalogs.AnyAsync(item => item.Id != catalogId && item.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<Catalog, AccessCatalogErrors>(AccessCatalogErrors.CatalogNameAlreadyExists);

        catalog.Update(name, description, status);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Catalog, AccessCatalogErrors>(catalog);
    }

    public async Task<Result<CatalogPackage, AccessCatalogErrors>> LinkPackageAsync(Guid catalogId, Guid packageId, bool isRequestable, CancellationToken cancellationToken = default)
    {
        if (!await db.Catalogs.AnyAsync(item => item.Id == catalogId, cancellationToken))
            return Result.Failure<CatalogPackage, AccessCatalogErrors>(AccessCatalogErrors.CatalogNotFound);

        if (!await db.Packages.AnyAsync(item => item.Id == packageId, cancellationToken))
            return Result.Failure<CatalogPackage, AccessCatalogErrors>(AccessCatalogErrors.PackageNotFound);

        CatalogPackage? existing = await db.CatalogPackages.SingleOrDefaultAsync(item => item.CatalogId == catalogId && item.PackageId == packageId, cancellationToken);
        if (existing is not null)
            return Result.Failure<CatalogPackage, AccessCatalogErrors>(AccessCatalogErrors.CatalogPackageAlreadyLinked);

        CatalogPackage link = CatalogPackage.Create(catalogId, packageId, isRequestable);
        db.CatalogPackages.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CatalogPackage, AccessCatalogErrors>(link);
    }

    public async Task<Result<AccessCatalogErrors>> UnlinkPackageAsync(Guid catalogId, Guid packageId, CancellationToken cancellationToken = default)
    {
        CatalogPackage? link = await db.CatalogPackages.SingleOrDefaultAsync(item => item.CatalogId == catalogId && item.PackageId == packageId, cancellationToken);
        if (link is null)
            return Result.Failure(AccessCatalogErrors.CatalogPackageNotLinked);

        db.CatalogPackages.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessCatalogErrors>();
    }
}
