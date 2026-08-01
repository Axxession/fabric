using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Contracts;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Endpoints;

public static class PackageEndpoints
{
    public static IEndpointRouteBuilder MapPackageEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder packages = app.MapGroup("/api/access-catalog/packages");

        packages.MapGet("", ListPackages).Produces<Page<PackageResponse>>();
        packages.MapPost("", CreatePackage).Produces<PackageResponse>(StatusCodes.Status201Created);
        packages.MapGet("/{packageId:guid}", GetPackage).Produces<PackageResponse>().Produces(StatusCodes.Status404NotFound);
        packages.MapPut("/{packageId:guid}", UpdatePackage).Produces<PackageResponse>().Produces(StatusCodes.Status404NotFound);
        packages.MapGet("/{packageId:guid}/access-items", ListPackageAccessItems).Produces<Page<PackageAccessItemResponse>>();
        packages.MapPost("/{packageId:guid}/access-items", AddPackageAccessItem).Produces<PackageAccessItemResponse>(StatusCodes.Status201Created);
        packages.MapDelete("/{packageId:guid}/access-items/{accessItemId:guid}", DeletePackageAccessItem).Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> ListPackages([AsParameters] ListPackagesRequest request, [FromQuery] Guid[]? ids, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<Package> query = db.Packages.AsNoTracking();
        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));
        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(item => item.Name.ToLower().Contains(request.Name.ToLower()));

        IPaged<Package> result = await query.OrderBy(item => item.Name).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> CreatePackage([FromBody] CreatePackageRequest request, PackageService service, CancellationToken cancellationToken = default)
    {
        Result<Package, AccessCatalogErrors> result = await service.CreateAsync(request.Name, request.Description, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/access-catalog/packages/{item.Id}", item.ToResponse()), error => MapError(error).ToResult());
    }

    private static async Task<IResult> GetPackage(Guid packageId, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        Package? package = await db.Packages.AsNoTracking().SingleOrDefaultAsync(item => item.Id == packageId, cancellationToken);
        return package is null ? Results.NotFound() : Results.Ok(package.ToResponse());
    }

    private static async Task<IResult> UpdatePackage(Guid packageId, [FromBody] UpdatePackageRequest request, PackageService service, CancellationToken cancellationToken = default)
    {
        Result<Package, AccessCatalogErrors> result = await service.UpdateAsync(packageId, request.Name, request.Description, request.Status, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListPackageAccessItems(Guid packageId, [AsParameters] BaseListRequest request, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<PackageAccessItem> result = await db.PackageAccessItems.AsNoTracking()
            .Where(item => item.PackageId == packageId)
            .OrderBy(item => item.AccessItemId)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> AddPackageAccessItem(Guid packageId, [FromBody] AddPackageAccessItemRequest request, PackageService service, CancellationToken cancellationToken = default)
    {
        Result<PackageAccessItem, AccessCatalogErrors> result = await service.AddAccessItemAsync(packageId, request.AccessItemId, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/access-catalog/packages/{packageId}/access-items/{item.AccessItemId}", item.ToResponse()), error => MapError(error).ToResult());
    }

    private static async Task<IResult> DeletePackageAccessItem(Guid packageId, Guid accessItemId, PackageService service, CancellationToken cancellationToken = default)
    {
        Result<AccessCatalogErrors> result = await service.RemoveAccessItemAsync(packageId, accessItemId, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(AccessCatalogErrors error) =>
        error switch
        {
            AccessCatalogErrors.PackageNotFound => Problem(StatusCodes.Status404NotFound, "Package not found."),
            AccessCatalogErrors.AccessItemNotFound => Problem(StatusCodes.Status404NotFound, "Access item not found."),
            AccessCatalogErrors.PackageNameAlreadyExists => Problem(StatusCodes.Status409Conflict, "Package name already exists."),
            AccessCatalogErrors.AccessItemAlreadyLinked => Problem(StatusCodes.Status409Conflict, "Access item already linked to package."),
            AccessCatalogErrors.AccessItemNotLinked => Problem(StatusCodes.Status404NotFound, "Access item not linked to package."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected access catalog error.")
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });

    private static IResult ToResult(this (int statusCode, ProblemDetails? problemDetails) error) =>
        error.problemDetails is null ? Results.StatusCode(error.statusCode) : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
