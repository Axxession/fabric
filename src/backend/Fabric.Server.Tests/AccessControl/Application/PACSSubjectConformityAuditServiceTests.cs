using System.Reflection;
using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.CredentialManagement.Domain;
using Fabric.Server.CredentialManagement.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

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
            TimeProvider.System);

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
            TimeProvider.System);

        Result<PACSSubjectConformityAuditService.PACSSubjectConformityAuditEnqueueSummary, AccessControlErrors> result = await service.EnqueueByAccessControlSystemIdAsync(system.Id);

        Assert.True(result.IsSuccess(out PACSSubjectConformityAuditService.PACSSubjectConformityAuditEnqueueSummary? summary));
        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalSubjects);
        Assert.Equal(1, summary.EligibleSubjects);
        Assert.Equal(1, summary.RecentlyAuditedSubjects);
        Assert.Equal(1, summary.EnqueuedSubjects);
    }

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
}
