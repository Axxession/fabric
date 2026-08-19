using Fabric.Server.Actors.Application;
using Fabric.Server.Actors.Contracts;
using Fabric.Server.Core;

namespace Fabric.Server.Learning.Endpoints;

internal static class LearningAuthorization
{
    public static async Task<Result<Guid, IResult>> GetCurrentIdentityIdAsync(HttpContext httpContext, CurrentActorService currentActorService, CancellationToken cancellationToken)
    {
        Result<CurrentActorResponse, ActorErrors> actorResult = await currentActorService.GetCurrentActorAsync(httpContext.User, cancellationToken);
        if (actorResult.IsFailure(out ActorErrors actorError))
            return Result.Failure<Guid, IResult>(MapActorError(actorError));

        actorResult.IsSuccess(out CurrentActorResponse? actor);
        if (actor is null || !actor.IdentityId.HasValue)
            return Result.Failure<Guid, IResult>(Results.Problem(statusCode: StatusCodes.Status403Forbidden, detail: "Authenticated actor is not linked to an identity."));

        return Result.Success<Guid, IResult>(actor.IdentityId.Value);
    }

    private static IResult MapActorError(ActorErrors error) => error switch
    {
        ActorErrors.AmbiguousEmployeeMatch => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Multiple employees matched the authenticated actor claims."),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unexpected actor resolution error.")
    };
}
