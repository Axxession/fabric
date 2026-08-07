using AccessControl.Unipass.Contracts;
using AccessControl.Unipass.Entities;
using System.Globalization;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.CredentialManagement.Domain;
using Fabric.Server.CredentialManagement.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fabric.Server.AccessControl.Application;

public sealed class PACSSubjectConformityAuditService(
    AccessControlDbContext db,
    CredentialManagementDbContext credentialDb,
    ITenantContext tenantContext,
    PACSSubjectConformityAuditTrigger trigger,
    UnipassApiFactory apiFactory,
    TimeProvider timeProvider,
    ILogger<PACSSubjectConformityAuditService> logger)
{
    private static readonly TimeSpan RoutineAuditInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan FailedAuditRetryInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ManualAuditCooldown = TimeSpan.FromMinutes(5);

    public sealed record PACSSubjectConformityAuditEnqueueSummary(Guid AccessControlSystemId, int TotalSubjects, int EligibleSubjects, int RecentlyAuditedSubjects, int EnqueuedSubjects);

    public async Task EnqueueAsync(Guid identityId, Guid accessControlSystemId, CancellationToken cancellationToken = default)
    {
        await trigger.EnqueueAsync(new PACSSubjectConformityAuditWorkItem(tenantContext.TenantId, identityId, accessControlSystemId), cancellationToken);
    }

    public async Task<Result<PACSSubject, AccessControlErrors>> EnqueueBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        PACSSubject? subject = await db.PACSSubjects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == subjectId, cancellationToken);
        if (subject is null)
            return Result.Failure<PACSSubject, AccessControlErrors>(AccessControlErrors.PACSSubjectNotFound);

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (subject.LastConformityCheckedAt.HasValue && subject.LastConformityCheckedAt.Value >= now.Subtract(ManualAuditCooldown))
            return Result.Success<PACSSubject, AccessControlErrors>(subject);

        await EnqueueAsync(subject.IdentityId, subject.AccessControlSystemId, cancellationToken);
        return Result.Success<PACSSubject, AccessControlErrors>(subject);
    }

    public async Task<Result<PACSSubjectConformityAuditEnqueueSummary, AccessControlErrors>> EnqueueByAccessControlSystemIdAsync(Guid accessControlSystemId, CancellationToken cancellationToken = default)
    {
        bool exists = await db.AccessControlSystems.AsNoTracking().AnyAsync(item => item.Id == accessControlSystemId, cancellationToken);
        if (!exists)
            return Result.Failure<PACSSubjectConformityAuditEnqueueSummary, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset cooldownThreshold = now.Subtract(ManualAuditCooldown);

        PACSSubject[] subjects = await db.PACSSubjects
            .AsNoTracking()
            .Where(item => item.AccessControlSystemId == accessControlSystemId)
            .ToArrayAsync(cancellationToken);

        PACSSubject[] eligibleSubjects = subjects
            .Where(item => !item.LastConformityCheckedAt.HasValue || item.LastConformityCheckedAt.Value < cooldownThreshold)
            .ToArray();

        int enqueuedSubjects = 0;
        foreach (PACSSubject subject in eligibleSubjects)
        {
            bool enqueued = await trigger.EnqueueAsync(
                new PACSSubjectConformityAuditWorkItem(tenantContext.TenantId, subject.IdentityId, subject.AccessControlSystemId),
                cancellationToken);
            if (enqueued)
                enqueuedSubjects++;
        }

        return Result.Success<PACSSubjectConformityAuditEnqueueSummary, AccessControlErrors>(
            new PACSSubjectConformityAuditEnqueueSummary(
                accessControlSystemId,
                subjects.Length,
                eligibleSubjects.Length,
                subjects.Length - eligibleSubjects.Length,
                enqueuedSubjects));
    }

    public async Task<IReadOnlyList<PACSSubjectConformityAuditWorkItem>> GetDueWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset staleThreshold = now.Subtract(RoutineAuditInterval);
        DateTimeOffset failedThreshold = now.Subtract(FailedAuditRetryInterval);

        return await db.PACSSubjects
            .IgnoreQueryFilters()
            .Where(item => item.LastConformityCheckedAt == null
                || (item.LastConformityError == null && item.LastConformityCheckedAt <= staleThreshold)
                || (item.LastConformityError != null && item.LastConformityCheckedAt <= failedThreshold))
            .Select(item => new PACSSubjectConformityAuditWorkItem(
                EF.Property<string>(item, TenantDbContext.TenantIdPropertyName),
                item.IdentityId,
                item.AccessControlSystemId))
            .ToListAsync(cancellationToken);
    }

    public async Task AuditAsync(Guid identityId, Guid accessControlSystemId, CancellationToken cancellationToken = default)
    {
        PACSSubject? subject = await db.PACSSubjects.SingleOrDefaultAsync(item => item.IdentityId == identityId && item.AccessControlSystemId == accessControlSystemId, cancellationToken);
        if (subject is null)
            return;

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == accessControlSystemId, cancellationToken);
        if (system?.UnipassConfig is null)
        {
            subject.MarkConformityCheckFailed("Access control system not found.", timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!int.TryParse(subject.NativeSubjectId, out int personId))
        {
            subject.MarkConformityCheckFailed("Native subject id is not a valid Unipass person id.", timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);

        try
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            HashSet<AccessRuleKey> expectedAccess = await GetExpectedAccessAsync(identityId, accessControlSystemId, now, cancellationToken);
            HashSet<int> expectedCards = await GetExpectedCardsAsync(identityId, accessControlSystemId, now, cancellationToken);

            List<UnipassAssignedAccessRule> assignedRules = await api.GetAssignedAccessRules(personId, cancellationToken);
            UnipassPerson? person = await api.GetPerson(personId, cancellationToken);

            AccessRuleKey[] expectedAccessKeys = [.. expectedAccess];
            AccessRuleKey[] actualAccessKeys = [.. assignedRules.Select(AccessRuleKey.FromAssignedRule)];
            HashSet<int> actualCards = [.. person?.Cards.Select(card => card.BadgeNumber) ?? []];
            AccessRuleCount[] missingAccess = CalculateMissing(expectedAccessKeys, actualAccessKeys);
            AccessRuleCount[] unexpectedAccess = CalculateMissing(actualAccessKeys, expectedAccessKeys);

            PACSSubjectConformityAuditLog.AccessRuleComparison(
                logger,
                subject.NativeSubjectId,
                FormatAccessRules(expectedAccessKeys),
                FormatAccessRules(actualAccessKeys),
                FormatAccessDiffs(missingAccess),
                FormatAccessDiffs(unexpectedAccess));

            List<string> issues = [];
            foreach (AccessRuleCount missing in missingAccess)
                issues.Add($"Missing access rule {missing.Key.ToDisplayString()} x{missing.Count}");

            foreach (AccessRuleCount unexpected in unexpectedAccess)
                issues.Add($"Unexpected access rule {unexpected.Key.ToDisplayString()} x{unexpected.Count}");

            foreach (int missing in expectedCards.Except(actualCards))
                issues.Add($"Missing credential {missing}");

            foreach (int unexpected in actualCards.Except(expectedCards))
                issues.Add($"Unexpected credential {unexpected}");

            subject.ApplyConformityCheck(
                issues.Count == 0 ? PACSSubjectConformityStatus.Conform : PACSSubjectConformityStatus.Anomaly,
                issues.Count == 0 ? null : string.Join("; ", issues),
                now);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            subject.MarkConformityCheckFailed(ex.Message, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<HashSet<AccessRuleKey>> GetExpectedAccessAsync(Guid identityId, Guid accessControlSystemId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        PACSProvisioning[] provisionings = await db.PACSProvisionings
            .Where(item => item.IdentityId == identityId)
            .Where(item => item.AccessControlSystemId == accessControlSystemId)
            .Where(item => item.Status == PACSProvisioningStatus.Provisioned || item.Status == PACSProvisioningStatus.PendingRevocation)
            .ToArrayAsync(cancellationToken);

        Guid[] targetIds = provisionings.Select(item => item.AccessLevelTargetId).Distinct().ToArray();
        UnipassAccessLevelTarget[] targets = await db.AccessLevelTargets
            .OfType<UnipassAccessLevelTarget>()
            .Where(item => targetIds.Contains(item.Id))
            .ToArrayAsync(cancellationToken);

        return [.. provisionings.Join(
            targets,
            provisioning => provisioning.AccessLevelTargetId,
            target => target.Id,
            (provisioning, target) => AccessRuleKey.FromProvisioning(target.SiteId, target.AccessRuleId, provisioning.ValidFrom, provisioning.ValidUntil, provisioning.DurationKind))];
    }

    private async Task<HashSet<int>> GetExpectedCardsAsync(Guid identityId, Guid accessControlSystemId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        (Guid AssignmentId, Guid CredentialId)[] provisionedAssignments = await db.CredentialPACSAssignments
            .Where(item => item.AccessControlSystemId == accessControlSystemId)
            .Where(item => item.Status == CredentialPACSAssignmentStatus.Provisioned)
            .Select(item => new ValueTuple<Guid, Guid>(item.Id, item.CredentialId))
            .ToArrayAsync(cancellationToken);
        if (provisionedAssignments.Length == 0)
            return [];

        Guid[] credentialIds = provisionedAssignments.Select(item => item.CredentialId).Distinct().ToArray();
        Credential[] credentials = await credentialDb.Credentials
            .Where(item => credentialIds.Contains(item.Id))
            .Where(item => item.IdentityId == identityId)
            .Where(item => item.Status == CredentialStatus.Issued || item.Status == CredentialStatus.Active)
            .Where(item => item.ValidFrom <= now)
            .Where(item => !item.ValidUntil.HasValue || item.ValidUntil > now)
            .ToArrayAsync(cancellationToken);

        HashSet<int> cards = [];
        foreach (Credential credential in credentials)
        {
            if (int.TryParse(credential.Identifier, out int badgeNumber))
                cards.Add(badgeNumber);
        }

        return cards;
    }

    private static AccessRuleCount[] CalculateMissing(IEnumerable<AccessRuleKey> expected, IEnumerable<AccessRuleKey> actual)
    {
        Dictionary<AccessRuleKey, int> actualCounts = actual
            .GroupBy(item => item)
            .ToDictionary(group => group.Key, group => group.Count());

        return [.. expected
            .GroupBy(item => item)
            .Select(group => new AccessRuleCount(group.Key, group.Count() - actualCounts.GetValueOrDefault(group.Key, 0)))
            .Where(item => item.Count > 0)
            .OrderBy(item => item.Key.SiteId)
            .ThenBy(item => item.Key.RuleId)
            .ThenBy(item => item.Key.Start)
            .ThenBy(item => item.Key.End)];
    }

    private static string FormatAccessRules(IEnumerable<AccessRuleKey> accessRules) =>
        string.Join(", ", accessRules.OrderBy(item => item.SiteId).ThenBy(item => item.RuleId).ThenBy(item => item.Start).ThenBy(item => item.End).Select(item => item.ToDisplayString()));

    private static string FormatAccessDiffs(IEnumerable<AccessRuleCount> accessRules) =>
        string.Join(", ", accessRules.Select(item => $"{item.Key.ToDisplayString()} x{item.Count}"));

    private sealed record AccessRuleKey(int SiteId, int RuleId, DateTimeOffset? Start, DateTimeOffset? End)
    {
        public static AccessRuleKey FromAssignedRule(UnipassAssignedAccessRule rule) =>
            new(rule.SiteId, rule.RuleId, NormalizeTime(rule.StartDate), NormalizeTime(rule.EndDate));

        public static AccessRuleKey FromProvisioning(int siteId, int ruleId, DateTimeOffset validFrom, DateTimeOffset? validUntil, PACSAssignmentDurationKind durationKind) =>
            durationKind == PACSAssignmentDurationKind.Permanent
                ? new(siteId, ruleId, null, null)
                : new(siteId, ruleId, NormalizeTime(validFrom), NormalizeTime(AdjustInclusiveEnd(validUntil)));

        public string ToDisplayString() => $"site={SiteId},rule={RuleId},start={FormatTime(Start)},end={FormatTime(End)}";

        private static string FormatTime(DateTimeOffset? value) => value?.ToString("O", CultureInfo.InvariantCulture) ?? "null";
    }

    private sealed record AccessRuleCount(AccessRuleKey Key, int Count);

    private static DateTimeOffset? AdjustInclusiveEnd(DateTimeOffset? value)
    {
        if (!value.HasValue)
            return null;

        DateTimeOffset adjusted = value.Value.AddMinutes(-1);
        if (adjusted.Hour == 0 && adjusted.Minute == 0)
            adjusted = adjusted.AddMinutes(-1);

        return adjusted;
    }

    private static DateTimeOffset? NormalizeTime(DateTimeOffset? value)
    {
        if (!value.HasValue)
            return null;

        DateTimeOffset local = value.Value.ToLocalTime();
        return new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, local.Offset);
    }
}

internal static partial class PACSSubjectConformityAuditLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "PACS subject {NativeSubjectId} access compare. expected=[{ExpectedAccess}] actual=[{ActualAccess}] expectedExceptActual=[{MissingAccess}] actualExceptExpected=[{UnexpectedAccess}]")]
    public static partial void AccessRuleComparison(
        ILogger logger,
        string nativeSubjectId,
        string expectedAccess,
        string actualAccess,
        string missingAccess,
        string unexpectedAccess);
}
