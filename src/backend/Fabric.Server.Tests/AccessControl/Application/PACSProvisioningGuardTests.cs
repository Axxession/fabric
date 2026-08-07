using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.CredentialManagement.Domain;
using Fabric.Server.CredentialManagement.Persistence;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.AccessControl.Application;

public sealed class PACSProvisioningGuardTests
{
    [Fact]
    public async Task GetDueProvisioningIdsAsync_ExcludesManuallyBlockedSubjects()
    {
        using TestDbScope scope = CreateScope();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Result<UnipassSystemConfig, AccessControlErrors> configResult = UnipassSystemConfig.Create("https://unipass.local", false, "user", "pass");
        Assert.True(configResult.IsSuccess(out UnipassSystemConfig? config));
        Result<AccessControlSystem, AccessControlErrors> systemResult = AccessControlSystem.CreateUnipass("Main PACS", config!, AnomalyBlockMode.WarnOnly);
        Assert.True(systemResult.IsSuccess(out AccessControlSystem? system));

        AccessItem accessItem = AccessItem.Create("Warehouse", null);
        UnipassAccessLevelTarget target = UnipassAccessLevelTarget.Create(accessItem.Id, system!.Id, null, "Target", 10, 100, "Rule", "Site", Fabric.Server.AccessControl.Domain.ProvisioningTiming.Eager);
        PACSProvisioning provisioning = PACSProvisioning.Create(target.Id, system.Id, Guid.NewGuid(), PACSAssignmentDurationKind.Permanent, now.AddMinutes(-5), null, Fabric.Server.AccessControl.Domain.ProvisioningTiming.Eager, now.AddMinutes(-1));
        PACSSubject subject = PACSSubject.Create(provisioning.IdentityId, system.Id, "1001", PACSSubjectState.Active, "Ada", "Lovelace", null, now);
        Assert.True(subject.BlockProvisioningManually("Cleanup required", now).IsSuccess(out _));

        scope.AccessControlDb.AccessControlSystems.Add(system);
        scope.AccessControlDb.AccessItems.Add(accessItem);
        scope.AccessControlDb.AccessLevelTargets.Add(target);
        scope.AccessControlDb.PACSProvisionings.Add(provisioning);
        scope.AccessControlDb.PACSSubjects.Add(subject);
        await scope.AccessControlDb.SaveChangesAsync();

        PACSProvisioningReconciliationService service = new(
            scope.AccessControlDb,
            scope.TenantContext,
            new PACSProvisioningReconciliationTrigger(),
            new PACSSubjectService(scope.AccessControlDb, scope.IdentitiesDb, null!, TimeProvider.System),
            null!,
            null!,
            TimeProvider.System);

        IReadOnlyList<Guid> dueIds = await service.GetDueProvisioningIdsAsync();

        Assert.Empty(dueIds);
    }

    [Fact]
    public async Task ApplyAsync_WhenSubjectIsManuallyBlocked_LeavesCredentialAssignmentPending()
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
            now,
            null,
            CredentialPurpose.EmployeeCredential,
            CredentialSourceKind.Manual,
            null,
            null,
            "Manual",
            now);
        Assert.True(credentialResult.IsSuccess(out Credential? credential));

        UnipassCredentialTypeTarget target = UnipassCredentialTypeTarget.Create(credentialType.Id, system!.Id, Fabric.Server.AccessControl.Domain.ProvisioningTiming.Eager, now);
        CredentialPACSAssignment assignment = CredentialPACSAssignment.Create(credential!.Id, target.Id, system.Id, now.AddMinutes(-1), now);
        PACSSubject subject = PACSSubject.Create(identityId, system.Id, "1001", PACSSubjectState.Active, "Ada", "Lovelace", null, now);
        Assert.True(subject.BlockProvisioningManually("Legacy record", now).IsSuccess(out _));

        scope.AccessControlDb.AccessControlSystems.Add(system);
        scope.AccessControlDb.CredentialTypeTargets.Add(target);
        scope.AccessControlDb.CredentialPACSAssignments.Add(assignment);
        scope.AccessControlDb.PACSSubjects.Add(subject);
        scope.CredentialDb.CredentialTypes.Add(credentialType);
        scope.CredentialDb.Credentials.Add(credential);
        await scope.AccessControlDb.SaveChangesAsync();
        await scope.CredentialDb.SaveChangesAsync();

        UnipassCredentialPacsProvisioner provisioner = new(
            scope.AccessControlDb,
            scope.CredentialDb,
            new PACSSubjectService(scope.AccessControlDb, scope.IdentitiesDb, null!, TimeProvider.System),
            null!,
            null!,
            TimeProvider.System);

        await provisioner.ApplyAsync(assignment.Id);

        CredentialPACSAssignment? updated = await scope.AccessControlDb.CredentialPACSAssignments.SingleAsync(item => item.Id == assignment.Id);
        Assert.Equal(CredentialPACSAssignmentStatus.Pending, updated.Status);
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
        DbContextOptions<IdentitiesDbContext> identityOptions = new DbContextOptionsBuilder<IdentitiesDbContext>()
            .UseInMemoryDatabase($"identities-{Guid.NewGuid()}")
            .Options;

        return new TestDbScope(
            tenantContext,
            new AccessControlDbContext(accessOptions, tenantContext),
            new CredentialManagementDbContext(credentialOptions, tenantContext),
            new IdentitiesDbContext(identityOptions, tenantContext));
    }

    private sealed record TestDbScope(
        TenantContext TenantContext,
        AccessControlDbContext AccessControlDb,
        CredentialManagementDbContext CredentialDb,
        IdentitiesDbContext IdentitiesDb) : IDisposable
    {
        public void Dispose()
        {
            AccessControlDb.Dispose();
            CredentialDb.Dispose();
            IdentitiesDb.Dispose();
        }
    }
}
