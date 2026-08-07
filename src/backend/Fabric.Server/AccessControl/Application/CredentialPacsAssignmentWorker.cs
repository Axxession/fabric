using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.CredentialManagement.Persistence;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class CredentialPacsAssignmentWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CredentialPacsAssignmentWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(PollInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ProcessDueAssignmentsAsync(stoppingToken);
    }

    private async Task ProcessDueAssignmentsAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        TenantsDbContext tenantsDb = scope.ServiceProvider.GetRequiredService<TenantsDbContext>();
        List<string> tenantIds = await tenantsDb.Tenants.AsNoTracking().Select(item => item.Id).ToListAsync(cancellationToken);

        foreach (string tenantId in tenantIds)
        {
            try
            {
                await using AsyncServiceScope tenantScope = scopeFactory.CreateAsyncScope();
                if (!await SetTenantAsync(tenantScope.ServiceProvider, tenantId, cancellationToken))
                    continue;

                AccessControlDbContext db = tenantScope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
                CredentialPACSAssignmentService service = tenantScope.ServiceProvider.GetRequiredService<CredentialPACSAssignmentService>();
                UnipassCredentialPacsProvisioner provisioner = tenantScope.ServiceProvider.GetRequiredService<UnipassCredentialPacsProvisioner>();
                PACSSubjectService subjectService = tenantScope.ServiceProvider.GetRequiredService<PACSSubjectService>();
                DateTimeOffset now = timeProvider.GetUtcNow();

                Guid[] dueIds = await db.CredentialPACSAssignments
                    .AsNoTracking()
                    .Where(item => item.Status == CredentialPACSAssignmentStatus.Pending || item.Status == CredentialPACSAssignmentStatus.FailedRetryable)
                    .Where(item => item.ScheduledFor <= now)
                    .OrderBy(item => item.ScheduledFor)
                    .Select(item => item.Id)
                    .ToArrayAsync(cancellationToken);

                foreach (Guid assignmentId in dueIds)
                {
                    CredentialPACSAssignment? assignment = await db.CredentialPACSAssignments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
                    if (assignment is null)
                        continue;

                    Guid? identityId = await tenantScope.ServiceProvider.GetRequiredService<CredentialManagementDbContext>().Credentials
                        .Where(item => item.Id == assignment.CredentialId)
                        .Select(item => (Guid?)item.IdentityId)
                        .SingleOrDefaultAsync(cancellationToken);
                    if (!identityId.HasValue)
                        continue;

                    (PACSSubjectProvisioningBlockStatus status, _) = await subjectService.GetProvisioningBlockAsync(identityId.Value, assignment.AccessControlSystemId, cancellationToken);
                    if (status != PACSSubjectProvisioningBlockStatus.ProvisioningAllowed)
                        continue;

                    await provisioner.ApplyAsync(assignmentId, cancellationToken);
                }

                IReadOnlyList<Guid> revocationIds = await service.GetProvisionedAssignmentIdsNeedingRevocationAsync(cancellationToken);
                foreach (Guid assignmentId in revocationIds)
                    await provisioner.RevokeAsync(assignmentId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing credential PACS assignments for tenant {TenantId}", tenantId);
            }
        }
    }

    private static async Task<bool> SetTenantAsync(IServiceProvider serviceProvider, string tenantId, CancellationToken cancellationToken)
    {
        ITenantStore tenantStore = serviceProvider.GetRequiredService<ITenantStore>();
        TenantInfo? tenant = await tenantStore.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
            return false;

        ITenantContextAccessor tenantContext = serviceProvider.GetRequiredService<ITenantContextAccessor>();
        tenantContext.SetTenant(tenant);
        return true;
    }
}
