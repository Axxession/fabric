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
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Fabric.Server.Sagas.LearningRequirements;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Application;

public sealed class ReceptionKioskComplianceService(
    ReceptionDbContext receptionDb,
    ContractorsDbContext contractorsDb,
    LearningDbContext learningDb,
    RequirementsDbContext requirementsDb,
    GrantRequirementsService grantRequirementsService,
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

        KioskRequirementContext context = await BuildRequirementContextAsync(arrival, cancellationToken);
        if (!context.LocationId.HasValue)
        {
            return Result.Success<ReceptionKioskComplianceResponse, ReceptionErrors>(
                new ReceptionKioskComplianceResponse(ContextComplianceStatus.Compliant, []));
        }

        Result<IReadOnlyList<DerivedGrantRequirement>, RequirementsEvaluationErrors> derivation = await grantRequirementsService.DeriveForGrantAsync(
            context.IdentityId ?? Guid.Empty,
            context.SubjectKind,
            context.LocationId.Value,
            context.ContractorJobTypeIds,
            cancellationToken);
        if (derivation.IsFailure(out _))
            return Result.Success<ReceptionKioskComplianceResponse, ReceptionErrors>(new ReceptionKioskComplianceResponse(ContextComplianceStatus.NonCompliant, []));

        derivation.IsSuccess(out IReadOnlyList<DerivedGrantRequirement> derivedRequirements);
        if (derivedRequirements.Count == 0)
            return Result.Success<ReceptionKioskComplianceResponse, ReceptionErrors>(new ReceptionKioskComplianceResponse(ContextComplianceStatus.Compliant, []));

        Guid[] requirementDefinitionIds = derivedRequirements.Select(item => item.RequirementDefinitionId).Distinct().ToArray();
        Dictionary<Guid, RequirementDefinition> definitionsById = await requirementsDb.RequirementDefinitions
            .AsNoTracking()
            .Where(item => requirementDefinitionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        Dictionary<Guid, EvaluatedGrantRequirement> evaluationsById = context.IdentityId.HasValue
            ? (await grantRequirementsService.EvaluateGrantRequirementsAsync(context.IdentityId.Value, requirementDefinitionIds, cancellationToken))
                .ToDictionary(item => item.RequirementDefinitionId)
            : [];
        Dictionary<Guid, OutstandingLearningCourseOption> coursesByRequirementId = context.IdentityId.HasValue
            ? (await learningRequirementAutomationService.ListOutstandingLearningCoursesAsync(context.IdentityId.Value, requirementDefinitionIds, cancellationToken))
                .GroupBy(item => item.RequirementDefinitionId)
                .ToDictionary(group => group.Key, group => group.First())
            : [];

        ReceptionKioskComplianceRequirementResponse[] requirements = derivedRequirements
            .Select(item => BuildRequirementResponse(item, definitionsById.GetValueOrDefault(item.RequirementDefinitionId), evaluationsById.GetValueOrDefault(item.RequirementDefinitionId), coursesByRequirementId.GetValueOrDefault(item.RequirementDefinitionId), context.IdentityId.HasValue))
            .OrderBy(item => item.Name)
            .ToArray();

        ContextComplianceStatus status = requirements.Any(item => item.IsBlocking && item.Status != RequirementResultStatus.Fulfilled)
            ? ContextComplianceStatus.NonCompliant
            : requirements.Any(item => item.ValidUntil.HasValue)
                ? ContextComplianceStatus.TemporarilyCompliant
                : ContextComplianceStatus.Compliant;

        return Result.Success<ReceptionKioskComplianceResponse, ReceptionErrors>(new ReceptionKioskComplianceResponse(status, requirements));
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

    private async Task<KioskRequirementContext> BuildRequirementContextAsync(ExpectedArrival arrival, CancellationToken cancellationToken)
    {
        Guid? identityId = await ResolveIdentityIdAsync(arrival, cancellationToken);
        if (arrival.Type != ArrivalType.Contractor || !arrival.JobAssignmentId.HasValue)
            return new KioskRequirementContext(arrival.LocationId, identityId, arrival.Type == ArrivalType.Contractor ? RequirementSubjectKind.Contractor : RequirementSubjectKind.Visitor, null);

        Guid[] jobTypeIds = await contractorsDb.ContractorJobAssignments
            .AsNoTracking()
            .Where(item => item.Id == arrival.JobAssignmentId.Value)
            .Join(contractorsDb.ContractorJobs.AsNoTracking(), assignment => assignment.ContractorJobId, job => job.Id, (_, job) => job.JobTypeId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        return new KioskRequirementContext(arrival.LocationId, identityId, RequirementSubjectKind.Contractor, jobTypeIds);
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
        DerivedGrantRequirement requirement,
        RequirementDefinition? definition,
        EvaluatedGrantRequirement? evaluation,
        OutstandingLearningCourseOption? course,
        bool hasIdentity)
    {
        RequirementResultStatus status = evaluation?.Status ?? RequirementResultStatus.Missing;
        string reason = hasIdentity
            ? evaluation?.Reason ?? "Requirement evidence is missing."
            : "No linked identity is available for compliance evaluation.";

        return new ReceptionKioskComplianceRequirementResponse(
            requirement.RequirementDefinitionId,
            definition?.Code ?? requirement.RequirementDefinitionId.ToString(),
            definition?.Name ?? requirement.RequirementDefinitionId.ToString(),
            requirement.IsBlocking,
            status,
            reason,
            evaluation?.ValidUntil,
            course is null ? null : new ReceptionKioskLearningCourseOptionResponse(course.CourseId, course.CourseCode, course.CourseTitle));
    }

    private sealed record KioskRequirementContext(Guid? LocationId, Guid? IdentityId, RequirementSubjectKind SubjectKind, IReadOnlyCollection<Guid>? ContractorJobTypeIds);
}
