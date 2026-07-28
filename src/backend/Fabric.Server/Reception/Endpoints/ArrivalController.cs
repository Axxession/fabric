using System.Security.Claims;
using Fabric.Server.Core;
using Fabric.Server.Employees.Persistence;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Reception.Application;
using Fabric.Server.Reception.Contracts;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Fabric.Server.Sagas.VisitorPreOnboarding;
using Fabric.Server.Visitors.Domain;
using Fabric.Server.Visitors.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Endpoints;

public static class ArrivalEndpoints
{
    public static IEndpointRouteBuilder MapReceptionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder arrivals = app.MapGroup("/api/reception/arrivals");

        arrivals.MapGet("/{id:guid}", GetArrivalById)
            .WithDescription("Retrieve an arrival by id")
            .WithSummary("Retrieve an arrival by id")
            .Produces<ArrivalResponse>()
            .Produces(StatusCodes.Status404NotFound);
        arrivals.MapGet("", ListArrivals)
            .WithDescription("List all arrivals matching the criteria")
            .WithSummary("List arrivals")
            .Produces<Page<ArrivalResponse>>();
        arrivals.MapPost("/{id:guid}/onboard", OnboardArrival)
            .WithDescription("Onboard an arrival with documents")
            .WithSummary("Onboard arrival")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        arrivals.MapPost("/{id:guid}/offboard", OffboardArrival)
            .WithDescription("Offboard an arrival")
            .WithSummary("Offboard arrival")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        arrivals.MapPost("/{id:guid}/check-in", CheckInArrival)
            .WithDescription("Check in an arrival")
            .WithSummary("Check in arrival")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        arrivals.MapPost("/{id:guid}/check-out", CheckOutArrival)
            .WithDescription("Check out an arrival")
            .WithSummary("Check out arrival")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        RouteGroupBuilder kioskArrivals = app.MapGroup("/api/reception/kiosk/arrivals")
            .RequireAuthorization(ReceptionKioskAuthenticationDefaults.Policy);

        kioskArrivals.MapGet("/lookup", LookupArrivalFromKiosk)
            .WithDescription("Look up an expected arrival from a reception kiosk QR code")
            .WithSummary("Kiosk lookup arrival")
            .Produces<ReceptionKioskExpectedArrivalResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status404NotFound);
        kioskArrivals.MapPost("/{id:guid}/onboard", OnboardArrivalFromKiosk)
            .WithDescription("Onboard an arrival from a reception kiosk")
            .WithSummary("Kiosk onboard arrival")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        kioskArrivals.MapPost("/{id:guid}/check-in", CheckInArrivalFromKiosk)
            .WithDescription("Check in an arrival from a reception kiosk")
            .WithSummary("Kiosk check in arrival")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        kioskArrivals.MapPost("/{id:guid}/check-out", CheckOutArrivalFromKiosk)
            .WithDescription("Check out an arrival from a reception kiosk")
            .WithSummary("Kiosk check out arrival")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetArrivalById(
        Guid id,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        ExpectedArrival? arrival = await db.Arrivals
            .Include(a => a.Entries)
            .Include(a => a.Documents)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (arrival is null)
            return Results.NotFound();

        return Results.Ok(arrival.ToResponse());
    }

    private static async Task<IResult> ListArrivals(
        [AsParameters] ListArrivalsRequest request,
        [FromQuery] DateTimeOffset? expectedArrivalAfter,
        [FromQuery] DateTimeOffset? expectedArrivalBefore,
        [FromQuery] DateTimeOffset? onboardedBefore,
        [FromQuery] DateTimeOffset? offboardedAfter,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ExpectedArrival> query = db.Arrivals.Include(a => a.Entries).Include(a => a.Documents).AsQueryable();

        if (request.Type.HasValue)
            query = query.Where(a => a.Type == request.Type.Value);

        if (request.Status.HasValue)
            query = query.Where(a => a.Status == request.Status.Value);

        if (request.CheckedIn.HasValue)
            query = query.Where(a => a.CheckedIn == request.CheckedIn.Value);

        if (request.LocationId.HasValue)
            query = query.Where(a => a.LocationId == request.LocationId.Value);

        if (expectedArrivalAfter.HasValue)
            query = query.Where(a => a.ExpectedArrivalTime >= expectedArrivalAfter.Value);

        if (expectedArrivalBefore.HasValue)
            query = query.Where(a => a.ExpectedArrivalTime < expectedArrivalBefore.Value);

        if (onboardedBefore.HasValue)
            query = query.Where(a => a.OnboardedAt.HasValue && a.OnboardedAt.Value < onboardedBefore.Value);

        if (offboardedAfter.HasValue)
            query = query.Where(a => a.OffboardedAt.HasValue && a.OffboardedAt.Value >= offboardedAfter.Value);

        query = request.Status == OnboardingStatus.Offboarded
            ? query.OrderByDescending(a => a.OffboardedAt)
            : query.OrderByDescending(a => a.ExpectedArrivalTime);

        IPaged<ExpectedArrival> result = await query.GetPageAsync(page ?? 0, pageSize ?? 25, cancellationToken);
        return Results.Ok(result.Map(a => a.ToResponse()));
    }

    private static async Task<IResult> OnboardArrival(
        Guid id,
        [FromBody] OnboardArrivalRequest request,
        ReceptionService receptionService,
        ReceptionDbContext db,
        VisitorPreOnboardingSagaService onboardingSagaService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ReceptionOperatorActor? actor = GetOperatorActor(httpContext.User);
        if (actor is null)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Authenticated operator email claim is required.");

        Result<ReceptionErrors> result = await receptionService.Onboard(id, [], [], actor.Identifier, actor.DisplayName, cancellationToken);
        if (result.IsSuccess(out _))
        {
            ExpectedArrival? arrival = await db.Arrivals.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (arrival?.Type == ArrivalType.Visitor)
                await onboardingSagaService.EnqueueVisitorArrivedAsync(id, cancellationToken);
        }

        return result.AsResponse(MapError);
    }

    private static async Task<IResult> LookupArrivalFromKiosk(
        [FromQuery] string code,
        ReceptionService receptionService,
        ReceptionDbContext db,
        EmployeesDbContext employeesDb,
        VisitorsDbContext visitorsDb,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Results.NotFound();

        Guid kioskLocationId = Guid.Parse(httpContext.User.FindFirstValue(ReceptionKioskAuthenticationDefaults.KioskLocationIdClaim)!);
        Guid kioskId = Guid.Parse(httpContext.User.FindFirstValue(ReceptionKioskAuthenticationDefaults.KioskIdClaim)!);

        ReceptionKiosk? kiosk = await db.ReceptionKiosks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == kioskId, cancellationToken);

        if (kiosk is null)
            return Results.NotFound();

        Result<ExpectedArrival?, ReceptionErrors> lookup = await receptionService.ResolveArrivalForKiosk(code, kiosk, cancellationToken);
        if (lookup.IsFailure(out ReceptionErrors lookupError))
            return lookup.AsResponse(MapError);

        if (!lookup.IsSuccess(out ExpectedArrival? arrival) || arrival is null)
            return Results.NotFound();

        ReceptionKioskVisitorDetailsResponse? visitor = arrival.Type == ArrivalType.Visitor && arrival.InvitationId.HasValue
            ? await GetVisitorDetails(arrival, employeesDb, visitorsDb, cancellationToken)
            : null;

        return Results.Ok(arrival.ToKioskResponse(kiosk, visitor));
    }

    private static async Task<IResult> OffboardArrival(
        Guid id,
        ReceptionService receptionService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ReceptionOperatorActor? actor = GetOperatorActor(httpContext.User);
        if (actor is null)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Authenticated operator email claim is required.");

        Result<ReceptionErrors> result = await receptionService.Offboard(id, actor.Identifier, actor.DisplayName, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> CheckInArrival(
        Guid id,
        ReceptionService receptionService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ReceptionOperatorActor? actor = GetOperatorActor(httpContext.User);
        if (actor is null)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Authenticated operator email claim is required.");

        Result<ReceptionErrors> result = await receptionService.CheckIn(id, actor.Identifier, actor.DisplayName, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> CheckOutArrival(
        Guid id,
        ReceptionService receptionService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ReceptionOperatorActor? actor = GetOperatorActor(httpContext.User);
        if (actor is null)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Authenticated operator email claim is required.");

        Result<ReceptionErrors> result = await receptionService.CheckOut(id, actor.Identifier, actor.DisplayName, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> OnboardArrivalFromKiosk(
        Guid id,
        [FromBody] OnboardArrivalFromKioskRequest request,
        ReceptionService receptionService,
        VisitorPreOnboardingSagaService onboardingSagaService,
        HttpContext httpContext,
        ReceptionDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ReceptionKioskActor actor = GetKioskActor(httpContext.User);
        ReceptionKiosk? kiosk = await db.ReceptionKiosks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == actor.Id, cancellationToken);

        if (kiosk is null)
            return Results.NotFound();

        ExpectedArrival? arrival = await db.Arrivals
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (arrival is null)
            return Results.NotFound();

        if (!kiosk.CanOnboardArrivalAt(timeProvider.GetUtcNow(), arrival.ExpectedArrivalTime))
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, detail: "Arrival is outside kiosk onboarding window.");

        Result<ReceptionErrors> validation = ValidateKioskIdentityVerification(kiosk, request.IdentityVerification);
        if (validation.IsFailure(out ReceptionErrors validationError))
            return validation.AsResponse(MapError);

        List<CheckInDocumentRequirement> requiredDocs = BuildRequiredDocuments(kiosk);
        List<CheckInDocument> providedDocs = BuildProvidedDocuments(request);

        Result<ReceptionErrors> result = await receptionService.OnboardFromKiosk(id, requiredDocs, providedDocs, actor.Id, actor.Name, cancellationToken);
        if (result.IsSuccess(out _))
        {
            if (arrival.Type == ArrivalType.Visitor)
                await onboardingSagaService.EnqueueVisitorArrivedAsync(id, cancellationToken);
        }

        return result.AsResponse(MapError);
    }

    private static async Task<IResult> CheckInArrivalFromKiosk(
        Guid id,
        ReceptionService receptionService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ReceptionKioskActor actor = GetKioskActor(httpContext.User);
        Result<ReceptionErrors> result = await receptionService.CheckInFromKiosk(id, actor.Id, actor.Name, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> CheckOutArrivalFromKiosk(
        Guid id,
        ReceptionService receptionService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ReceptionKioskActor actor = GetKioskActor(httpContext.User);
        Result<ReceptionErrors> result = await receptionService.CheckOutFromKiosk(id, actor.Id, actor.Name, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static ReceptionOperatorActor? GetOperatorActor(ClaimsPrincipal user)
    {
        string? identifier = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("email")
            ?? user.FindFirstValue("preferred_username");

        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        string? displayName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue("name");
        return new ReceptionOperatorActor(identifier, displayName);
    }

    private static ReceptionKioskActor GetKioskActor(ClaimsPrincipal user)
    {
        Guid id = Guid.Parse(user.FindFirstValue(ReceptionKioskAuthenticationDefaults.KioskIdClaim)!);
        string name = user.FindFirstValue(ReceptionKioskAuthenticationDefaults.KioskNameClaim)!;
        return new ReceptionKioskActor(id, name);
    }

    private static List<CheckInDocumentRequirement> BuildRequiredDocuments(ReceptionKiosk kiosk)
    {
        List<CheckInDocumentRequirement> requiredDocuments = [];

        if (kiosk.RequireFacePicture)
        {
            requiredDocuments.Add(new CheckInDocumentRequirement
            {
                Name = "Face picture",
                Required = true,
                DocumentType = CheckInDocumentType.FacePicture
            });
        }

        if (kiosk.IdentityVerificationMethod is IdentityVerificationMethod.Picture)
        {
            requiredDocuments.Add(new CheckInDocumentRequirement
            {
                Name = "Identity document picture",
                Required = true,
                DocumentType = CheckInDocumentType.IdentityDocumentImage
            });
        }

        return requiredDocuments;
    }

    private static List<CheckInDocument> BuildProvidedDocuments(OnboardArrivalFromKioskRequest request)
    {
        List<CheckInDocument> providedDocuments = [];

        if (request.FacePicture is { Length: > 0 })
            providedDocuments.Add(CheckInDocument.Create("Face picture", CheckInDocumentType.FacePicture, request.FacePicture));

        if (request.IdentityVerification is { Method: IdentityVerificationMethod.Picture, Content.Length: > 0 })
        {
            providedDocuments.Add(CheckInDocument.Create(
                "Identity document picture",
                CheckInDocumentType.IdentityDocumentImage,
                request.IdentityVerification.Content));
        }

        return providedDocuments;
    }

    private static Result<ReceptionErrors> ValidateKioskIdentityVerification(
        ReceptionKiosk kiosk,
        IdentityVerificationCaptureRequest? identityVerification)
    {
        if (kiosk.IdentityVerificationMethod is null)
            return identityVerification is null ? Result<ReceptionErrors>.Success() : Result<ReceptionErrors>.Failure(ReceptionErrors.InvalidIdentityVerificationMethod);

        if (identityVerification is null)
            return Result<ReceptionErrors>.Success();

        return identityVerification.Method == kiosk.IdentityVerificationMethod
            ? Result<ReceptionErrors>.Success()
            : Result<ReceptionErrors>.Failure(ReceptionErrors.InvalidIdentityVerificationMethod);
    }

    private static async Task<ReceptionKioskVisitorDetailsResponse?> GetVisitorDetails(
        ExpectedArrival arrival,
        EmployeesDbContext employeesDb,
        VisitorsDbContext visitorsDb,
        CancellationToken cancellationToken)
    {
        Visit? visit = await visitorsDb.Visits
            .AsNoTracking()
            .Include(x => x.Invitations)
            .SingleOrDefaultAsync(x => x.Invitations.Any(invitation => invitation.Id == arrival.InvitationId), cancellationToken);

        VisitInvitation? invitation = visit?.Invitations.SingleOrDefault(x => x.Id == arrival.InvitationId);
        if (visit is null || invitation is null)
            return null;

        Employees.Domain.Employee host = await employeesDb.Employees
            .AsNoTracking()
            .SingleAsync(x => x.Id == visit.HostEmployeeId, cancellationToken);

        var visitDetails = new ReceptionKioskVisitDetailsResponse(
            visit.Id,
            visit.Summary,
            visit.Status,
            visit.Start,
            visit.Stop,
            visit.LocationId,
            $"{host.FirstName} {host.LastName}".Trim(),
            host.Email);

        return new ReceptionKioskVisitorDetailsResponse(
            invitation.VisitorId,
            invitation.Id,
            invitation.Email,
            invitation.ConfirmationStatus,
            invitation.Transport,
            invitation.LicensePlate,
            visitDetails);
    }

    private static (int statusCode, ProblemDetails problemDetails) MapError(ReceptionErrors errors)
    {
        return errors switch
        {
            ReceptionErrors.AlreadyOnboarded => Problem(StatusCodes.Status409Conflict, "Arrival is already onboarded."),
            ReceptionErrors.ArrivalNotFound => Problem(StatusCodes.Status404NotFound, "Arrival not found."),
            ReceptionErrors.NotYetOnboarded => Problem(StatusCodes.Status409Conflict, "Arrival is not yet onboarded."),
            ReceptionErrors.AlreadyOffboarded => Problem(StatusCodes.Status409Conflict, "Arrival is already offboarded."),
            ReceptionErrors.ArrivalOutsideKioskOnboardingWindow => Problem(StatusCodes.Status409Conflict, "Arrival is outside kiosk onboarding window."),
            ReceptionErrors.ArrivalCodeConflictAcrossSubjects => Problem(StatusCodes.Status409Conflict, "Arrival code is already assigned to another active subject."),
            ReceptionErrors.InvalidStatus => Problem(StatusCodes.Status409Conflict, "Arrival status does not allow this operation."),
            ReceptionErrors.MissingRequiredDocuments => Problem(StatusCodes.Status400BadRequest, "Missing required documents."),
            ReceptionErrors.InvalidIdentityVerificationMethod => Problem(StatusCodes.Status400BadRequest, "Identity verification method does not match kiosk configuration."),
            ReceptionErrors.NotAVisitor => Problem(StatusCodes.Status409Conflict, "Arrival is not a visitor."),
            ReceptionErrors.SubjectAlreadyHasOnboardedArrival => Problem(StatusCodes.Status409Conflict, "Subject already has another onboarded arrival."),
            ReceptionErrors.ExpectedArrivalMustBeBeforeExpectedOffboard => Problem(StatusCodes.Status400BadRequest, "Expected arrival must be before expected offboard."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected reception error."),
        };
    }

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) => (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });

    private sealed record ReceptionOperatorActor(string Identifier, string? DisplayName);
    private sealed record ReceptionKioskActor(Guid Id, string Name);
}
