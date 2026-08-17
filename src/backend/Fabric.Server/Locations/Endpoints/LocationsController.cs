using Fabric.Server.Core;
using Fabric.Server.Locations.Application;
using Fabric.Server.Locations.Contracts;
using Fabric.Server.Locations.Domain;
using Fabric.Server.Locations.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Locations.Endpoints;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/locations/locations", ListLocations)
            .WithDescription("Retrieve locations by ids")
            .WithSummary("List locations")
            .Produces<LocationResponse[]>();

        app.MapGet("/api/locations/locations/{id:guid}", GetLocationById)
            .WithDescription("Retrieve a site, building, or room by id")
            .WithSummary("Retrieve a location by id")
            .Produces<LocationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        RouteGroupBuilder sites = app.MapGroup("/api/locations/sites");

        sites.MapGet("", ListSites)
            .WithDescription("List all sites")
            .WithSummary("List sites")
            .Produces<Page<SiteResponse>>();
        sites.MapGet("/{siteId:guid}/buildings", ListBuildings)
            .WithDescription("List buildings for a site")
            .WithSummary("List site buildings")
            .Produces<BuildingResponse[]>()
            .Produces(StatusCodes.Status404NotFound);
        sites.MapGet("/{siteId:guid}/buildings/{buildingId:guid}/rooms", ListRooms)
            .WithDescription("List rooms for a building")
            .WithSummary("List building rooms")
            .Produces<RoomResponse[]>()
            .Produces(StatusCodes.Status404NotFound);
        sites.MapPost("", CreateSite)
            .WithDescription("Create a site")
            .WithSummary("Create site")
            .Produces<LocationResponse>(StatusCodes.Status201Created);
        sites.MapPost("/{siteId:guid}/buildings", AddBuilding)
            .WithDescription("Add a building to a site")
            .WithSummary("Add building")
            .Produces<LocationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        sites.MapPut("/{siteId:guid}", UpdateSite)
            .WithDescription("Update a site's name")
            .WithSummary("Update site")
            .Produces<LocationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        sites.MapPut("/{siteId:guid}/buildings/{buildingId:guid}", UpdateBuilding)
            .WithDescription("Update a building's name")
            .WithSummary("Update building")
            .Produces<LocationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        sites.MapDelete("/{siteId:guid}/buildings/{buildingId:guid}", RemoveBuilding)
            .WithDescription("Remove a building from a site")
            .WithSummary("Remove building")
            .Produces<LocationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        sites.MapPost("/{siteId:guid}/buildings/{buildingId:guid}/rooms", AddRoom)
            .WithDescription("Add a room to a building")
            .WithSummary("Add room")
            .Produces<LocationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        sites.MapPut("/{siteId:guid}/buildings/{buildingId:guid}/rooms/{roomId:guid}", UpdateRoom)
            .WithDescription("Update a room")
            .WithSummary("Update room")
            .Produces<LocationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        sites.MapDelete("/{siteId:guid}/buildings/{buildingId:guid}/rooms/{roomId:guid}", RemoveRoom)
            .WithDescription("Remove a room from a building")
            .WithSummary("Remove room")
            .Produces<LocationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListLocations(
        [FromQuery] Guid[]? ids,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        if (ids is not { Length: > 0 })
            return Results.Ok(Array.Empty<LocationResponse>());

        IReadOnlyList<Location> locations = await locationService.GetLocationsByIds(ids, cancellationToken);
        return Results.Ok(locations.Select(location => location.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetLocationById(
        Guid id,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Location? location = await locationService.GetLocationById(id, cancellationToken);
        return location is null ? Results.NotFound() : Results.Ok(location.ToResponse());
    }

    private static async Task<IResult> ListSites(
        [AsParameters] ListSitesRequest request,
        LocationsDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Site> query = db.Sites.AsNoTracking().OrderBy(site => site.Name);
        IPaged<Site> result = await query.GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(site => site.ToResponse()));
    }

    private static async Task<IResult> ListBuildings(
        Guid siteId,
        LocationsDbContext db,
        CancellationToken cancellationToken = default)
    {
        Site? site = await db.Sites
            .AsNoTracking()
            .Include(x => x.Buildings)
            .SingleOrDefaultAsync(x => x.Id == siteId, cancellationToken);

        if (site is null)
            return Results.NotFound();

        return Results.Ok(site.Buildings.OrderBy(x => x.Name).Select(x => x.ToResponse()).ToArray());
    }

    private static async Task<IResult> ListRooms(
        Guid siteId,
        Guid buildingId,
        LocationsDbContext db,
        CancellationToken cancellationToken = default)
    {
        Site? site = await db.Sites
            .AsNoTracking()
            .Include(x => x.Buildings)
            .ThenInclude(x => x.Rooms)
            .SingleOrDefaultAsync(x => x.Id == siteId, cancellationToken);

        Building? building = site?.Buildings.SingleOrDefault(x => x.Id == buildingId);
        if (building is null)
            return Results.NotFound();

        return Results.Ok(building.Rooms.OrderBy(x => x.Name).Select(x => x.ToResponse()).ToArray());
    }

    private static async Task<IResult> CreateSite(
        [FromBody] CreateSiteRequest request,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Result<Location, LocationErrors> result = await locationService.CreateSite(
            request.Id ?? Guid.NewGuid(),
            request.Name,
            request.Address,
            cancellationToken);

        return result.Match(
            location => Results.Created($"/api/locations/locations/{location.Id}", location.ToResponse()),
            error => MapError(error).ToResult());
    }

    private static async Task<IResult> AddBuilding(
        Guid siteId,
        [FromBody] AddBuildingRequest request,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Result<Location, LocationErrors> result = await locationService.AddBuilding(
            siteId,
            request.Name,
            request.Address,
            cancellationToken);

        return result.Map(location => location.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> UpdateSite(
        Guid siteId,
        [FromBody] UpdateSiteRequest request,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Result<Location, LocationErrors> result = await locationService.UpdateSite(siteId, request.Name, cancellationToken);
        return result.Map(location => location.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> UpdateBuilding(
        Guid siteId,
        Guid buildingId,
        [FromBody] UpdateBuildingRequest request,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Result<Location, LocationErrors> result = await locationService.UpdateBuilding(
            siteId,
            buildingId,
            request.Name,
            cancellationToken);

        return result.Map(location => location.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> RemoveBuilding(
        Guid siteId,
        Guid buildingId,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Result<Location, LocationErrors> result = await locationService.RemoveBuilding(siteId, buildingId, cancellationToken);
        return result.Map(location => location.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> AddRoom(
        Guid siteId,
        Guid buildingId,
        [FromBody] AddRoomRequest request,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Result<Location, LocationErrors> result = await locationService.AddRoom(
            siteId,
            buildingId,
            request.Name,
            request.Capacity,
            request.WheelchairAccessible,
            cancellationToken);

        return result.Map(location => location.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> UpdateRoom(
        Guid siteId,
        Guid buildingId,
        Guid roomId,
        [FromBody] UpdateRoomRequest request,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Result<Location, LocationErrors> result = await locationService.UpdateRoom(
            siteId,
            buildingId,
            roomId,
            request.Name,
            request.Capacity,
            request.WheelchairAccessible,
            cancellationToken);

        return result.Map(location => location.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> RemoveRoom(
        Guid siteId,
        Guid buildingId,
        Guid roomId,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Result<Location, LocationErrors> result = await locationService.RemoveRoom(siteId, buildingId, roomId, cancellationToken);
        return result.Map(location => location.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(LocationErrors error)
    {
        return error switch
        {
            LocationErrors.SiteNotFound => Problem(StatusCodes.Status404NotFound, "Site not found."),
            LocationErrors.BuildingNotFound => Problem(StatusCodes.Status404NotFound, "Building not found."),
            LocationErrors.RoomNotFound => Problem(StatusCodes.Status404NotFound, "Room not found."),
            LocationErrors.BuildingAlreadyExists => Problem(StatusCodes.Status409Conflict, "Building already exists."),
            LocationErrors.RoomAlreadyExists => Problem(StatusCodes.Status409Conflict, "Room already exists."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected location error.")
        };
    }

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}

file static class LocationControllerResultExtensions
{
    public static IResult ToResult(this (int statusCode, ProblemDetails? problemDetails) error) =>
        error.problemDetails is null
            ? Results.StatusCode(error.statusCode)
            : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
