using Fabric.Server.Core;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class AccessItemService(AccessControlDbContext db)
{
    public async Task<Result<AccessItem, AccessControlErrors>> CreateAsync(
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        bool exists = await db.AccessItems.AnyAsync(item => item.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<AccessItem, AccessControlErrors>(AccessControlErrors.AccessItemNameAlreadyExists);

        AccessItem item = AccessItem.Create(name, description);
        db.AccessItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessItem, AccessControlErrors>(item);
    }

    public async Task<Result<AccessItem, AccessControlErrors>> UpdateAsync(
        Guid itemId,
        string name,
        string? description,
        AccessItemStatus status,
        CancellationToken cancellationToken = default)
    {
        AccessItem? item = await db.AccessItems.SingleOrDefaultAsync(existing => existing.Id == itemId, cancellationToken);
        if (item is null)
            return Result.Failure<AccessItem, AccessControlErrors>(AccessControlErrors.AccessItemNotFound);

        bool exists = await db.AccessItems.AnyAsync(existing => existing.Id != itemId && existing.Name == name, cancellationToken);
        if (exists)
            return Result.Failure<AccessItem, AccessControlErrors>(AccessControlErrors.AccessItemNameAlreadyExists);

        item.Update(name, description);
        item.SetStatus(status);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<AccessItem, AccessControlErrors>(item);
    }
}
