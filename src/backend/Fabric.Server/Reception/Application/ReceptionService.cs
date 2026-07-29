using Fabric.Server.Core;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Application;

public class ReceptionService(
    ReceptionDbContext db,
    TimeProvider timeProvider,
    ReceptionLocationScopeService locationScopeService,
    ReceptionTriggeredPackageAssignmentService receptionTriggeredPackageAssignmentService)
{
    private static readonly TimeSpan CompletedArrivalLookupWindow = TimeSpan.FromHours(12);
    private readonly record struct SubjectIdentity(ArrivalType Type, Guid Id);

    private async Task<ExpectedArrival?> GetAggregate(Guid id, CancellationToken ct) =>
        await db.Arrivals
            .Include(a => a.Entries)
            .Include(a => a.Documents)
            .SingleOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Result<ExpectedArrival, ReceptionErrors>> RegisterVisitorArrival(
        string firstName,
        string lastName,
        string? company,
        Guid identityId,
        Guid visitorId,
        Guid invitationId,
        DateTimeOffset expectedArrivalTime,
        DateTimeOffset expectedOffboardTime,
        string? arrivalCode,
        Guid? locationId,
        CancellationToken ct = default)
    {
        if (expectedArrivalTime >= expectedOffboardTime)
            return Result.Failure<ExpectedArrival, ReceptionErrors>(ReceptionErrors.ExpectedArrivalMustBeBeforeExpectedOffboard);

        Result<ReceptionErrors> validation = await ValidateArrivalCodeAssignment(ArrivalType.Visitor, visitorId, arrivalCode, excludedArrivalId: null, ct);
        if (validation.IsFailure(out ReceptionErrors validationError))
            return Result.Failure<ExpectedArrival, ReceptionErrors>(validationError);

        var arrival = ExpectedArrival.CreateVisitorArrival(
                firstName, lastName, company, identityId, visitorId, invitationId, expectedArrivalTime, expectedOffboardTime, arrivalCode, locationId);

        db.Arrivals.Add(arrival);
        await db.SaveChangesAsync(ct);
        await receptionTriggeredPackageAssignmentService.ApplyTrigger(arrival, ReceptionAccessPolicyTrigger.ExpectedVisitorAdded, ct);
        return Result<ExpectedArrival, ReceptionErrors>.Success(arrival);
    }

    public async Task<Result<ReceptionErrors>> UpdateArrivalCode(Guid arrivalId, string arrivalCode, CancellationToken cancellationToken = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, cancellationToken);

        if (arrival is null)
            return Result.Failure(ReceptionErrors.ArrivalNotFound);

        SubjectIdentity subject = GetSubjectIdentity(arrival);
        Result<ReceptionErrors> validation = await ValidateArrivalCodeAssignment(subject.Type, subject.Id, arrivalCode, arrival.Id, cancellationToken);
        if (validation.IsFailure(out _))
            return validation;

        Result<ReceptionErrors> result = arrival.SetArrivalCode(arrivalCode);

        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result<ReceptionErrors>> Reschedule(Guid arrivalId, DateTimeOffset expectedArrivalTime, DateTimeOffset expectedOffboardTime, CancellationToken cancellationToken = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, cancellationToken);

        if (arrival is null)
            return Result.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> result = arrival.Reschedule(expectedArrivalTime, expectedOffboardTime);

        if (result.IsSuccess(out _))
        {
            await db.SaveChangesAsync(cancellationToken);
            await receptionTriggeredPackageAssignmentService.RecreateAssignedPolicies(arrival, cancellationToken);
        }

        return result;
    }

    public async Task<Result<ReceptionErrors>> Relocate(Guid arrivalId, Guid locationId, CancellationToken cancellationToken = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, cancellationToken);

        if (arrival is null)
            return Result.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> result = arrival.Relocate(locationId);

        if (result.IsSuccess(out _))
        {
            await db.SaveChangesAsync(cancellationToken);
            await receptionTriggeredPackageAssignmentService.RecreateAssignedPolicies(arrival, cancellationToken);
        }

        return result;
    }

    public async Task<Result<ReceptionErrors>> Cancel(Guid arrivalId, CancellationToken cancellationToken = default)
    {
        ExpectedArrival? arrival = await db.Arrivals.SingleOrDefaultAsync(x => x.Id == arrivalId, cancellationToken);

        if (arrival is null)
            return Result.Failure(ReceptionErrors.ArrivalNotFound);

        await receptionTriggeredPackageAssignmentService.RetractAssignedPolicies(arrivalId, cancellationToken);
        db.Arrivals.Remove(arrival);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ReceptionErrors>();
    }

    public async Task<Result<ExpectedArrival, ReceptionErrors>> RegisterContractorArrival(
        string firstName,
        string lastName,
        string company,
        Guid? identityId,
        Guid contractorId,
        Guid jobAssignmentId,
        DateTimeOffset expectedArrivalTime,
        DateTimeOffset expectedOffboardTime,
        string? arrivalCode,
        Guid locationId,
        CancellationToken ct = default)
    {
        if (expectedArrivalTime >= expectedOffboardTime)
            return Result.Failure<ExpectedArrival, ReceptionErrors>(ReceptionErrors.ExpectedArrivalMustBeBeforeExpectedOffboard);

        Result<ReceptionErrors> validation = await ValidateArrivalCodeAssignment(ArrivalType.Contractor, contractorId, arrivalCode, excludedArrivalId: null, ct);
        if (validation.IsFailure(out ReceptionErrors validationError))
            return Result.Failure<ExpectedArrival, ReceptionErrors>(validationError);

        var arrival = ExpectedArrival.CreateContractorArrival(
                firstName, lastName, company,
            identityId, contractorId, jobAssignmentId, expectedArrivalTime, expectedOffboardTime, arrivalCode, locationId);

        db.Arrivals.Add(arrival);
        await db.SaveChangesAsync(ct);
        await receptionTriggeredPackageAssignmentService.ApplyTrigger(arrival, ReceptionAccessPolicyTrigger.ContractorExpectedAdded, ct);
        return Result<ExpectedArrival, ReceptionErrors>.Success(arrival);
    }

    public async Task<Result<ReceptionErrors>> Onboard(
        Guid arrivalId,
        List<CheckInDocumentRequirement> requiredDocs,
        List<CheckInDocument> providedDocs,
        string operatorEmail,
        string? operatorDisplayName = null,
        CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> validation = await ValidateSubjectCanOnboard(arrival, ct);
        if (validation.IsFailure(out _))
            return validation;

        Result<ReceptionErrors> result = arrival.Onboard(timeProvider.GetUtcNow(), requiredDocs, providedDocs, operatorEmail, operatorDisplayName);
        if (result.IsSuccess(out _))
            await SaveOnboardedArrival(arrival, arrivalId, ct);

        return result;
    }

    public async Task<Result<ReceptionErrors>> OnboardFromKiosk(
        Guid arrivalId,
        List<CheckInDocumentRequirement> requiredDocs,
        List<CheckInDocument> providedDocs,
        Guid kioskId,
        string kioskName,
        CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> validation = await ValidateSubjectCanOnboard(arrival, ct);
        if (validation.IsFailure(out _))
            return validation;

        Result<ReceptionErrors> result = arrival.Onboard(timeProvider.GetUtcNow(), requiredDocs, providedDocs, kioskId, kioskName);
        if (result.IsSuccess(out _))
            await SaveOnboardedArrival(arrival, arrivalId, ct);

        return result;
    }

    private async Task SaveOnboardedArrival(ExpectedArrival arrival, Guid arrivalId, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        ReceptionAccessPolicyTrigger trigger = arrival.Type == ArrivalType.Visitor
            ? ReceptionAccessPolicyTrigger.VisitorOnboarded
            : ReceptionAccessPolicyTrigger.ContractorOnboarded;
        await receptionTriggeredPackageAssignmentService.ApplyTrigger(arrival, trigger, ct);
    }

    public async Task<Result<ReceptionErrors>> Offboard(Guid arrivalId, string operatorEmail, string? operatorDisplayName = null, CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> result = arrival.Offboard(timeProvider.GetUtcNow(), operatorEmail, operatorDisplayName);
        if (result.IsSuccess(out _))
        {
            await db.SaveChangesAsync(ct);
            await receptionTriggeredPackageAssignmentService.RetractAssignedPolicies(arrivalId, ct);
        }

        return result;
    }

    public async Task<Result<ReceptionErrors>> OffboardFromKiosk(Guid arrivalId, Guid kioskId, string kioskName, CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> result = arrival.Offboard(timeProvider.GetUtcNow(), kioskId, kioskName);
        if (result.IsSuccess(out _))
        {
            await db.SaveChangesAsync(ct);
            await receptionTriggeredPackageAssignmentService.RetractAssignedPolicies(arrivalId, ct);
        }

        return result;
    }

    public async Task<Result<ReceptionErrors>> CheckIn(Guid arrivalId, string operatorEmail, string? operatorDisplayName = null, CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> result = arrival.CheckIn(timeProvider.GetUtcNow(), operatorEmail, operatorDisplayName);
        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(ct);

        return result;
    }

    public async Task<Result<ReceptionErrors>> CheckInFromKiosk(Guid arrivalId, Guid kioskId, string kioskName, CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> result = arrival.CheckIn(timeProvider.GetUtcNow(), kioskId, kioskName);
        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(ct);

        return result;
    }

    public async Task<Result<ReceptionErrors>> CheckOut(Guid arrivalId, string operatorEmail, string? operatorDisplayName = null, CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> result = arrival.CheckOut(timeProvider.GetUtcNow(), operatorEmail, operatorDisplayName);
        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(ct);

        return result;
    }

    public async Task<Result<ReceptionErrors>> CheckOutFromKiosk(Guid arrivalId, Guid kioskId, string kioskName, CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> result = arrival.CheckOut(timeProvider.GetUtcNow(), kioskId, kioskName);
        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(ct);

        return result;
    }

    public async Task<Result<ReceptionErrors>> ConfirmVisitor(string firstName, string lastName, string? company, Guid arrivalId, CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        arrival.UpdateVisitorDetails(firstName, lastName, company);

        Result<ReceptionErrors> result = arrival.ConfirmVisitor();
        if (result.IsSuccess(out _))
        {
            await db.SaveChangesAsync(ct);
            await receptionTriggeredPackageAssignmentService.ApplyTrigger(arrival, ReceptionAccessPolicyTrigger.VisitorConfirmed, ct);
        }

        return result;
    }


    public async Task<Result<ReceptionErrors>> RejectConfirmationVisitor(Guid arrivalId, CancellationToken ct = default)
    {
        ExpectedArrival? arrival = await GetAggregate(arrivalId, ct);
        if (arrival is null)
            return Result<ReceptionErrors>.Failure(ReceptionErrors.ArrivalNotFound);

        Result<ReceptionErrors> result = arrival.RejectConfirmationVisitor();
        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(ct);

        return result;
    }

    public async Task<Result<ExpectedArrival?, ReceptionErrors>> ResolveArrivalForKiosk(string code, ReceptionKiosk kiosk, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result<ExpectedArrival?, ReceptionErrors>.Success(null);

        List<ExpectedArrival> matches = await db.Arrivals
            .AsNoTracking()
            .Where(x => x.ArrivalCode == code && (x.LocationId == null || x.LocationId == kiosk.LocationId))
            .ToListAsync(ct);

        if (matches.Count == 0)
            return Result<ExpectedArrival?, ReceptionErrors>.Success(null);

        List<ExpectedArrival> activeMatches = matches
            .Where(x => x.Status is OnboardingStatus.NotYetOnboarded or OnboardingStatus.Onboarded)
            .ToList();

        if (HasActiveCrossSubjectConflict(activeMatches))
            return Result.Failure<ExpectedArrival?, ReceptionErrors>(ReceptionErrors.ArrivalCodeConflictAcrossSubjects);

        List<ExpectedArrival> onboardedMatches = activeMatches
            .Where(x => x.Status == OnboardingStatus.Onboarded)
            .ToList();

        if (onboardedMatches.Count > 1)
            return Result.Failure<ExpectedArrival?, ReceptionErrors>(ReceptionErrors.SubjectAlreadyHasOnboardedArrival);

        if (onboardedMatches.Count == 1)
        {
            ExpectedArrival selectedOnboarded = onboardedMatches[0];
            if (await HasAnotherOnboardedArrival(GetSubjectIdentity(selectedOnboarded), selectedOnboarded.Id, ct))
                return Result.Failure<ExpectedArrival?, ReceptionErrors>(ReceptionErrors.SubjectAlreadyHasOnboardedArrival);

            return Result<ExpectedArrival?, ReceptionErrors>.Success(selectedOnboarded);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        List<ExpectedArrival> notYetOnboardedMatches = activeMatches
            .Where(x => x.Status == OnboardingStatus.NotYetOnboarded)
            .ToList();

        if (notYetOnboardedMatches.Count > 0)
        {
            List<ExpectedArrival> eligibleNotYetOnboardedMatches = notYetOnboardedMatches
                .Where(x => kiosk.CanOnboardArrivalAt(now, x.ExpectedArrivalTime))
                .ToList();

            return eligibleNotYetOnboardedMatches.Count > 0
                ? Result<ExpectedArrival?, ReceptionErrors>.Success(SelectBestKioskArrival(eligibleNotYetOnboardedMatches, kiosk.LocationId, now))
                : Result.Failure<ExpectedArrival?, ReceptionErrors>(ReceptionErrors.ArrivalOutsideKioskOnboardingWindow);
        }

        List<ExpectedArrival> candidates = matches
            .Where(x => x.Status == OnboardingStatus.Offboarded && now - GetArrivalEndTime(x) <= CompletedArrivalLookupWindow)
            .ToList();

        if (candidates.Count == 0)
            return Result<ExpectedArrival?, ReceptionErrors>.Success(null);

        return Result<ExpectedArrival?, ReceptionErrors>.Success(SelectBestKioskArrival(candidates, kiosk.LocationId, now));
    }

    public async Task<Result<ExpectedArrival?, ReceptionErrors>> ResolveArrivalForWorkstation(string code, ReceptionDeskWorkstation workstation, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result<ExpectedArrival?, ReceptionErrors>.Success(null);

        HashSet<Guid>? scopedLocationIds = await locationScopeService.GetScopedLocationIds(workstation.LocationId, ct);
        if (scopedLocationIds is null || scopedLocationIds.Count == 0)
            return Result<ExpectedArrival?, ReceptionErrors>.Success(null);

        List<ExpectedArrival> matches = await db.Arrivals
            .AsNoTracking()
            .Where(x => x.ArrivalCode == code && x.LocationId.HasValue && scopedLocationIds.Contains(x.LocationId.Value))
            .ToListAsync(ct);

        if (matches.Count == 0)
            return Result<ExpectedArrival?, ReceptionErrors>.Success(null);

        List<ExpectedArrival> activeMatches = matches
            .Where(x => x.Status is OnboardingStatus.NotYetOnboarded or OnboardingStatus.Onboarded)
            .ToList();

        if (HasActiveCrossSubjectConflict(activeMatches))
            return Result.Failure<ExpectedArrival?, ReceptionErrors>(ReceptionErrors.ArrivalCodeConflictAcrossSubjects);

        List<ExpectedArrival> onboardedMatches = activeMatches
            .Where(x => x.Status == OnboardingStatus.Onboarded)
            .ToList();

        if (onboardedMatches.Count > 1)
            return Result.Failure<ExpectedArrival?, ReceptionErrors>(ReceptionErrors.SubjectAlreadyHasOnboardedArrival);

        if (onboardedMatches.Count == 1)
        {
            ExpectedArrival selectedOnboarded = onboardedMatches[0];
            if (await HasAnotherOnboardedArrival(GetSubjectIdentity(selectedOnboarded), selectedOnboarded.Id, ct))
                return Result.Failure<ExpectedArrival?, ReceptionErrors>(ReceptionErrors.SubjectAlreadyHasOnboardedArrival);

            return Result<ExpectedArrival?, ReceptionErrors>.Success(selectedOnboarded);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        List<ExpectedArrival> notYetOnboardedMatches = activeMatches
            .Where(x => x.Status == OnboardingStatus.NotYetOnboarded)
            .ToList();

        if (notYetOnboardedMatches.Count > 0)
            return Result<ExpectedArrival?, ReceptionErrors>.Success(SelectBestKioskArrival(notYetOnboardedMatches, workstation.LocationId, now));

        List<ExpectedArrival> candidates = matches
            .Where(x => x.Status == OnboardingStatus.Offboarded && now - GetArrivalEndTime(x) <= CompletedArrivalLookupWindow)
            .ToList();

        if (candidates.Count == 0)
            return Result<ExpectedArrival?, ReceptionErrors>.Success(null);

        return Result<ExpectedArrival?, ReceptionErrors>.Success(SelectBestKioskArrival(candidates, workstation.LocationId, now));
    }

    private async Task<Result<ReceptionErrors>> ValidateArrivalCodeAssignment(
        ArrivalType subjectType,
        Guid subjectId,
        string? arrivalCode,
        Guid? excludedArrivalId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(arrivalCode))
            return Result.Success<ReceptionErrors>();

        List<ExpectedArrival> activeMatches = await db.Arrivals
            .AsNoTracking()
            .Where(x => x.ArrivalCode == arrivalCode
                && x.Status != OnboardingStatus.Offboarded
                && (!excludedArrivalId.HasValue || x.Id != excludedArrivalId.Value))
            .ToListAsync(ct);

        bool hasCrossSubjectConflict = activeMatches.Any(x => !MatchesSubject(x, subjectType, subjectId));
        return hasCrossSubjectConflict
            ? Result.Failure(ReceptionErrors.ArrivalCodeConflictAcrossSubjects)
            : Result.Success<ReceptionErrors>();
    }

    private async Task<Result<ReceptionErrors>> ValidateSubjectCanOnboard(ExpectedArrival arrival, CancellationToken ct)
    {
        bool hasAnotherOnboarded = await HasAnotherOnboardedArrival(GetSubjectIdentity(arrival), arrival.Id, ct);
        return hasAnotherOnboarded
            ? Result.Failure(ReceptionErrors.SubjectAlreadyHasOnboardedArrival)
            : Result.Success<ReceptionErrors>();
    }

    private async Task<bool> HasAnotherOnboardedArrival(SubjectIdentity subject, Guid excludedArrivalId, CancellationToken ct) =>
        subject.Type switch
        {
            ArrivalType.Visitor => await db.Arrivals
                .AsNoTracking()
                .AnyAsync(x => x.Id != excludedArrivalId && x.VisitorId == subject.Id && x.Status == OnboardingStatus.Onboarded, ct),
            ArrivalType.Contractor => await db.Arrivals
                .AsNoTracking()
                .AnyAsync(x => x.Id != excludedArrivalId && x.ContractorId == subject.Id && x.Status == OnboardingStatus.Onboarded, ct),
            _ => false,
        };

    private static SubjectIdentity GetSubjectIdentity(ExpectedArrival arrival) =>
        arrival.Type switch
        {
            ArrivalType.Visitor when arrival.VisitorId.HasValue => new(arrival.Type, arrival.VisitorId.Value),
            ArrivalType.Contractor when arrival.ContractorId.HasValue => new(arrival.Type, arrival.ContractorId.Value),
            _ => throw new InvalidOperationException("Arrival subject identity is missing."),
        };

    private static bool HasActiveCrossSubjectConflict(List<ExpectedArrival> arrivals)
    {
        if (arrivals.Count <= 1)
            return false;

        SubjectIdentity first = GetSubjectIdentity(arrivals[0]);
        return arrivals.Skip(1).Any(x => GetSubjectIdentity(x) != first);
    }

    private static bool MatchesSubject(ExpectedArrival arrival, ArrivalType subjectType, Guid subjectId) =>
        arrival.Type == subjectType && subjectType switch
        {
            ArrivalType.Visitor => arrival.VisitorId == subjectId,
            ArrivalType.Contractor => arrival.ContractorId == subjectId,
            _ => false,
        };

    private static ExpectedArrival SelectBestKioskArrival(List<ExpectedArrival> arrivals, Guid kioskLocationId, DateTimeOffset now) =>
        arrivals
            .OrderByDescending(x => x.LocationId == kioskLocationId)
            .ThenBy(x => Math.Abs((x.ExpectedArrivalTime - now).Ticks))
            .ThenByDescending(x => x.ExpectedArrivalTime)
            .ThenBy(x => x.Id)
            .First();

    private static DateTimeOffset GetArrivalEndTime(ExpectedArrival arrival) => arrival.OffboardedAt ?? arrival.ExpectedOffboardTime;
}
