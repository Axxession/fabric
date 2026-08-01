using Fabric.Server.Actors.Application;
using Fabric.Server.Actors.Contracts;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Actors.Endpoints;

public static class ActorEndpoints
{
    public static IEndpointRouteBuilder MapActorEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder actors = app.MapGroup("/api/actors");

        actors.MapGet("/me", GetCurrentActor)
            .WithSummary("Get current actor")
            .Produces<CurrentActorResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetCurrentActor(
        HttpContext context,
        CurrentActorService service,
        CancellationToken cancellationToken = default)
    {
        Result<CurrentActorResponse, ActorErrors> result = await service.GetCurrentActorAsync(context.User, cancellationToken);
        return result.Match<IResult>(
            Results.Ok,
            error => error switch
            {
                ActorErrors.AmbiguousEmployeeMatch => Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Ambiguous employee match",
                    detail: "Multiple employees matched the authenticated actor claims."),
                _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unexpected actor resolution error.")
            });
    }
}
