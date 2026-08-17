using Fabric.Server.Actors.Application;
using Fabric.Server.Actors.Contracts;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Contractors.Endpoints;

internal static class ContractorAuthorization
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

    public static async Task<bool> OwnsJobAsync(ContractorsDbContext db, Guid contractorJobId, Guid identityId, CancellationToken cancellationToken) =>
        await db.ContractorJobs.AsNoTracking().AnyAsync(item => item.Id == contractorJobId && item.CreatedByIdentityId == identityId, cancellationToken);

    public static async Task<ContractorJob?> GetOwnedJobAsync(ContractorsDbContext db, Guid contractorJobId, Guid identityId, CancellationToken cancellationToken) =>
        await db.ContractorJobs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == contractorJobId && item.CreatedByIdentityId == identityId, cancellationToken);

    private static IResult MapActorError(ActorErrors error) => error switch
    {
        ActorErrors.AmbiguousEmployeeMatch => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Multiple employees matched the authenticated actor claims."),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unexpected actor resolution error.")
    };
}
