using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Contracts;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Endpoints;

public static class AccessControlEndpoints
{
    public static IEndpointRouteBuilder MapAccessControlEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder root = app.MapGroup("/api/access-control");
        RouteGroupBuilder systems = root.MapGroup("/systems");
        RouteGroupBuilder items = root.MapGroup("/items");
        RouteGroupBuilder credentialTargets = root.MapGroup("/credential-type-targets");
        RouteGroupBuilder credentialAssignments = root.MapGroup("/credential-pacs-assignments");
        RouteGroupBuilder assignments = root.MapGroup("/assignments");
        RouteGroupBuilder provisionings = root.MapGroup("/provisionings");
        RouteGroupBuilder subjects = root.MapGroup("/subjects");
        RouteGroupBuilder subjectProvisionings = root.MapGroup("/subject-provisionings");

        systems.MapGet("", ListSystems)
            .Produces<Page<AccessControlSystemResponse>>();
        systems.MapPost("/unipass", CreateUnipassSystem)
            .Produces<AccessControlSystemResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        systems.MapGet("/{systemId:guid}", GetSystem)
            .Produces<AccessControlSystemResponse>()
            .Produces(StatusCodes.Status404NotFound);
        systems.MapGet("/{systemId:guid}/details", GetSystemDetails)
            .Produces<AccessControlSystemDetailsResponse>()
            .Produces(StatusCodes.Status404NotFound);
        systems.MapPut("/{systemId:guid}/unipass", UpdateUnipassSystem)
            .Produces<AccessControlSystemResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        systems.MapGet("/{systemId:guid}/metadata", GetSystemMetadata)
            .Produces<SystemMetadata>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        systems.MapPost("/{systemId:guid}/audit", TriggerSystemAudit)
            .Produces<AccessControlSystemAuditResponse>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        systems.MapGet("/{systemId:guid}/locations", ListSystemLocations)
            .Produces<Page<AccessControlSystemLocationResponse>>();
        systems.MapPost("/{systemId:guid}/locations", LinkSystemLocation)
            .Produces<AccessControlSystemLocationResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        systems.MapDelete("/locations/{linkId:guid}", DeleteSystemLocation)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        systems.MapGet("/resolve/{locationId:guid}", ResolveSystemForLocation)
            .Produces<ResolveAccessControlSystemResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        items.MapGet("", ListItems)
            .Produces<Page<AccessItemResponse>>();
        items.MapPost("", CreateItem)
            .Produces<AccessItemResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        items.MapGet("/{itemId:guid}", GetItem)
            .Produces<AccessItemResponse>()
            .Produces(StatusCodes.Status404NotFound);
        items.MapPut("/{itemId:guid}", UpdateItem)
            .Produces<AccessItemResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        items.MapGet("/{itemId:guid}/targets", ListTargets)
            .Produces<Page<AccessLevelTargetResponse>>();
        items.MapPost("/{itemId:guid}/targets/unipass", CreateUnipassTarget)
            .Produces<UnipassAccessLevelTargetResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        items.MapPut("/targets/unipass/{targetId:guid}", UpdateUnipassTarget)
            .Produces<UnipassAccessLevelTargetResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        credentialTargets.MapGet("", ListCredentialTypeTargets)
            .Produces<Page<CredentialTypeTargetResponse>>();
        credentialTargets.MapPost("/unipass", CreateUnipassCredentialTypeTarget)
            .Produces<UnipassCredentialTypeTargetResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        credentialTargets.MapPut("/unipass/{targetId:guid}", UpdateUnipassCredentialTypeTarget)
            .Produces<UnipassCredentialTypeTargetResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        credentialAssignments.MapGet("", ListCredentialPacsAssignments)
            .Produces<Page<CredentialPACSAssignmentResponse>>();

        assignments.MapGet("", ListAssignments)
            .Produces<Page<PACSAssignmentResponse>>();
        assignments.MapGet("/{assignmentId:guid}", GetAssignment)
            .Produces<PACSAssignmentResponse>()
            .Produces(StatusCodes.Status404NotFound);
        assignments.MapPost("", CreateAssignments)
            .Produces<PACSAssignmentResponse[]>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        assignments.MapPost("/{assignmentId:guid}/revoke", RevokeAssignment)
            .Produces<PACSAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        provisionings.MapGet("", ListProvisionings)
            .Produces<Page<PACSProvisioningResponse>>();
        provisionings.MapGet("/{provisioningId:guid}", GetProvisioning)
            .Produces<PACSProvisioningResponse>()
            .Produces(StatusCodes.Status404NotFound);

        subjects.MapGet("", ListSubjects)
            .Produces<Page<PACSSubjectResponse>>();
        subjects.MapGet("/{subjectId:guid}", GetSubject)
            .Produces<PACSSubjectResponse>()
            .Produces(StatusCodes.Status404NotFound);
        subjects.MapPost("/{subjectId:guid}/block-provisioning", BlockSubjectProvisioning)
            .Produces<PACSSubjectResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        subjects.MapPost("/{subjectId:guid}/allow-provisioning", AllowSubjectProvisioning)
            .Produces<PACSSubjectResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        subjects.MapPost("/{subjectId:guid}/audit", TriggerSubjectAudit)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        subjectProvisionings.MapGet("", ListSubjectProvisionings)
            .Produces<Page<PACSSubjectProvisioningResponse>>();
        subjectProvisionings.MapPost("", UpsertSubjectProvisioning)
            .Produces<PACSSubjectProvisioningResultResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListSystems(
        [AsParameters] ListAccessControlSystemsRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AccessControlSystem> query = db.AccessControlSystems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(system => system.Name.ToLower().Contains(request.Name.ToLower()));

        IPaged<AccessControlSystem> result = await query
            .OrderBy(system => system.Name)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(system => system.ToResponse()));
    }

    private static async Task<IResult> CreateUnipassSystem(
        [FromBody] CreateUnipassAccessControlSystemRequest request,
        AccessControlSystemService service,
        CancellationToken cancellationToken = default)
    {
        Result<UnipassSystemConfig, AccessControlErrors> config = UnipassSystemConfig.Create(
            request.Endpoint,
            request.SslValidation,
            request.Username,
            request.Password);

        if (config.IsFailure(out AccessControlErrors error))
            return Result.Failure(error).AsResponse(MapError);

        config.IsSuccess(out UnipassSystemConfig value);
        Result<AccessControlSystem, AccessControlErrors> result = await service.CreateUnipassSystemAsync(request.Name, value, request.AnomalyBlockMode, cancellationToken);

        return result.Match<IResult>(
            system => Results.Created($"/api/access-control/systems/{system.Id}", system.ToResponse()),
            error => MapError(error).ToResult());
    }

    private static async Task<IResult> GetSystem(
        Guid systemId,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        AccessControlSystem? system = await db.AccessControlSystems.AsNoTracking().SingleOrDefaultAsync(item => item.Id == systemId, cancellationToken);
        return system is null ? Results.NotFound() : Results.Ok(system.ToResponse());
    }

    private static async Task<IResult> GetSystemDetails(
        Guid systemId,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        AccessControlSystem? system = await db.AccessControlSystems.AsNoTracking().SingleOrDefaultAsync(item => item.Id == systemId, cancellationToken);
        return system is null ? Results.NotFound() : Results.Ok(system.ToDetailsResponse());
    }

    private static async Task<IResult> UpdateUnipassSystem(
        Guid systemId,
        [FromBody] UpdateUnipassAccessControlSystemRequest request,
        AccessControlSystemService service,
        CancellationToken cancellationToken = default)
    {
        Result<AccessControlSystem, AccessControlErrors> result = await service.UpdateUnipassSystemAsync(
            systemId,
            request.Name,
            request.Endpoint,
            request.SslValidation,
            request.Username,
            request.Password,
            request.Status,
            request.AnomalyBlockMode,
            cancellationToken);

        return result.Map(system => system.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> GetSystemMetadata(
        Guid systemId,
        AccessControlSystemService service,
        CancellationToken cancellationToken = default)
    {
        Result<SystemMetadata, AccessControlErrors> result = await service.FetchMetadataAsync(systemId, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> TriggerSystemAudit(
        Guid systemId,
        PACSSubjectConformityAuditService service,
        CancellationToken cancellationToken = default)
    {
        Result<PACSSubjectConformityAuditService.PACSSubjectConformityAuditEnqueueSummary, AccessControlErrors> result = await service.EnqueueByAccessControlSystemIdAsync(systemId, cancellationToken);
        return result.Match<IResult>(summary => Results.Accepted($"/api/access-control/systems/{systemId}/audit", summary.ToResponse()), error => MapError(error).ToResult());
    }

    private static async Task<IResult> ListSystemLocations(
        Guid systemId,
        [AsParameters] BaseListRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IPaged<AccessControlSystemLocation> result = await db.AccessControlSystemLocations
            .AsNoTracking()
            .Where(link => link.AccessControlSystemId == systemId)
            .OrderBy(link => link.LocationId)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(link => link.ToResponse()));
    }

    private static async Task<IResult> LinkSystemLocation(
        Guid systemId,
        [FromBody] LinkAccessControlSystemLocationRequest request,
        AccessControlSystemService service,
        CancellationToken cancellationToken = default)
    {
        Result<AccessControlSystemLocation, AccessControlErrors> result = await service.LinkLocationAsync(systemId, request.LocationId, cancellationToken);

        return result.Match<IResult>(
            link => Results.Created($"/api/access-control/systems/locations/{link.Id}", link.ToResponse()),
            error => MapError(error).ToResult());
    }

    private static async Task<IResult> DeleteSystemLocation(
        Guid linkId,
        AccessControlSystemService service,
        CancellationToken cancellationToken = default)
    {
        Result<AccessControlErrors> result = await service.UnlinkLocationAsync(linkId, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> ResolveSystemForLocation(
        Guid locationId,
        AccessControlLocationResolver resolver,
        CancellationToken cancellationToken = default)
    {
        Result<ResolvedAccessControlSystem, AccessControlErrors> result = await resolver.ResolveSystemForLocationAsync(locationId, cancellationToken);
        return result.Map(resolved => resolved.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListItems(
        [AsParameters] ListAccessItemsRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AccessItem> query = db.AccessItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(item => item.Name.ToLower().Contains(request.Name.ToLower()));

        IPaged<AccessItem> result = await query
            .OrderBy(item => item.Name)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateItem(
        [FromBody] CreateAccessItemRequest request,
        AccessItemService service,
        CancellationToken cancellationToken = default)
    {
        Result<AccessItem, AccessControlErrors> result = await service.CreateAsync(request.Name, request.Description, cancellationToken);

        return result.Match<IResult>(
            item => Results.Created($"/api/access-control/items/{item.Id}", item.ToResponse()),
            error => MapError(error).ToResult());
    }

    private static async Task<IResult> GetItem(
        Guid itemId,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        AccessItem? item = await db.AccessItems.AsNoTracking().SingleOrDefaultAsync(existing => existing.Id == itemId, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item.ToResponse());
    }

    private static async Task<IResult> UpdateItem(
        Guid itemId,
        [FromBody] UpdateAccessItemRequest request,
        AccessItemService service,
        CancellationToken cancellationToken = default)
    {
        Result<AccessItem, AccessControlErrors> result = await service.UpdateAsync(itemId, request.Name, request.Description, request.Status, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListTargets(
        Guid itemId,
        [AsParameters] BaseListRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IPaged<AccessLevelTarget> result = await db.AccessLevelTargets
            .AsNoTracking()
            .Where(target => target.AccessItemId == itemId)
            .OrderBy(target => target.Name)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(target => target.ToResponse()));
    }

    private static async Task<IResult> CreateUnipassTarget(
        Guid itemId,
        [FromBody] CreateUnipassAccessLevelTargetRequest request,
        AccessLevelTargetService service,
        CancellationToken cancellationToken = default)
    {
        Result<UnipassAccessLevelTarget, AccessControlErrors> result = await service.CreateUnipassTargetAsync(
            itemId,
            request.AccessControlSystemId,
            request.LocationId,
            request.Name,
            request.SiteId,
            request.AccessRuleId,
            request.ProvisioningTiming,
            cancellationToken);

        return result.Match<IResult>(
            target => Results.Created($"/api/access-control/items/targets/unipass/{target.Id}", (UnipassAccessLevelTargetResponse)target.ToResponse()),
            error => MapError(error).ToResult());
    }

    private static async Task<IResult> ListCredentialTypeTargets(
        [AsParameters] ListCredentialTypeTargetsRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CredentialTypeTarget> query = db.CredentialTypeTargets.AsNoTracking();
        if (request.CredentialTypeId.HasValue)
            query = query.Where(item => item.CredentialTypeId == request.CredentialTypeId.Value);
        if (request.AccessControlSystemId.HasValue)
            query = query.Where(item => item.AccessControlSystemId == request.AccessControlSystemId.Value);

        IPaged<CredentialTypeTarget> result = await query.OrderBy(item => item.CredentialTypeId).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateUnipassCredentialTypeTarget(
        [FromBody] CreateUnipassCredentialTypeTargetRequest request,
        CredentialPACSAssignmentService service,
        CancellationToken cancellationToken = default)
    {
        Result<UnipassCredentialTypeTarget, AccessControlErrors> result = await service.CreateUnipassCredentialTypeTargetAsync(request.CredentialTypeId, request.AccessControlSystemId, request.ProvisioningTiming, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/access-control/credential-type-targets/unipass/{item.Id}", (UnipassCredentialTypeTargetResponse)item.ToResponse()), error => MapError(error).ToResult());
    }

    private static async Task<IResult> UpdateUnipassCredentialTypeTarget(
        Guid targetId,
        [FromBody] UpdateUnipassCredentialTypeTargetRequest request,
        CredentialPACSAssignmentService service,
        CancellationToken cancellationToken = default)
    {
        Result<UnipassCredentialTypeTarget, AccessControlErrors> result = await service.UpdateUnipassCredentialTypeTargetAsync(targetId, request.ProvisioningTiming, request.IsEnabled, cancellationToken);
        return result.Map(item => (UnipassCredentialTypeTargetResponse)item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListCredentialPacsAssignments(
        [AsParameters] ListCredentialPACSAssignmentsRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CredentialPACSAssignment> query = db.CredentialPACSAssignments.AsNoTracking();

        if (request.CredentialIds.Length > 0)
            query = query.Where(item => request.CredentialIds.Contains(item.CredentialId));

        if (request.CredentialId.HasValue)
            query = query.Where(item => item.CredentialId == request.CredentialId.Value);
        if (request.AccessControlSystemId.HasValue)
            query = query.Where(item => item.AccessControlSystemId == request.AccessControlSystemId.Value);
        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);

        IPaged<CredentialPACSAssignment> result = await query.OrderBy(item => item.ScheduledFor).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> UpdateUnipassTarget(
        Guid targetId,
        [FromBody] UpdateUnipassAccessLevelTargetRequest request,
        AccessLevelTargetService service,
        CancellationToken cancellationToken = default)
    {
        Result<UnipassAccessLevelTarget, AccessControlErrors> result = await service.UpdateUnipassTargetAsync(
            targetId,
            request.LocationId,
            request.Name,
            request.SiteId,
            request.AccessRuleId,
            request.IsEnabled,
            request.ProvisioningTiming,
            cancellationToken);

        return result.Map(target => (UnipassAccessLevelTargetResponse)target.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListAssignments(
        [AsParameters] ListPACSAssignmentsRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PACSAssignment> query = db.PACSAssignments.AsNoTracking();

        if (request.SourceAssignmentId.HasValue)
            query = query.Where(item => item.SourceAssignmentId == request.SourceAssignmentId.Value);

        if (request.IdentityId.HasValue)
            query = query.Where(item => item.IdentityId == request.IdentityId.Value);

        if (request.AccessControlSystemId.HasValue)
            query = query.Where(item => item.AccessControlSystemId == request.AccessControlSystemId.Value);

        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);

        IPaged<PACSAssignment> result = await query
            .OrderBy(item => item.IdentityId)
            .ThenBy(item => item.ValidFrom)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetAssignment(
        Guid assignmentId,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        PACSAssignment? assignment = await db.PACSAssignments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
        return assignment is null ? Results.NotFound() : Results.Ok(assignment.ToResponse());
    }

    private static async Task<IResult> CreateAssignments(
        [FromBody] CreatePACSAssignmentsRequest request,
        PACSAssignmentService service,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyList<PACSAssignment>, AccessControlErrors> result = await service.CreateAssignmentsForGrantAsync(
            request.SourceAssignmentId,
            request.IdentityId,
            request.AccessItemId,
            request.LocationId,
            request.DurationKind,
            request.ValidFrom,
            request.ValidUntil,
            cancellationToken);

        return result.Match<IResult>(
            assignments => Results.Created(
                "/api/access-control/assignments",
                assignments.Select(item => item.ToResponse()).ToArray()),
            error => MapError(error).ToResult());
    }

    private static async Task<IResult> RevokeAssignment(
        Guid assignmentId,
        PACSAssignmentService service,
        CancellationToken cancellationToken = default)
    {
        Result<PACSAssignment, AccessControlErrors> result = await service.RevokeAsync(assignmentId, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListProvisionings(
        [AsParameters] ListPACSProvisioningsRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PACSProvisioning> query = db.PACSProvisionings.AsNoTracking();

        if (request.IdentityId.HasValue)
            query = query.Where(item => item.IdentityId == request.IdentityId.Value);

        if (request.AccessControlSystemId.HasValue)
            query = query.Where(item => item.AccessControlSystemId == request.AccessControlSystemId.Value);

        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);

        IPaged<PACSProvisioning> result = await query
            .OrderBy(item => item.IdentityId)
            .ThenBy(item => item.ValidFrom)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        PACSProvisioning[] items = result.Items.ToArray();
        Dictionary<Guid, Guid[]> links = await db.PACSProvisioningSourceAssignments.AsNoTracking()
            .Where(item => items.Select(x => x.Id).Contains(item.PACSProvisioningId))
            .GroupBy(item => item.PACSProvisioningId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.PACSAssignmentId).ToArray(), cancellationToken);

        return Results.Ok(result.Map(item => item.ToResponse(links.GetValueOrDefault(item.Id, []))));
    }

    private static async Task<IResult> GetProvisioning(
        Guid provisioningId,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        PACSProvisioning? provisioning = await db.PACSProvisionings.AsNoTracking().SingleOrDefaultAsync(item => item.Id == provisioningId, cancellationToken);
        if (provisioning is null)
            return Results.NotFound();

        Guid[] links = await db.PACSProvisioningSourceAssignments.AsNoTracking()
            .Where(item => item.PACSProvisioningId == provisioningId)
            .Select(item => item.PACSAssignmentId)
            .ToArrayAsync(cancellationToken);

        return Results.Ok(provisioning.ToResponse(links));
    }

    private static async Task<IResult> ListSubjects(
        [AsParameters] ListPACSSubjectsRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PACSSubject> query = db.PACSSubjects.AsNoTracking();

        if (request.IdentityId.HasValue)
            query = query.Where(item => item.IdentityId == request.IdentityId.Value);

        if (request.AccessControlSystemId.HasValue)
            query = query.Where(item => item.AccessControlSystemId == request.AccessControlSystemId.Value);

        IPaged<PACSSubject> result = await query
            .OrderBy(item => item.IdentityId)
            .ThenBy(item => item.AccessControlSystemId)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        Guid[] systemIds = result.Items.Select(item => item.AccessControlSystemId).Distinct().ToArray();
        Dictionary<Guid, AnomalyBlockMode> anomalyModes = await db.AccessControlSystems.AsNoTracking()
            .Where(item => systemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.AnomalyBlockMode, cancellationToken);

        return Results.Ok(result.Map(item => item.ToResponse(anomalyModes.GetValueOrDefault(item.AccessControlSystemId, AnomalyBlockMode.WarnOnly))));
    }

    private static async Task<IResult> GetSubject(
        Guid subjectId,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        PACSSubject? subject = await db.PACSSubjects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == subjectId, cancellationToken);
        if (subject is null)
            return Results.NotFound();

        AnomalyBlockMode anomalyBlockMode = await db.AccessControlSystems.AsNoTracking()
            .Where(item => item.Id == subject.AccessControlSystemId)
            .Select(item => item.AnomalyBlockMode)
            .SingleAsync(cancellationToken);

        return Results.Ok(subject.ToResponse(anomalyBlockMode));
    }

    private static async Task<IResult> BlockSubjectProvisioning(
        Guid subjectId,
        [FromBody] BlockPACSSubjectProvisioningRequest request,
        PACSSubjectService service,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        Result<PACSSubject, AccessControlErrors> result = await service.BlockProvisioningManuallyAsync(subjectId, request.Reason, cancellationToken);
        if (result.IsFailure(out AccessControlErrors error))
            return MapError(error).ToResult();

        result.IsSuccess(out PACSSubject subject);
        AnomalyBlockMode anomalyBlockMode = await db.AccessControlSystems.AsNoTracking()
            .Where(item => item.Id == subject.AccessControlSystemId)
            .Select(item => item.AnomalyBlockMode)
            .SingleAsync(cancellationToken);
        return Results.Ok(subject.ToResponse(anomalyBlockMode));
    }

    private static async Task<IResult> AllowSubjectProvisioning(
        Guid subjectId,
        PACSSubjectService service,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        Result<PACSSubject, AccessControlErrors> result = await service.AllowProvisioningManuallyAsync(subjectId, cancellationToken);
        if (result.IsFailure(out AccessControlErrors error))
            return MapError(error).ToResult();

        result.IsSuccess(out PACSSubject subject);
        AnomalyBlockMode anomalyBlockMode = await db.AccessControlSystems.AsNoTracking()
            .Where(item => item.Id == subject.AccessControlSystemId)
            .Select(item => item.AnomalyBlockMode)
            .SingleAsync(cancellationToken);
        return Results.Ok(subject.ToResponse(anomalyBlockMode));
    }

    private static async Task<IResult> TriggerSubjectAudit(
        Guid subjectId,
        PACSSubjectConformityAuditService service,
        CancellationToken cancellationToken = default)
    {
        Result<PACSSubject, AccessControlErrors> result = await service.EnqueueBySubjectIdAsync(subjectId, cancellationToken);
        return result.Match<IResult>(_ => Results.Accepted(), error => MapError(error).ToResult());
    }

    private static async Task<IResult> ListSubjectProvisionings(
        [AsParameters] ListPACSSubjectProvisioningsRequest request,
        AccessControlDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PACSSubjectProvisioning> query = db.PACSSubjectProvisionings.AsNoTracking();

        if (request.PACSSubjectId.HasValue)
            query = query.Where(item => item.PACSSubjectId == request.PACSSubjectId.Value);

        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);

        IPaged<PACSSubjectProvisioning> result = await query
            .OrderBy(item => item.ScheduledFor)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> UpsertSubjectProvisioning(
        [FromBody] UpsertPACSSubjectProvisioningRequest request,
        PACSSubjectProvisioningService service,
        CancellationToken cancellationToken = default)
    {
        Result<PACSSubjectProvisioningResult, AccessControlErrors> result = await service.UpsertAsync(
            request.IdentityId,
            request.AccessControlSystemId,
            request.DesiredState,
            request.DesiredFirstName,
            request.DesiredLastName,
            request.DesiredEmail,
            request.Reason,
            request.SourceKind,
            request.SourceId,
            cancellationToken);

        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(AccessControlErrors error) =>
        error switch
        {
            AccessControlErrors.SystemNotFound => Problem(StatusCodes.Status404NotFound, "Access control system not found."),
            AccessControlErrors.AccessItemNotFound => Problem(StatusCodes.Status404NotFound, "Access item not found."),
            AccessControlErrors.AccessLevelTargetNotFound => Problem(StatusCodes.Status404NotFound, "Access level target not found."),
            AccessControlErrors.LocationNotFound => Problem(StatusCodes.Status404NotFound, "Location not found."),
            AccessControlErrors.SystemLocationNotFound => Problem(StatusCodes.Status404NotFound, "System location link not found."),
            AccessControlErrors.IdentityNotFound => Problem(StatusCodes.Status404NotFound, "Identity not found."),
            AccessControlErrors.PACSAssignmentNotFound => Problem(StatusCodes.Status404NotFound, "PACS assignment not found."),
            AccessControlErrors.PACSSubjectNotFound => Problem(StatusCodes.Status404NotFound, "PACS subject not found."),
            AccessControlErrors.PACSSubjectProvisioningNotFound => Problem(StatusCodes.Status404NotFound, "PACS subject provisioning not found."),
            AccessControlErrors.ProvisioningBlockReasonRequired => Problem(StatusCodes.Status400BadRequest, "Provisioning block reason is required."),
            AccessControlErrors.ConfigInvalid => Problem(StatusCodes.Status400BadRequest, "Config invalid."),
            AccessControlErrors.SystemProviderNotSupported => Problem(StatusCodes.Status400BadRequest, "System provider not supported."),
            AccessControlErrors.NoAccessLevelTargetsResolved => Problem(StatusCodes.Status400BadRequest, "No enabled access level targets resolved for the requested access item and PACS."),
            AccessControlErrors.AccessControlSystemInactive => Problem(StatusCodes.Status409Conflict, "Access control system is inactive."),
            AccessControlErrors.SystemNameAlreadyExists => Problem(StatusCodes.Status409Conflict, "System name already exists."),
            AccessControlErrors.AccessItemNameAlreadyExists => Problem(StatusCodes.Status409Conflict, "Access item name already exists."),
            AccessControlErrors.AccessLevelTargetAlreadyExists => Problem(StatusCodes.Status409Conflict, "Access level target already exists."),
            AccessControlErrors.LocationAlreadyLinked => Problem(StatusCodes.Status409Conflict, "Location already linked to an access control system."),
            AccessControlErrors.SiteNotFoundInMetadata => Problem(StatusCodes.Status400BadRequest, "Site not found in metadata."),
            AccessControlErrors.AccessRuleNotFoundInMetadata => Problem(StatusCodes.Status400BadRequest, "Access rule not found in metadata."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected access control error.")
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });

    private static IResult ToResult(this (int statusCode, ProblemDetails? problemDetails) error) =>
        error.problemDetails is null ? Results.StatusCode(error.statusCode) : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
