using Fabric.Server.Actors.Application;
using Fabric.Server.Core;
using Fabric.Server.Employees.Persistence;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Locations.Application;
using Fabric.Server.Locations.Domain;
using Fabric.Server.Sagas.VisitorPreOnboarding;
using Fabric.Server.Visitors.Application;
using Fabric.Server.Visitors.Contracts;
using Fabric.Server.Visitors.Domain;
using Fabric.Server.Visitors.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Visitors.Endpoints;

public static class VisitorEndpoints
{
    public static IEndpointRouteBuilder MapVisitorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/visitors/visitors", ListVisitors)
            .WithDescription("List visitors")
            .WithSummary("List visitors")
            .Produces<Page<VisitorResponse>>();
        app.MapGet("/api/visitors/visitors/{id:guid}", GetVisitor)
            .WithDescription("Get visitor")
            .WithSummary("Get visitor")
            .Produces<VisitorResponse>()
            .Produces(StatusCodes.Status404NotFound);
        app.MapGet("/api/visitors/invitations/{invitationId:guid}/visit", GetVisitByInvitationId)
            .WithDescription("Retrieve the visit for an invitation")
            .WithSummary("Retrieve invitation visit")
            .Produces<VisitResponse>()
            .Produces(StatusCodes.Status404NotFound);

        RouteGroupBuilder visits = app.MapGroup("/api/visitors/visits");

        visits.MapGet("/{id:guid}", GetVisitById)
            .WithDescription("Retrieve a visit by id")
            .WithSummary("Retrieve a visit by id")
            .Produces<VisitResponse>()
            .Produces(StatusCodes.Status404NotFound);
        visits.MapGet("", ListVisits)
            .WithDescription("List all visits matching the criteria")
            .WithSummary("List visits")
            .Produces<Page<VisitResponse>>();
        visits.MapPost("", CreateVisit)
            .RequireAuthorization(FabricRoleDefaults.HostPolicy)
            .WithDescription("Create a new visit")
            .WithSummary("Create a new visit")
            .Produces<VisitResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);
        visits.MapPost("/{id:guid}/cancel", CancelVisit)
            .WithDescription("Cancel a visit")
            .WithSummary("Cancel a visit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        visits.MapPost("/{id:guid}/reschedule", RescheduleVisit)
            .WithDescription("Reschedule a visit")
            .WithSummary("Reschedule a visit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        visits.MapPut("/{id:guid}/summary", UpdateVisitSummary)
            .WithDescription("Update visit summary")
            .WithSummary("Update visit summary")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        visits.MapPost("/{id:guid}/relocate", RelocateVisit)
            .WithDescription("Relocate a visit")
            .WithSummary("Relocate a visit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        visits.MapPost("/{id:guid}/invitations", InviteToVisit)
            .WithDescription("Invite someone to a visit")
            .WithSummary("Invite someone to a visit")
            .Produces<VisitInvitationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        visits.MapGet("/{visitId:guid}/invitations/{invitationId:guid}/confirmation", GetVisitConfirmation)
            .AllowAnonymous()
            .WithDescription("Retrieve anonymous visit confirmation details")
            .WithSummary("Retrieve visit confirmation")
            .Produces<VisitConfirmationResponse>()
            .Produces(StatusCodes.Status404NotFound);
        visits.MapPost("/{visitId:guid}/invitations/{invitationId:guid}/confirm", ConfirmInvitation)
            .AllowAnonymous()
            .WithDescription("Confirm participation in a visit")
            .WithSummary("Confirm invitation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        visits.MapPost("/{visitId:guid}/invitations/{invitationId:guid}/reject", RejectInvitation)
            .AllowAnonymous()
            .WithDescription("Reject participation in a visit")
            .WithSummary("Reject invitation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListVisitors(
        [AsParameters] ListVisitorsRequest request,
        [FromQuery] Guid[]? ids,
        VisitorsDbContext db,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<Visitor> query = db.Visitors.AsNoTracking();

        if (ids is { Length: > 0 })
            query = query.Where(visitor => ids.Contains(visitor.Id));

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(visitor =>
                EF.Functions.ILike(visitor.FirstName, filter)
                || EF.Functions.ILike(visitor.LastName, filter)
                || EF.Functions.ILike(visitor.FirstName + " " + visitor.LastName, filter)
                || visitor.Email != null && EF.Functions.ILike(visitor.Email, filter)
                || visitor.Company != null && EF.Functions.ILike(visitor.Company, filter)
            );
        }

        IPaged<Visitor> result = await query
            .OrderBy(visitor => visitor.LastName)
            .ThenBy(visitor => visitor.FirstName)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(visitor => visitor.ToResponse()));
    }

    private static async Task<IResult> GetVisitById(
        VisitorsDbContext db,
        HostService hostService,
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        Visit? visitRow = await db
            .Visits.Include(x => x.Invitations)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (visitRow is null)
            return Results.NotFound();

        HostResponse? host = await hostService.GetHostByEmployeeIdAsync(visitRow.HostEmployeeId, cancellationToken);
        return host is null ? Results.NotFound() : Results.Ok(visitRow.ToResponse(host));
    }

    private static async Task<IResult> GetVisitor(
        Guid id,
        VisitorsDbContext db,
        CancellationToken cancellationToken = default)
    {
        Visitor? visitor = await db.Visitors.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return visitor is null ? Results.NotFound() : Results.Ok(visitor.ToResponse());
    }

    private static async Task<IResult> GetVisitByInvitationId(
        Guid invitationId,
        VisitorsDbContext db,
        HostService hostService,
        CancellationToken cancellationToken = default
    )
    {
        Visit? visitRow = await db
            .Visits.Include(x => x.Invitations)
            .SingleOrDefaultAsync(x => x.Invitations.Any(invitation => invitation.Id == invitationId), cancellationToken);

        if (visitRow is null)
            return Results.NotFound();

        HostResponse? host = await hostService.GetHostByEmployeeIdAsync(visitRow.HostEmployeeId, cancellationToken);
        return host is null ? Results.NotFound() : Results.Ok(visitRow.ToResponse(host));
    }

    private static async Task<IResult> ListVisits(
        HttpContext context,
        [FromQuery] VisitStatus[]? withStatus,
        [FromQuery] DateTimeOffset? after,
        [FromQuery] DateTimeOffset? before,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        VisitorsDbContext db,
        HostService hostService,
        CancellationToken cancellationToken = default
    )
    {
        Guid? employeeId = context.User.GetEmployeeId();
        if (!employeeId.HasValue)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                detail: "Authenticated actor is not linked to an employee.");
        }

        IQueryable<Visit> query = db.Visits.Include(x => x.Invitations).AsQueryable();

        if (withStatus is { Length: > 0 })
            query = query.Where(x => withStatus.Contains(x.Status));

        query = query.Where(x => x.HostEmployeeId == employeeId.Value);

        if (after.HasValue)
            query = query.Where(x => x.Start >= after.Value);

        if (before.HasValue)
            query = query.Where(x => x.Stop <= before.Value);

        query = query.OrderBy(x => x.Start);

        IPaged<Visit> result = await query.GetPageAsync(page ?? 0, pageSize ?? 25, cancellationToken);

        Guid[] hostEmployeeIds = result.Items.Select(x => x.HostEmployeeId).Distinct().ToArray();
        Dictionary<Guid, HostResponse> hostMap = await hostService.GetHostMapAsync(hostEmployeeIds, cancellationToken);

        return Results.Ok(result.Map(visit => visit.ToResponse(hostMap[visit.HostEmployeeId])));
    }

    private static async Task<IResult> CreateVisit(
        HttpContext context,
        [FromBody] CreateVisitRequest request,
        VisitService visitService,
        CurrentActorService currentActorService,
        CancellationToken cancellationToken = default
    )
    {
        Result<Fabric.Server.Actors.Contracts.CurrentActorResponse, ActorErrors> actorResult = await currentActorService.GetCurrentActorAsync(context.User, cancellationToken);
        if (actorResult.IsFailure(out ActorErrors actorError))
        {
            return actorError switch
            {
                ActorErrors.AmbiguousEmployeeMatch => Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Ambiguous employee match",
                    detail: "Multiple employees matched the authenticated actor claims."),
                _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "Unexpected actor resolution error.")
            };
        }

        Guid? employeeId = context.User.GetEmployeeId();
        if (!employeeId.HasValue)
        {
            actorResult.IsSuccess(out Fabric.Server.Actors.Contracts.CurrentActorResponse? actor);
            employeeId = actor?.EmployeeId;
        }

        if (!employeeId.HasValue)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                detail: "Authenticated actor is not linked to an employee.");
        }

        Result<(Visit, HostResponse), VisitErrors> result = await visitService.Create(
            employeeId.Value,
            request.Summary,
            request.Start,
            request.Stop,
            request.LocationId,
            cancellationToken
        );

        return result.Map(x => x.Item1.ToResponse(x.Item2)).AsResponse(MapError);
    }

    private static async Task<IResult> CancelVisit(
        Guid id,
        VisitService visitService,
        VisitorPreOnboardingSagaService onboardingSagaService,
        CancellationToken cancellationToken = default
    )
    {
        Result<VisitErrors> result = await visitService.Cancel(id, cancellationToken);

        if (result.IsSuccess(out _))
        {
            await onboardingSagaService.EnqueueVisitCancelledAsync(id, cancellationToken);
        }

        return result.AsResponse(MapError);
    }

    private static async Task<IResult> RescheduleVisit(
        Guid id,
        [FromBody] RescheduleVisitRequest request,
        VisitService visitService,
        VisitorPreOnboardingSagaService onboardingSagaService,
        CancellationToken cancellationToken = default
    )
    {
        Result<VisitErrors> result = await visitService.Reschedule(
            id,
            request.Start,
            request.Stop,
            cancellationToken
        );

        if (result.IsSuccess(out _))
        {
            await onboardingSagaService.EnqueueVisitRescheduledAsync(id, cancellationToken);
        }

        return result.AsResponse(MapError);
    }

    private static async Task<IResult> UpdateVisitSummary(
        Guid id,
        [FromBody] UpdateVisitSummaryRequest request,
        VisitService visitService,
        CancellationToken cancellationToken = default
    )
    {
        Result<VisitErrors> result = await visitService.UpdateSummary(id, request.Summary, cancellationToken);

        return result.AsResponse(MapError);
    }

    private static async Task<IResult> RelocateVisit(
        Guid id,
        [FromBody] RelocateVisitRequest request,
        VisitService visitService,
        VisitorPreOnboardingSagaService onboardingSagaService,
        CancellationToken cancellationToken = default
    )
    {
        Result<VisitErrors> result = await visitService.Relocate(id, request.LocationId, cancellationToken);

        if (result.IsSuccess(out _) && request.LocationId.HasValue)
            await onboardingSagaService.EnqueueVisitRelocatedAsync(id, cancellationToken);

        return result.AsResponse(MapError);
    }

    private static async Task<IResult> InviteToVisit(
        Guid id,
        [FromBody] InviteVisitRequest request,
        VisitService visitService,
        VisitorPreOnboardingSagaService onboardingSagaService,
        VisitorsDbContext db,
        CancellationToken cancellationToken = default
    )
    {
        Result<VisitInvitation, VisitErrors> result = await visitService.Invite(
            id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Company,
            cancellationToken
        );

        if (result.IsSuccess(out VisitInvitation? invitation))
        {
            Visit visit = await db.Visits.SingleAsync(x => x.Id == id, cancellationToken);
            _ = await onboardingSagaService.StartAsync(
                visit.Id,
                invitation.Id,
                visit.Start,
                cancellationToken
            );
        }

        return result.Map(x => x.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> GetVisitConfirmation(
        Guid visitId,
        Guid invitationId,
        VisitorsDbContext db,
        HostService hostService,
        LocationService locationService,
        CancellationToken cancellationToken = default
    )
    {
        Visit? visit = await db
            .Visits.Include(x => x.Invitations.Where(invitation => invitation.Id == invitationId))
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken);

        VisitInvitation? invitation = visit?.Invitations.SingleOrDefault();
        if (visit is null || invitation is null)
            return Results.NotFound();

        HostResponse? host = await hostService.GetHostByEmployeeIdAsync(visit.HostEmployeeId, cancellationToken);
        if (host is null)
            return Results.NotFound();

        string? locationLabel = visit.LocationId.HasValue
            ? FormatLocationLabel(await locationService.GetLocationById(visit.LocationId.Value, cancellationToken))
            : null;

        return Results.Ok(visit.ToConfirmationResponse(invitation, host, locationLabel));
    }

    private static async Task<IResult> ConfirmInvitation(

        Guid visitId,
        Guid invitationId,
        [FromBody] ConfirmInvitationRequest request,
        VisitService visitService,
        VisitorPreOnboardingSagaService onboardingSagaService,
        CancellationToken cancellationToken = default
    )
    {
        Result<Visitor, VisitErrors> result = await visitService.AcceptInvitation(
            visitId,
            invitationId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Company,
            request.Transport,
            request.LicensePlate,
            cancellationToken
        );

        if (result.IsSuccess(out _))
        {
            await onboardingSagaService.EnqueueVisitorConfirmedAsync(visitId, invitationId, cancellationToken);
            return Results.NoContent();
        }

        result.IsFailure(out VisitErrors error);
        (int statusCode, ProblemDetails? problemDetails) = MapError(error);
        return problemDetails is not null ? Results.Json(problemDetails, statusCode: statusCode) : Results.StatusCode(statusCode);
    }

    private static async Task<IResult> RejectInvitation(
        Guid visitId,
        Guid invitationId,
        VisitService visitService,
        VisitorPreOnboardingSagaService onboardingSagaService,
        CancellationToken cancellationToken = default
    )
    {
        Result<VisitErrors> result = await visitService.RejectInvitation(visitId, invitationId, cancellationToken);

        if (result.IsSuccess(out _))
        {
            await onboardingSagaService.EnqueueVisitorRejectedAsync(visitId, invitationId, cancellationToken);
        }

        return result.AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(VisitErrors errors)
    {
        return errors switch
        {
            VisitErrors.VisitNotFound => Problem(StatusCodes.Status404NotFound, "Visit not found."),
            VisitErrors.HostNotFound => Problem(
                StatusCodes.Status404NotFound,
                "Host not found."
            ),
            VisitErrors.InvitationNotFound => Problem(
                StatusCodes.Status404NotFound,
                "Invitation not found."
            ),
            VisitErrors.StartMustBeBeforeStop => Problem(
                StatusCodes.Status400BadRequest,
                "Start must be before stop."
            ),
            VisitErrors.StopMustBeFuture => Problem(
                StatusCodes.Status400BadRequest,
                "Stop must be in the future."
            ),
            VisitErrors.Cancelled => Problem(StatusCodes.Status409Conflict, "Visit is cancelled."),
            VisitErrors.Completed => Problem(StatusCodes.Status409Conflict, "Visit is completed."),
            VisitErrors.AlreadyCancelled => Problem(
                StatusCodes.Status409Conflict,
                "Visit is already cancelled."
            ),
            VisitErrors.InvalidStatus => Problem(
                StatusCodes.Status409Conflict,
                "Visit status does not allow this operation."
            ),
            VisitErrors.DuplicateInvitationEmail => Problem(
                StatusCodes.Status409Conflict,
                "Invitation email already exists."
            ),
            VisitErrors.IdentitySyncFailed => Problem(
                StatusCodes.Status409Conflict,
                "Visitor could not be linked to a canonical identity."
            ),
            VisitErrors.InvitationAlreadyResponded => Problem(
                StatusCodes.Status409Conflict,
                "Invitation has already been responded to."
            ),
            VisitErrors.LicensePlateRequired => Problem(
                StatusCodes.Status400BadRequest,
                "License plate is required."
            ),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected visit error."),
        };
    }

    private static (int statusCode, ProblemDetails problemDetails) Problem(
        int statusCode,
        string detail
    ) => (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });

    private static string? FormatLocationLabel(Location? location) =>
        location switch
        {
            Location.SiteLocation site => site.Site.Name,
            Location.BuildingLocation building => $"{building.Site.Name} / {building.Building.Name}",
            Location.RoomLocation room => $"{room.Site.Name} / {room.Building.Name} / {room.Room.Name}",
            _ => null,
        };

}
