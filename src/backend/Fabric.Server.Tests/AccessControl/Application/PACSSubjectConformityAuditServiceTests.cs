using System.Reflection;
using AccessControl.Unipass.ChangeSets;
using AccessControl.Unipass.Contracts;
using AccessControl.Unipass.Entities;
using AccessControl.Unipass.Filters;
using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.CredentialManagement.Domain;
using Fabric.Server.CredentialManagement.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fabric.Server.Tests.AccessControl.Application;

public sealed class PACSSubjectConformityAuditServiceTests
{
    [Fact]
    public async Task GetExpectedCardsAsync_IncludesIssuedCredentials()
    {
        using TestDbScope scope = CreateScope();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Result<UnipassSystemConfig, AccessControlErrors> configResult = UnipassSystemConfig.Create("https://unipass.local", false, "user", "pass");
        Assert.True(configResult.IsSuccess(out UnipassSystemConfig? config));
        Result<AccessControlSystem, AccessControlErrors> systemResult = AccessControlSystem.CreateUnipass("Main PACS", config!, AnomalyBlockMode.WarnOnly);
        Assert.True(systemResult.IsSuccess(out AccessControlSystem? system));

        Result<CredentialType, CredentialManagementErrors> typeResult = CredentialType.Create(
            "Employee Desfire",
            CredentialTechnology.Desfire,
            CredentialAllocationMode.Provided,
            CredentialRecyclePolicy.NeverReuse,
            TimeSpan.Zero,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            now);
        Assert.True(typeResult.IsSuccess(out CredentialType? credentialType));

        Guid identityId = Guid.NewGuid();
        Result<Credential, CredentialManagementErrors> credentialResult = Credential.Create(
            credentialType!.Id,
            "123456",
            identityId,
            CredentialDurationKind.Permanent,
            now.AddHours(2),
            null,
            CredentialPurpose.EmployeeCredential,
            CredentialSourceKind.Manual,
            null,
            null,
            "Future credential",
            now);
        Assert.True(credentialResult.IsSuccess(out Credential? credential));

        UnipassCredentialTypeTarget target = UnipassCredentialTypeTarget.Create(credentialType.Id, system!.Id, Fabric.Server.AccessControl.Domain.ProvisioningTiming.Eager, now);
        CredentialPACSAssignment assignment = CredentialPACSAssignment.Create(credential!.Id, target.Id, system.Id, now, now);
        assignment.MarkProvisioned("99", now);

        scope.AccessControlDb.AccessControlSystems.Add(system);
        scope.AccessControlDb.CredentialTypeTargets.Add(target);
        scope.AccessControlDb.CredentialPACSAssignments.Add(assignment);
        scope.CredentialDb.CredentialTypes.Add(credentialType);
        scope.CredentialDb.Credentials.Add(credential);
        await scope.AccessControlDb.SaveChangesAsync();
        await scope.CredentialDb.SaveChangesAsync();

        PACSSubjectConformityAuditService service = new(
            scope.AccessControlDb,
            scope.CredentialDb,
            scope.TenantContext,
            new PACSSubjectConformityAuditTrigger(),
            null!,
            TimeProvider.System,
            NullLogger<PACSSubjectConformityAuditService>.Instance);

        MethodInfo method = typeof(PACSSubjectConformityAuditService)
            .GetMethod("GetExpectedCardsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Task<HashSet<int>> task = (Task<HashSet<int>>)method.Invoke(service, [identityId, system.Id, now.AddHours(3), CancellationToken.None])!;
        HashSet<int> cards = await task;

        Assert.Contains(123456, cards);
    }

    [Fact]
    public async Task EnqueueByAccessControlSystemIdAsync_ReturnsCooldownAndEnqueueCounts()
    {
        using TestDbScope scope = CreateScope();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Result<UnipassSystemConfig, AccessControlErrors> configResult = UnipassSystemConfig.Create("https://unipass.local", false, "user", "pass");
        Assert.True(configResult.IsSuccess(out UnipassSystemConfig? config));
        Result<AccessControlSystem, AccessControlErrors> systemResult = AccessControlSystem.CreateUnipass("Main PACS", config!, AnomalyBlockMode.WarnOnly);
        Assert.True(systemResult.IsSuccess(out AccessControlSystem? system));

        PACSSubject eligibleSubject = PACSSubject.Create(Guid.NewGuid(), system!.Id, "1001", PACSSubjectState.Active, "Ada", "Lovelace", null, now);
        PACSSubject recentSubject = PACSSubject.Create(Guid.NewGuid(), system.Id, "1002", PACSSubjectState.Active, "Grace", "Hopper", null, now);
        recentSubject.ApplyConformityCheck(PACSSubjectConformityStatus.Conform, null, now);

        scope.AccessControlDb.AccessControlSystems.Add(system);
        scope.AccessControlDb.PACSSubjects.AddRange(eligibleSubject, recentSubject);
        await scope.AccessControlDb.SaveChangesAsync();

        PACSSubjectConformityAuditTrigger trigger = new();
        PACSSubjectConformityAuditService service = new(
            scope.AccessControlDb,
            scope.CredentialDb,
            scope.TenantContext,
            trigger,
            null!,
            TimeProvider.System,
            NullLogger<PACSSubjectConformityAuditService>.Instance);

        Result<PACSSubjectConformityAuditService.PACSSubjectConformityAuditEnqueueSummary, AccessControlErrors> result = await service.EnqueueByAccessControlSystemIdAsync(system.Id);

        Assert.True(result.IsSuccess(out PACSSubjectConformityAuditService.PACSSubjectConformityAuditEnqueueSummary? summary));
        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalSubjects);
        Assert.Equal(1, summary.EligibleSubjects);
        Assert.Equal(1, summary.RecentlyAuditedSubjects);
        Assert.Equal(1, summary.EnqueuedSubjects);
    }

    [Fact]
    public async Task AuditAsync_WhenProvisioningIsPendingRevocation_StillExpectsUnipassRow()
    {
        using TestDbScope scope = CreateScope();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Result<UnipassSystemConfig, AccessControlErrors> configResult = UnipassSystemConfig.Create("https://unipass.local", false, "user", "pass");
        Assert.True(configResult.IsSuccess(out UnipassSystemConfig? config));
        Result<AccessControlSystem, AccessControlErrors> systemResult = AccessControlSystem.CreateUnipass("Main PACS", config!, AnomalyBlockMode.WarnOnly);
        Assert.True(systemResult.IsSuccess(out AccessControlSystem? system));

        AccessItem accessItem = AccessItem.Create("Warehouse", null);
        UnipassAccessLevelTarget target = UnipassAccessLevelTarget.Create(accessItem.Id, system!.Id, null, "Target", 100, 10, "Rule", "Site", Fabric.Server.AccessControl.Domain.ProvisioningTiming.Eager);
        Guid identityId = Guid.NewGuid();
        PACSSubject subject = PACSSubject.Create(identityId, system.Id, "93", PACSSubjectState.Active, "Ada", "Lovelace", null, now);
        PACSProvisioning provisioning = PACSProvisioning.Create(target.Id, system.Id, identityId, PACSAssignmentDurationKind.Permanent, now.AddDays(-10), null, Fabric.Server.AccessControl.Domain.ProvisioningTiming.Eager, now.AddDays(-10));
        provisioning.MarkProvisioned("1", now.AddDays(-10));
        provisioning.MarkPendingRevocation(now);

        scope.AccessControlDb.AccessControlSystems.Add(system);
        scope.AccessControlDb.AccessItems.Add(accessItem);
        scope.AccessControlDb.AccessLevelTargets.Add(target);
        scope.AccessControlDb.PACSSubjects.Add(subject);
        scope.AccessControlDb.PACSProvisionings.Add(provisioning);
        await scope.AccessControlDb.SaveChangesAsync();

        PACSSubjectConformityAuditService service = CreateAuditService(scope, new TestUnipassApi(
            [new UnipassAssignedAccessRule { PersonId = 93, SiteId = 10, RuleId = 100 }],
            new UnipassPerson { Id = 93 }));

        await service.AuditAsync(identityId, system.Id);

        PACSSubject updated = await scope.AccessControlDb.PACSSubjects.SingleAsync(item => item.Id == subject.Id);
        Assert.Equal(PACSSubjectConformityStatus.Conform, updated.ConformityStatus);
        Assert.Null(updated.ConformityDetails);
    }

    [Fact]
    public async Task AuditAsync_WhenSameSiteAndRuleHaveDifferentTimes_MarksAnomaly()
    {
        using TestDbScope scope = CreateScope();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Result<UnipassSystemConfig, AccessControlErrors> configResult = UnipassSystemConfig.Create("https://unipass.local", false, "user", "pass");
        Assert.True(configResult.IsSuccess(out UnipassSystemConfig? config));
        Result<AccessControlSystem, AccessControlErrors> systemResult = AccessControlSystem.CreateUnipass("Main PACS", config!, AnomalyBlockMode.WarnOnly);
        Assert.True(systemResult.IsSuccess(out AccessControlSystem? system));

        AccessItem accessItem = AccessItem.Create("Warehouse", null);
        UnipassAccessLevelTarget target = UnipassAccessLevelTarget.Create(accessItem.Id, system!.Id, null, "Target", 100, 10, "Rule", "Site", Fabric.Server.AccessControl.Domain.ProvisioningTiming.Eager);
        Guid identityId = Guid.NewGuid();
        PACSSubject subject = PACSSubject.Create(identityId, system.Id, "93", PACSSubjectState.Active, "Ada", "Lovelace", null, now);
        DateTimeOffset validFrom = new(2026, 8, 7, 8, 45, 0, TimeSpan.Zero);
        DateTimeOffset validUntil = new(2026, 8, 7, 17, 0, 0, TimeSpan.Zero);
        PACSProvisioning provisioning = PACSProvisioning.Create(target.Id, system.Id, identityId, PACSAssignmentDurationKind.Temporary, validFrom, validUntil, Fabric.Server.AccessControl.Domain.ProvisioningTiming.Eager, validFrom);
        provisioning.MarkProvisioned("1", now);

        scope.AccessControlDb.AccessControlSystems.Add(system);
        scope.AccessControlDb.AccessItems.Add(accessItem);
        scope.AccessControlDb.AccessLevelTargets.Add(target);
        scope.AccessControlDb.PACSSubjects.Add(subject);
        scope.AccessControlDb.PACSProvisionings.Add(provisioning);
        await scope.AccessControlDb.SaveChangesAsync();

        PACSSubjectConformityAuditService service = CreateAuditService(scope, new TestUnipassApi(
            [new UnipassAssignedAccessRule { PersonId = 93, SiteId = 10, RuleId = 100, StartDate = validFrom.AddMinutes(30), EndDate = validUntil }],
            new UnipassPerson { Id = 93 }));

        await service.AuditAsync(identityId, system.Id);

        PACSSubject updated = await scope.AccessControlDb.PACSSubjects.SingleAsync(item => item.Id == subject.Id);
        Assert.Equal(PACSSubjectConformityStatus.Anomaly, updated.ConformityStatus);
        Assert.NotNull(updated.ConformityDetails);
        Assert.Contains("Missing access rule", updated.ConformityDetails);
        Assert.Contains("Unexpected access rule", updated.ConformityDetails);
    }

    private static PACSSubjectConformityAuditService CreateAuditService(TestDbScope scope, IUnipassApi api) =>
        new(
            scope.AccessControlDb,
            scope.CredentialDb,
            scope.TenantContext,
            new PACSSubjectConformityAuditTrigger(),
            new TestUnipassApiFactory(api),
            TimeProvider.System,
            NullLogger<PACSSubjectConformityAuditService>.Instance);

    private static TestDbScope CreateScope()
    {
        TenantContext tenantContext = new();
        DbContextOptions<AccessControlDbContext> accessOptions = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase($"access-control-{Guid.NewGuid()}")
            .Options;
        DbContextOptions<CredentialManagementDbContext> credentialOptions = new DbContextOptionsBuilder<CredentialManagementDbContext>()
            .UseInMemoryDatabase($"credential-management-{Guid.NewGuid()}")
            .Options;

        return new TestDbScope(
            tenantContext,
            new AccessControlDbContext(accessOptions, tenantContext),
            new CredentialManagementDbContext(credentialOptions, tenantContext));
    }

    private sealed record TestDbScope(
        TenantContext TenantContext,
        AccessControlDbContext AccessControlDb,
        CredentialManagementDbContext CredentialDb) : IDisposable
    {
        public void Dispose()
        {
            AccessControlDb.Dispose();
            CredentialDb.Dispose();
        }
    }

    private sealed class TestUnipassApiFactory(IUnipassApi api) : UnipassApiFactory
    {
        public override IUnipassApi Create(UnipassSystemConfig config) => api;
    }

    private sealed class TestUnipassApi(IReadOnlyList<UnipassAssignedAccessRule> assignedRules, UnipassPerson person) : IUnipassApi
    {
        public void Dispose() { }

        public Task<List<UnipassSite>> GetSites(SitesFilter? sitesFilter = null, CancellationToken ct = default) => Task.FromResult<List<UnipassSite>>([]);

        public Task<List<AccessRuleDto>> GetAccessRules(AccessRuleFilter? accessRuleFilter = null, CancellationToken ct = default) => Task.FromResult<List<AccessRuleDto>>([]);

        public Task<List<UnipassAssignedAccessRule>> GetAssignedAccessRules(int personId, CancellationToken ct = default) => Task.FromResult(assignedRules.Where(item => item.PersonId == personId).ToList());

        public Task<UnipassPerson?> GetPerson(int personId, CancellationToken ct = default) => Task.FromResult<UnipassPerson?>(person.Id == personId ? person : null);

        public Task<List<UnipassPerson>> GetPersons(PersonFilter? personFilter, CancellationToken ct = default) => Task.FromResult<List<UnipassPerson>>([person]);

        public Task<UnipassOperationResponse> ApplyChangeSet(IChangeSet changeSet, CancellationToken ct = default) => Task.FromResult(new UnipassOperationResponse { Id = "1", Success = true });
    }
}
