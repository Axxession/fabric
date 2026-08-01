using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Contracts;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder catalogs = app.MapGroup("/api/access-catalog/catalogs");

        catalogs.MapGet("", ListCatalogs).Produces<Page<CatalogResponse>>();
        catalogs.MapPost("", CreateCatalog).Produces<CatalogResponse>(StatusCodes.Status201Created);
        catalogs.MapGet("/{catalogId:guid}", GetCatalog).Produces<CatalogResponse>().Produces(StatusCodes.Status404NotFound);
        catalogs.MapPut("/{catalogId:guid}", UpdateCatalog).Produces<CatalogResponse>().Produces(StatusCodes.Status404NotFound);
        catalogs.MapGet("/{catalogId:guid}/packages", ListCatalogPackages).Produces<Page<CatalogPackageResponse>>();
        catalogs.MapPost("/{catalogId:guid}/packages", LinkCatalogPackage).Produces<CatalogPackageResponse>(StatusCodes.Status201Created);
        catalogs.MapDelete("/{catalogId:guid}/packages/{packageId:guid}", DeleteCatalogPackage).Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> ListCatalogs([AsParameters] ListCatalogsRequest request, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<Catalog> query = db.Catalogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(item => item.Name.ToLower().Contains(request.Name.ToLower()));

        IPaged<Catalog> result = await query.OrderBy(item => item.Name).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateCatalog([FromBody] CreateCatalogRequest request, CatalogService service, CancellationToken cancellationToken = default)
    {
        Result<Catalog, AccessCatalogErrors> result = await service.CreateAsync(request.Name, request.Description, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/access-catalog/catalogs/{item.Id}", item.ToResponse()), error => MapError(error).ToResult());
    }

    private static async Task<IResult> GetCatalog(Guid catalogId, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        Catalog? catalog = await db.Catalogs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == catalogId, cancellationToken);
        return catalog is null ? Results.NotFound() : Results.Ok(catalog.ToResponse());
    }

    private static async Task<IResult> UpdateCatalog(Guid catalogId, [FromBody] UpdateCatalogRequest request, CatalogService service, CancellationToken cancellationToken = default)
    {
        Result<Catalog, AccessCatalogErrors> result = await service.UpdateAsync(catalogId, request.Name, request.Description, request.Status, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListCatalogPackages(Guid catalogId, [AsParameters] BaseListRequest request, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<CatalogPackage> result = await db.CatalogPackages.AsNoTracking()
            .Where(item => item.CatalogId == catalogId)
            .OrderBy(item => item.PackageId)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> LinkCatalogPackage(Guid catalogId, [FromBody] LinkCatalogPackageRequest request, CatalogService service, CancellationToken cancellationToken = default)
    {
        Result<CatalogPackage, AccessCatalogErrors> result = await service.LinkPackageAsync(catalogId, request.PackageId, request.IsRequestable, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/access-catalog/catalogs/{catalogId}/packages/{item.PackageId}", item.ToResponse()), error => MapError(error).ToResult());
    }

    private static async Task<IResult> DeleteCatalogPackage(Guid catalogId, Guid packageId, CatalogService service, CancellationToken cancellationToken = default)
    {
        Result<AccessCatalogErrors> result = await service.UnlinkPackageAsync(catalogId, packageId, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(AccessCatalogErrors error) =>
        error switch
        {
            AccessCatalogErrors.CatalogNotFound => Problem(StatusCodes.Status404NotFound, "Catalog not found."),
            AccessCatalogErrors.PackageNotFound => Problem(StatusCodes.Status404NotFound, "Package not found."),
            AccessCatalogErrors.CatalogNameAlreadyExists => Problem(StatusCodes.Status409Conflict, "Catalog name already exists."),
            AccessCatalogErrors.CatalogPackageAlreadyLinked => Problem(StatusCodes.Status409Conflict, "Package already linked to catalog."),
            AccessCatalogErrors.CatalogPackageNotLinked => Problem(StatusCodes.Status404NotFound, "Package link not found in catalog."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected access catalog error.")
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });

    private static IResult ToResult(this (int statusCode, ProblemDetails? problemDetails) error) =>
        error.problemDetails is null ? Results.StatusCode(error.statusCode) : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
