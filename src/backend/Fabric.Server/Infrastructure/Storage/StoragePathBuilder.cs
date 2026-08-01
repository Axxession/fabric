using Fabric.Server.Infrastructure.Tenancy;

namespace Fabric.Server.Infrastructure.Storage;

public interface IStoragePathBuilder
{
    string BuildTenantScopedPath(string domain, params string[] segments);
    string GetTenantRootPath();
}

public sealed class StoragePathBuilder(ITenantContext tenantContext) : IStoragePathBuilder
{
    public string BuildTenantScopedPath(string domain, params string[] segments)
    {
        string[] allSegments = [tenantContext.TenantId, domain, .. segments];
        return string.Join('/', allSegments.SelectMany(SplitAndNormalize));
    }

    public string GetTenantRootPath() => string.Join('/', SplitAndNormalize(tenantContext.TenantId));

    private static IEnumerable<string> SplitAndNormalize(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            throw new InvalidOperationException("Storage path segment cannot be empty.");

        string[] parts = segment.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            throw new InvalidOperationException("Storage path segment cannot be empty.");

        foreach (string part in parts)
        {
            if (part is "." or "..")
                throw new InvalidOperationException("Storage path segment contains invalid traversal tokens.");

            yield return part;
        }
    }
}
