using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Sagas;
using Fabric.Server.Visitors.Domain;
using Fabric.Server.Visitors.Persistence;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Endpoints;

public static class ContextComplianceEndpoints
{
    public static IEndpointRouteBuilder MapContextComplianceEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder compliance = app.MapGroup("/api/requirements/context-compliance");
        compliance.MapGet("/visits/{visitId:guid}/invitations/{invitationId:guid}", GetVisitInvitationCompliance).Produces<ContextComplianceResponse>().Produces(StatusCodes.Status404NotFound);
        compliance.MapPost("/visits/{visitId:guid}/invitations/{invitationId:guid}/waivers", CreateVisitInvitationWaiver).Produces<RequirementEvidenceResponse>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status404NotFound);
        compliance.MapGet("/contractor-assignments/{assignmentId:guid}", GetContractorAssignmentCompliance).Produces<ContextComplianceResponse>().Produces(StatusCodes.Status404NotFound);
        compliance.MapPost("/contractor-assignments/{assignmentId:guid}/waivers", CreateContractorAssignmentWaiver).Produces<RequirementEvidenceResponse>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status404NotFound);
        compliance.MapPost("/contractor-preview", PreviewContractorAssignmentCompliance).Produces<ContractorAssignmentContextComplianceResponse>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> GetVisitInvitationCompliance(
        Guid visitId,
        Guid invitationId,
        VisitorsDbContext visitorsDb,
        IdentitiesDbContext identitiesDb,
        ContextComplianceService service,
        CancellationToken cancellationToken = default)
    {
        Visit? visit = await visitorsDb.Visits
            .AsNoTracking()
            .Include(item => item.Invitations)
            .SingleOrDefaultAsync(item => item.Id == visitId, cancellationToken);
        if (visit is null)
            return Results.NotFound();

        VisitInvitation? invitation = visit.Invitations.SingleOrDefault(item => item.Id == invitationId);
        if (invitation is null)
            return Results.NotFound();

        Guid? identityId = await identitiesDb.VisitorAffiliations
            .AsNoTracking()
            .Where(item => item.VisitorId == invitation.VisitorId)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);

        Result<ContextComplianceResponse, RequirementsEvaluationErrors> result = await service.EvaluateAsync(
            identityId,
            RequirementSubjectKind.Visitor,
            visit.LocationId,
            visit.Stop,
            unavailableReason: identityId.HasValue ? null : "No linked identity is available for compliance evaluation.",
            cancellationToken: cancellationToken);

        return result.Match<IResult>(Results.Ok, error => Results.Problem($"Could not evaluate visit context compliance: {error}.", statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<IResult> GetContractorAssignmentCompliance(
        Guid assignmentId,
        ContractorsDbContext contractorsDb,
        IdentitiesDbContext identitiesDb,
        ContextComplianceService service,
        CancellationToken cancellationToken = default)
    {
        ContractorAssignmentContext? context = await LoadContractorAssignmentContextAsync(contractorsDb, assignmentId, cancellationToken);
        if (context is null)
            return Results.NotFound();

        Guid? identityId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => item.ContractorId == context.ContractorId)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);

        Result<ContextComplianceResponse, RequirementsEvaluationErrors> result = await service.EvaluateAsync(
            identityId,
            RequirementSubjectKind.Contractor,
            context.LocationId,
            context.AssignedUntil,
            contractorJobTypeIds: [context.JobTypeId],
            unavailableReason: identityId.HasValue ? null : "No linked identity is available for compliance evaluation.",
            cancellationToken: cancellationToken);

        return result.Match<IResult>(Results.Ok, error => Results.Problem($"Could not evaluate contractor assignment context compliance: {error}.", statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<IResult> PreviewContractorAssignmentCompliance(
        [FromBody] ContractorAssignmentContextComplianceRequest request,
        ContractorsDbContext contractorsDb,
        IdentitiesDbContext identitiesDb,
        AccessCatalogDbContext accessCatalogDb,
        ContextComplianceService service,
        CancellationToken cancellationToken = default)
    {
        ContractorJob? job = await contractorsDb.ContractorJobs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.ContractorJobId, cancellationToken);
        if (job is null)
            return Results.NotFound();

        Contractor? contractor = await contractorsDb.Contractors.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.ContractorId, cancellationToken);
        if (contractor is null)
            return Results.NotFound();

        if (contractor.CompanyId != job.CompanyId)
            return Results.Problem("Contractor does not belong to the same company as the job.", statusCode: StatusCodes.Status400BadRequest);
        if (request.AssignedUntil <= request.AssignedFrom)
            return Results.Problem("Assigned until must be after assigned from.", statusCode: StatusCodes.Status400BadRequest);
        if (request.AssignedUntil > job.PlannedEnd)
            return Results.Problem("Assignment must fit inside the job window.", statusCode: StatusCodes.Status400BadRequest);

        Guid? identityId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => item.ContractorId == request.ContractorId)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);

        Result<ContextComplianceResponse, RequirementsEvaluationErrors> compliance = await service.EvaluateAsync(
            identityId,
            RequirementSubjectKind.Contractor,
            job.LocationId,
            request.AssignedUntil,
            contractorJobTypeIds: [job.JobTypeId],
            unavailableReason: identityId.HasValue ? null : "No linked identity is available for compliance evaluation.",
            cancellationToken: cancellationToken);
        if (compliance.IsFailure(out RequirementsEvaluationErrors complianceError))
            return Results.Problem($"Could not build contractor assignment compliance preview: {complianceError}.", statusCode: StatusCodes.Status400BadRequest);

        compliance.IsSuccess(out ContextComplianceResponse? contextCompliance);
        Guid[] packageIds = await service.ResolveContractorPackageIdsAsync(job.JobTypeId, job.LocationId, cancellationToken);
        Dictionary<Guid, string> packageNamesById = packageIds.Length == 0
            ? []
            : await accessCatalogDb.Packages.AsNoTracking().Where(item => packageIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        ContractorAssignmentContextCompliancePackageResponse[] packages = packageIds
            .Select(packageId => new ContractorAssignmentContextCompliancePackageResponse(
                packageId,
                packageNamesById.GetValueOrDefault(packageId, packageId.ToString()),
                contextCompliance!.Status,
                contextCompliance.CompliantUntil,
                contextCompliance.Requirements))
            .ToArray();

        return Results.Ok(new ContractorAssignmentContextComplianceResponse(
            request.ContractorId,
            request.ContractorJobId,
            job.LocationId,
            job.JobTypeId,
            contextCompliance!.UnavailableReason,
            packages));
    }

    private static async Task<IResult> CreateVisitInvitationWaiver(
        Guid visitId,
        Guid invitationId,
        [FromBody] CreateContextComplianceWaiverRequest request,
        VisitorsDbContext visitorsDb,
        IdentitiesDbContext identitiesDb,
        ContextComplianceService contextComplianceService,
        RequirementsService requirementsService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        Visit? visit = await visitorsDb.Visits
            .AsNoTracking()
            .Include(item => item.Invitations)
            .SingleOrDefaultAsync(item => item.Id == visitId, cancellationToken);
        if (visit is null)
            return Results.NotFound();

        VisitInvitation? invitation = visit.Invitations.SingleOrDefault(item => item.Id == invitationId);
        if (invitation is null)
            return Results.NotFound();

        Guid? identityId = await identitiesDb.VisitorAffiliations
            .AsNoTracking()
            .Where(item => item.VisitorId == invitation.VisitorId)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);
        if (!identityId.HasValue)
            return Results.Problem("No linked identity is available for compliance evaluation.", statusCode: StatusCodes.Status400BadRequest);
        if (!visit.LocationId.HasValue)
            return Results.Problem("Visit has no location for compliance evaluation.", statusCode: StatusCodes.Status400BadRequest);

        return await CreateWaiverForContextAsync(
            identityId.Value,
            visit.LocationId.Value,
            visit.Stop,
            RequirementSubjectKind.Visitor,
            null,
            request,
            contextComplianceService,
            requirementsService,
            httpContext,
            cancellationToken);
    }

    private static async Task<IResult> CreateContractorAssignmentWaiver(
        Guid assignmentId,
        [FromBody] CreateContextComplianceWaiverRequest request,
        ContractorsDbContext contractorsDb,
        IdentitiesDbContext identitiesDb,
        ContextComplianceService contextComplianceService,
        RequirementsService requirementsService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ContractorAssignmentContext? context = await LoadContractorAssignmentContextAsync(contractorsDb, assignmentId, cancellationToken);
        if (context is null)
            return Results.NotFound();

        Guid? identityId = await identitiesDb.ContractorAffiliations
            .AsNoTracking()
            .Where(item => item.ContractorId == context.ContractorId)
            .Select(item => (Guid?)item.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);
        if (!identityId.HasValue)
            return Results.Problem("No linked identity is available for compliance evaluation.", statusCode: StatusCodes.Status400BadRequest);

        return await CreateWaiverForContextAsync(
            identityId.Value,
            context.LocationId,
            context.AssignedUntil,
            RequirementSubjectKind.Contractor,
            [context.JobTypeId],
            request,
            contextComplianceService,
            requirementsService,
            httpContext,
            cancellationToken);
    }

    private static async Task<IResult> CreateWaiverForContextAsync(
        Guid identityId,
        Guid locationId,
        DateTimeOffset validContextUntil,
        RequirementSubjectKind subjectKind,
        IReadOnlyCollection<Guid>? contractorJobTypeIds,
        CreateContextComplianceWaiverRequest request,
        ContextComplianceService contextComplianceService,
        RequirementsService requirementsService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Results.Problem("Waiver reason is required.", statusCode: StatusCodes.Status400BadRequest);

        Result<ContextComplianceResponse, RequirementsEvaluationErrors> compliance = await contextComplianceService.EvaluateAsync(
            identityId,
            subjectKind,
            locationId,
            validContextUntil,
            contractorJobTypeIds,
            cancellationToken: cancellationToken);
        if (compliance.IsFailure(out RequirementsEvaluationErrors error))
            return Results.Problem($"Could not evaluate context compliance: {error}.", statusCode: StatusCodes.Status400BadRequest);

        compliance.IsSuccess(out ContextComplianceResponse? currentCompliance);
        RequirementComplianceResponse? requirement = currentCompliance!.Requirements.SingleOrDefault(item => item.RequirementDefinitionId == request.RequirementDefinitionId);
        if (requirement is null)
            return Results.Problem("Requirement does not apply in this context.", statusCode: StatusCodes.Status400BadRequest);
        if (!requirement.AllowedEvidenceKinds.Contains(RequirementEvidenceKind.RequirementWaiver))
            return Results.Problem("Requirement does not support waiver evidence.", statusCode: StatusCodes.Status400BadRequest);

        string actor = GetActorReference(httpContext.User);
        Result<RequirementEvidence, RequirementEvidenceErrors> create = await requirementsService.CreateRequirementEvidenceAsync(new CreateRequirementEvidenceRequest(
            identityId,
            request.RequirementDefinitionId,
            RequirementEvidenceKind.RequirementWaiver,
            RequirementEvidenceStatus.Valid,
            null,
            request.ValidUntil,
            string.IsNullOrWhiteSpace(request.SourceReference) ? actor : $"{actor} | {request.SourceReference}",
            request.Reason,
            false,
            DateTimeOffset.UtcNow,
            null,
            null), cancellationToken);

        return create.Match<IResult>(item => Results.Ok(item.ToResponse()), error => Results.Problem($"Could not create waiver evidence: {error}.", statusCode: StatusCodes.Status400BadRequest));
    }

    private static async Task<ContractorAssignmentContext?> LoadContractorAssignmentContextAsync(ContractorsDbContext contractorsDb, Guid assignmentId, CancellationToken cancellationToken)
    {
        return await contractorsDb.ContractorJobAssignments
            .AsNoTracking()
            .Where(item => item.Id == assignmentId)
            .Join(contractorsDb.ContractorJobs.AsNoTracking(), assignment => assignment.ContractorJobId, job => job.Id, (assignment, job) => new ContractorAssignmentContext(
                assignment.ContractorId,
                job.LocationId,
                job.JobTypeId,
                assignment.AssignedUntil))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static string GetActorReference(ClaimsPrincipal user)
    {
        string? identifier = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("email")
            ?? user.FindFirstValue("preferred_username")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue("name");
        return string.IsNullOrWhiteSpace(identifier) ? "Reception desk guard" : $"Reception desk waiver by {identifier}";
    }

    private sealed record ContractorAssignmentContext(Guid ContractorId, Guid LocationId, Guid JobTypeId, DateTimeOffset AssignedUntil);
}
