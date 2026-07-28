using Fabric.Server.Core;
using Fabric.Server.Visitors.Application;
using Fabric.Server.Visitors.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Visitors.Endpoints;

public static class HostEndpoints
{
    public static IEndpointRouteBuilder MapHostEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder hosts = app.MapGroup("/api/visitors/hosts");

        hosts.MapGet("", ListHosts)
            .Produces<Page<HostResponse>>();
        hosts.MapGet("/{employeeId:guid}", GetHost)
            .Produces<HostResponse>()
            .Produces(StatusCodes.Status404NotFound);
        hosts.MapPost("/{employeeId:guid}", AddHost)
            .Produces<HostResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        hosts.MapDelete("/{employeeId:guid}", RemoveHost)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        RouteGroupBuilder settings = app.MapGroup("/api/visitors/host-settings");

        settings.MapGet("", GetSettings)
            .Produces<HostSettingsResponse>();
        settings.MapPut("", UpdateSettings)
            .Produces<HostSettingsResponse>();

        return app;
    }

    private static async Task<IResult> ListHosts([AsParameters] ListHostsRequest request, HostService hostService, CancellationToken cancellationToken = default) =>
        Results.Ok(await hostService.ListHostsAsync(request, cancellationToken));

    private static async Task<IResult> GetHost(Guid employeeId, HostService hostService, CancellationToken cancellationToken = default)
    {
        HostResponse? host = await hostService.GetHostByEmployeeIdAsync(employeeId, cancellationToken);
        return host is null ? Results.NotFound() : Results.Ok(host);
    }

    private static async Task<IResult> AddHost(Guid employeeId, HostService hostService, CancellationToken cancellationToken = default)
    {
        Result<HostResponse, HostErrors> result = await hostService.AddHostAsync(employeeId, cancellationToken);
        return result.Match<IResult>(
            host => Results.Created($"/api/visitors/hosts/{host.EmployeeId}", host),
            error => ToResult(MapError(error)));
    }

    private static async Task<IResult> RemoveHost(Guid employeeId, HostService hostService, CancellationToken cancellationToken = default)
    {
        Result<HostErrors> result = await hostService.RemoveHostAsync(employeeId, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static IResult GetSettings(HostService hostService) => Results.Ok(hostService.GetSettings());

    private static async Task<IResult> UpdateSettings([FromBody] UpdateHostSettingsRequest request, HostService hostService, CancellationToken cancellationToken = default) =>
        Results.Ok(await hostService.UpdateSettingsAsync(request.AssignmentMode, cancellationToken));

    private static (int statusCode, ProblemDetails? problemDetails) MapError(HostErrors error) =>
        error switch
        {
            HostErrors.EmployeeNotFound => Problem(StatusCodes.Status404NotFound, "Employee not found."),
            HostErrors.EmployeeArchived => Problem(StatusCodes.Status409Conflict, "Archived employees cannot be hosts."),
            HostErrors.HostNotFound => Problem(StatusCodes.Status404NotFound, "Host not found."),
            HostErrors.AssignmentModeDoesNotSupportAllowList => Problem(StatusCodes.Status409Conflict, "Host allow list is only available in allow-list mode."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected host error.")
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });

    private static IResult ToResult((int statusCode, ProblemDetails? problemDetails) error) =>
        error.problemDetails is null
            ? Results.StatusCode(error.statusCode)
            : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
