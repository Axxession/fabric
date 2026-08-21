using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Learning.Application;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence;
using Fabric.Server.Reception.Contracts;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Sagas.LearningRequirements;
using Fabric.Server.Visitors.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Application;

public sealed class ReceptionKioskComplianceService(
    ReceptionDbContext receptionDb,
    ContractorsDbContext contractorsDb,
    LearningDbContext learningDb,
    VisitorsDbContext visitorsDb,
    ContextComplianceService contextComplianceService,
    LearningRequirementAutomationService learningRequirementAutomationService,
    IdentityService identityService,
    EnrollmentService enrollmentService,
    LearningRuntimeService learningRuntimeService)
{
    public async Task<Result<ReceptionKioskComplianceResponse, ReceptionErrors>> GetComplianceAsync(Guid arrivalId, CancellationToken cancellationToken = default)
    {
        ExpectedArrival? arrival = await receptionDb.Arrivals
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == arrivalId, cancellationToken);
        if (arrival is null)
            return Result.Failure<ReceptionKioskComplianceResponse, ReceptionErrors>(ReceptionErrors.ArrivalNotFound);

        Result<ContextComplianceResponse, RequirementsEvaluationErrors> evaluation = await EvaluateArrivalContextAsync(arrival, cancellationToken);
        if (evaluation.IsFailure(out _))
            return Result.Success<ReceptionKioskComplianceResponse, ReceptionErrors>(new ReceptionKioskComplianceResponse(ContextComplianceStatus.NonCompliant, []));

        evaluation.IsSuccess(out ContextComplianceResponse? contextCompliance);
        Guid? identityId = await ResolveIdentityIdAsync(arrival, cancellationToken);
        Guid[] requirementDefinitionIds = contextCompliance!.Requirements.Select(item => item.RequirementDefinitionId).Distinct().ToArray();
        Dictionary<Guid, OutstandingLearningCourseOption> coursesByRequirementId = identityId.HasValue
            ? (await learningRequirementAutomationService.ListOutstandingLearningCoursesAsync(identityId.Value, requirementDefinitionIds, cancellationToken))
                .GroupBy(item => item.RequirementDefinitionId)
                .ToDictionary(group => group.Key, group => group.First())
            : [];

        ReceptionKioskComplianceRequirementResponse[] requirements = contextCompliance.Requirements
            .Select(item => BuildRequirementResponse(item, coursesByRequirementId.GetValueOrDefault(item.RequirementDefinitionId)))
            .OrderBy(item => item.Name)
            .ToArray();

        return Result.Success<ReceptionKioskComplianceResponse, ReceptionErrors>(new ReceptionKioskComplianceResponse(contextCompliance.Status, requirements));
    }

    public async Task<Result<ReceptionKioskComplianceCourseLaunchResponse, ReceptionErrors>> LaunchRequirementCourseAsync(
        Guid arrivalId,
        Guid requirementDefinitionId,
        Guid? languageId,
        CancellationToken cancellationToken = default)
    {
        ExpectedArrival? arrival = await receptionDb.Arrivals
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == arrivalId, cancellationToken);
        if (arrival is null)
            return Result.Failure<ReceptionKioskComplianceCourseLaunchResponse, ReceptionErrors>(ReceptionErrors.ArrivalNotFound);

        Guid? identityId = await ResolveIdentityIdAsync(arrival, cancellationToken);
        if (!identityId.HasValue)
            return Result.Failure<ReceptionKioskComplianceCourseLaunchResponse, ReceptionErrors>(ReceptionErrors.InvalidStatus);

        IReadOnlyList<OutstandingLearningCourseOption> options = await learningRequirementAutomationService.ListOutstandingLearningCoursesAsync(identityId.Value, [requirementDefinitionId], cancellationToken);
        OutstandingLearningCourseOption? option = options.FirstOrDefault(item => item.RequirementDefinitionId == requirementDefinitionId);
        if (option is null)
            return Result.Failure<ReceptionKioskComplianceCourseLaunchResponse, ReceptionErrors>(ReceptionErrors.InvalidStatus);

        CourseLanguageResponse[] languages = await learningDb.CourseLanguages
            .AsNoTracking()
            .Where(item => item.CourseId == option.CourseId && item.IsActive && item.CurrentVersionId.HasValue)
            .OrderBy(item => item.DisplayLabel)
            .Select(item => item.ToResponse())
            .ToArrayAsync(cancellationToken);

        if (languages.Length == 0)
            return Result.Failure<ReceptionKioskComplianceCourseLaunchResponse, ReceptionErrors>(ReceptionErrors.InvalidStatus);

        Guid? selectedLanguageId = languageId;
        if (!selectedLanguageId.HasValue && languages.Length == 1)
            selectedLanguageId = languages[0].Id;

        string? token = null;
        if (selectedLanguageId.HasValue)
        {
            Result<Learning.Domain.Enrollment, EnrollmentErrors> enrollment = await enrollmentService.UpsertEnrollmentAsync(new CreateEnrollmentRequest(option.CourseId, identityId.Value), identityId.Value, cancellationToken);
            if (!enrollment.IsSuccess(out Learning.Domain.Enrollment? currentEnrollment) || currentEnrollment is null)
                return Result.Failure<ReceptionKioskComplianceCourseLaunchResponse, ReceptionErrors>(ReceptionErrors.InvalidStatus);

            Result<Learning.Domain.LaunchSession, EnrollmentErrors> launch = await learningRuntimeService.CreateLaunchSessionAsync(currentEnrollment.Id, selectedLanguageId.Value, null, cancellationToken);
            if (!launch.IsSuccess(out Learning.Domain.LaunchSession? session) || session is null)
                return Result.Failure<ReceptionKioskComplianceCourseLaunchResponse, ReceptionErrors>(ReceptionErrors.InvalidStatus);

            token = session.Token;
        }

        return Result.Success<ReceptionKioskComplianceCourseLaunchResponse, ReceptionErrors>(
            new ReceptionKioskComplianceCourseLaunchResponse(
                requirementDefinitionId,
                option.CourseId,
                option.CourseTitle,
                languages.Select(item => new ReceptionKioskCourseLanguageResponse(item.Id, item.LanguageCode, item.DisplayLabel)).ToArray(),
                token));
    }

    private async Task<Result<ContextComplianceResponse, RequirementsEvaluationErrors>> EvaluateArrivalContextAsync(ExpectedArrival arrival, CancellationToken cancellationToken)
    {
        Guid? identityId = await ResolveIdentityIdAsync(arrival, cancellationToken);
        string? unavailableReason = identityId.HasValue ? null : "No linked identity is available for compliance evaluation.";

        if (arrival.Type == ArrivalType.Visitor)
        {
            if (!arrival.InvitationId.HasValue)
                return Result.Success<ContextComplianceResponse, RequirementsEvaluationErrors>(new ContextComplianceResponse(ContextComplianceStatus.NonCompliant, null, unavailableReason, []));

            VisitWindow? visit = await visitorsDb.Visits
                .AsNoTracking()
                .Where(item => item.Invitations.Any(invitation => invitation.Id == arrival.InvitationId.Value))
                .Select(visit => new VisitWindow(visit.LocationId, visit.Stop))
                .SingleOrDefaultAsync(cancellationToken);

            return visit is null
                ? Result.Success<ContextComplianceResponse, RequirementsEvaluationErrors>(new ContextComplianceResponse(ContextComplianceStatus.NonCompliant, null, unavailableReason, []))
                : await contextComplianceService.EvaluateAsync(identityId, RequirementSubjectKind.Visitor, visit.LocationId, visit.Stop, unavailableReason: unavailableReason, cancellationToken: cancellationToken);
        }

        if (arrival.Type != ArrivalType.Contractor || !arrival.JobAssignmentId.HasValue)
            return await contextComplianceService.EvaluateAsync(identityId, RequirementSubjectKind.Visitor, arrival.LocationId, arrival.ExpectedOffboardTime, unavailableReason: unavailableReason, cancellationToken: cancellationToken);

        ContractorAssignmentWindow? assignment = await contractorsDb.ContractorJobAssignments
            .AsNoTracking()
            .Where(item => item.Id == arrival.JobAssignmentId.Value)
            .Join(contractorsDb.ContractorJobs.AsNoTracking(), assignment => assignment.ContractorJobId, job => job.Id, (assignment, job) => new ContractorAssignmentWindow(job.LocationId, job.JobTypeId, assignment.AssignedUntil))
            .SingleOrDefaultAsync(cancellationToken);

        return assignment is null
            ? Result.Success<ContextComplianceResponse, RequirementsEvaluationErrors>(new ContextComplianceResponse(ContextComplianceStatus.NonCompliant, null, unavailableReason, []))
            : await contextComplianceService.EvaluateAsync(identityId, RequirementSubjectKind.Contractor, assignment.LocationId, assignment.AssignedUntil, [assignment.JobTypeId], unavailableReason, cancellationToken);
    }

    private async Task<Guid?> ResolveIdentityIdAsync(ExpectedArrival arrival, CancellationToken cancellationToken)
    {
        if (arrival.IdentityId.HasValue)
            return arrival.IdentityId.Value;

        if (arrival.Type == ArrivalType.Visitor && arrival.VisitorId.HasValue)
            return await identityService.GetIdentityIdForVisitorAsync(arrival.VisitorId.Value, cancellationToken);

        if (arrival.Type == ArrivalType.Contractor && arrival.ContractorId.HasValue)
            return await identityService.GetIdentityIdForContractorAsync(arrival.ContractorId.Value, cancellationToken);

        return null;
    }

    private static ReceptionKioskComplianceRequirementResponse BuildRequirementResponse(
        RequirementComplianceResponse requirement,
        OutstandingLearningCourseOption? course)
    {
        return new ReceptionKioskComplianceRequirementResponse(
            requirement.RequirementDefinitionId,
            requirement.Code,
            requirement.Name,
            requirement.IsBlocking,
            requirement.Status,
            requirement.Reason,
            requirement.ValidUntil,
            course is null ? null : new ReceptionKioskLearningCourseOptionResponse(course.CourseId, course.CourseCode, course.CourseTitle));
    }

    private sealed record VisitWindow(Guid? LocationId, DateTimeOffset Stop);
    private sealed record ContractorAssignmentWindow(Guid LocationId, Guid JobTypeId, DateTimeOffset AssignedUntil);
}
