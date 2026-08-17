using Fabric.Server.Contractors.Application;
using Fabric.Server.Contractors.Contracts;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Contractors.Endpoints;

public static class ContractorEndpoints
{
    public static IEndpointRouteBuilder MapContractorEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder contractors = app.MapGroup("/api/contractors/contractors");

        contractors.MapGet("", ListContractors)
            .WithSummary("List contractors")
            .RequireAuthorization(FabricRoleDefaults.ContractorEnrollmentOrPlanningPolicy)
            .Produces<Page<ContractorResponse>>();
        contractors.MapGet("/{id:guid}", GetContractor)
            .WithSummary("Get contractor")
            .RequireAuthorization(FabricRoleDefaults.ContractorEnrollmentOrPlanningPolicy)
            .Produces<ContractorResponse>()
            .Produces(StatusCodes.Status404NotFound);
        contractors.MapPost("", CreateContractor)
            .WithSummary("Create contractor")
            .RequireAuthorization(FabricRoleDefaults.ContractorEnrollmentPolicy)
            .Produces<ContractorResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        contractors.MapPut("/{id:guid}", UpdateContractor)
            .WithSummary("Update contractor")
            .RequireAuthorization(FabricRoleDefaults.ContractorEnrollmentPolicy)
            .Produces<ContractorResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        contractors.MapPost("/{id:guid}/archive", ArchiveContractor)
            .WithSummary("Archive contractor")
            .RequireAuthorization(FabricRoleDefaults.ContractorEnrollmentPolicy)
            .Produces<ContractorResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        contractors.MapPost("/{id:guid}/unarchive", UnarchiveContractor)
            .WithSummary("Unarchive contractor")
            .RequireAuthorization(FabricRoleDefaults.ContractorEnrollmentPolicy)
            .Produces<ContractorResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListContractors(
        [AsParameters] ListContractorsRequest request,
        [FromQuery] Guid[]? ids,
        ContractorsDbContext db,
        IdentitiesDbContext identitiesDb,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Contractor> query = db.Contractors.AsNoTracking();

        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));

        if (request.CompanyId.HasValue)
            query = query.Where(item => item.CompanyId == request.CompanyId.Value);

        if (request.IsArchived.HasValue)
            query = request.IsArchived.Value
                ? query.Where(item => item.ArchivedAt.HasValue)
                : query.Where(item => !item.ArchivedAt.HasValue);

        if (request.IdentityId.HasValue)
        {
            Guid[] contractorIds = await identitiesDb.ContractorAffiliations
                .AsNoTracking()
                .Where(item => item.IdentityId == request.IdentityId.Value)
                .Select(item => item.ContractorId)
                .ToArrayAsync(cancellationToken);
            query = query.Where(item => contractorIds.Contains(item.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.FirstName, filter)
                || EF.Functions.ILike(item.LastName, filter)
                || EF.Functions.ILike(item.FirstName + " " + item.LastName, filter)
                || item.Email != null && EF.Functions.ILike(item.Email, filter));
        }

        IPaged<Contractor> result = await query.OrderBy(item => item.LastName).ThenBy(item => item.FirstName).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        Dictionary<Guid, Guid> identityIdsByContractorId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => result.Items.Select(contractor => contractor.Id).Contains(item.ContractorId))
            .ToDictionaryAsync(item => item.ContractorId, item => item.IdentityId, cancellationToken);

        return Results.Ok(result.Map(item => item.ToResponse(identityIdsByContractorId.GetValueOrDefault(item.Id))));
    }

    private static async Task<IResult> GetContractor(Guid id, ContractorsDbContext db, IdentitiesDbContext identitiesDb, CancellationToken cancellationToken = default)
    {
        Contractor? contractor = await db.Contractors.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (contractor is null)
            return Results.NotFound();

        Guid? identityId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => item.ContractorId == id)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);

        return Results.Ok(contractor.ToResponse(identityId));
    }

    private static async Task<IResult> CreateContractor([FromBody] CreateContractorRequest request, ContractorsService service, IdentitiesDbContext identitiesDb, CancellationToken cancellationToken = default)
    {
        Result<Contractor, ContractorErrors> result = await service.CreateContractorAsync(request, cancellationToken);
        if (result.IsFailure(out ContractorErrors error))
            return result.Map(item => item.ToResponse(request.IdentityId)).AsResponse(MapError);

        result.IsSuccess(out Contractor contractor);
        Guid? identityId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => item.ContractorId == contractor.Id)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);
        return Results.Created($"/api/contractors/contractors/{contractor.Id}", contractor.ToResponse(identityId));
    }

    private static async Task<IResult> UpdateContractor(Guid id, [FromBody] UpdateContractorRequest request, ContractorsService service, IdentitiesDbContext identitiesDb, CancellationToken cancellationToken = default)
    {
        Result<Contractor, ContractorErrors> result = await service.UpdateContractorAsync(id, request, cancellationToken);
        if (result.IsFailure(out _))
            return result.Map(item => item.ToResponse(null)).AsResponse(MapError);

        result.IsSuccess(out Contractor contractor);
        Guid? identityId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => item.ContractorId == contractor.Id)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);
        return Results.Ok(contractor.ToResponse(identityId));
    }

    private static async Task<IResult> ArchiveContractor(Guid id, ContractorsService service, IdentitiesDbContext identitiesDb, CancellationToken cancellationToken = default)
    {
        Result<Contractor, ContractorErrors> result = await service.SetContractorArchivedAsync(id, true, cancellationToken);
        if (result.IsFailure(out _))
            return result.Map(item => item.ToResponse(null)).AsResponse(MapError);

        result.IsSuccess(out Contractor contractor);
        Guid? identityId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => item.ContractorId == contractor.Id)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);
        return Results.Ok(contractor.ToResponse(identityId));
    }

    private static async Task<IResult> UnarchiveContractor(Guid id, ContractorsService service, IdentitiesDbContext identitiesDb, CancellationToken cancellationToken = default)
    {
        Result<Contractor, ContractorErrors> result = await service.SetContractorArchivedAsync(id, false, cancellationToken);
        if (result.IsFailure(out _))
            return result.Map(item => item.ToResponse(null)).AsResponse(MapError);

        result.IsSuccess(out Contractor contractor);
        Guid? identityId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => item.ContractorId == contractor.Id)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);
        return Results.Ok(contractor.ToResponse(identityId));
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(ContractorErrors error) =>
        error switch
        {
            ContractorErrors.ContractorNotFound => Problem(StatusCodes.Status404NotFound, "Contractor not found."),
            ContractorErrors.CompanyNotFound => Problem(StatusCodes.Status404NotFound, "Company not found."),
            ContractorErrors.IdentityNotFound => Problem(StatusCodes.Status404NotFound, "Identity not found."),
            ContractorErrors.ContractorAlreadyLinkedToDifferentIdentity => Problem(StatusCodes.Status409Conflict, "Contractor is already linked to a different identity."),
            ContractorErrors.ContractorAlreadyArchived => Problem(StatusCodes.Status409Conflict, "Contractor is already archived."),
            ContractorErrors.ContractorNotArchived => Problem(StatusCodes.Status409Conflict, "Contractor is not archived."),
            ContractorErrors.FirstNameRequired => Problem(StatusCodes.Status400BadRequest, "First name is required."),
            ContractorErrors.LastNameRequired => Problem(StatusCodes.Status400BadRequest, "Last name is required."),
            _ => Problem(StatusCodes.Status400BadRequest, "Contractor request is invalid."),
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
