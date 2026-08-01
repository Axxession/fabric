using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Application;

public sealed class PackageService(
    AccessCatalogDbContext db,
    AccessControlDbContext accessControlDb)
{
    public async Task<Result<Package, AccessCatalogErrors>> CreateAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        bool exists = await db.Packages.AnyAsync(item => item.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<Package, AccessCatalogErrors>(AccessCatalogErrors.PackageNameAlreadyExists);

        Package package = Package.Create(name, description);
        db.Packages.Add(package);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Package, AccessCatalogErrors>(package);
    }

    public async Task<Result<Package, AccessCatalogErrors>> UpdateAsync(Guid packageId, string name, string? description, PackageStatus status, CancellationToken cancellationToken = default)
    {
        Package? package = await db.Packages.SingleOrDefaultAsync(item => item.Id == packageId, cancellationToken);
        if (package is null)
            return Result.Failure<Package, AccessCatalogErrors>(AccessCatalogErrors.PackageNotFound);

        bool exists = await db.Packages.AnyAsync(item => item.Id != packageId && item.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<Package, AccessCatalogErrors>(AccessCatalogErrors.PackageNameAlreadyExists);

        package.Update(name, description, status);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Package, AccessCatalogErrors>(package);
    }

    public async Task<Result<PackageAccessItem, AccessCatalogErrors>> AddAccessItemAsync(Guid packageId, Guid accessItemId, CancellationToken cancellationToken = default)
    {
        if (!await db.Packages.AnyAsync(item => item.Id == packageId, cancellationToken))
            return Result.Failure<PackageAccessItem, AccessCatalogErrors>(AccessCatalogErrors.PackageNotFound);

        if (!await accessControlDb.AccessItems.AnyAsync(item => item.Id == accessItemId, cancellationToken))
            return Result.Failure<PackageAccessItem, AccessCatalogErrors>(AccessCatalogErrors.AccessItemNotFound);

        PackageAccessItem? existing = await db.PackageAccessItems.SingleOrDefaultAsync(item => item.PackageId == packageId && item.AccessItemId == accessItemId, cancellationToken);
        if (existing is not null)
            return Result.Failure<PackageAccessItem, AccessCatalogErrors>(AccessCatalogErrors.AccessItemAlreadyLinked);

        PackageAccessItem link = PackageAccessItem.Create(packageId, accessItemId);
        db.PackageAccessItems.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<PackageAccessItem, AccessCatalogErrors>(link);
    }

    public async Task<Result<AccessCatalogErrors>> RemoveAccessItemAsync(Guid packageId, Guid accessItemId, CancellationToken cancellationToken = default)
    {
        PackageAccessItem? link = await db.PackageAccessItems.SingleOrDefaultAsync(item => item.PackageId == packageId && item.AccessItemId == accessItemId, cancellationToken);
        if (link is null)
            return Result.Failure(AccessCatalogErrors.AccessItemNotLinked);

        db.PackageAccessItems.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessCatalogErrors>();
    }
}
