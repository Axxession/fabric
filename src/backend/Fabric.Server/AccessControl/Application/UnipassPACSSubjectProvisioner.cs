using AccessControl.Unipass.ChangeSets;
using AccessControl.Unipass.Contracts;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class UnipassPACSSubjectProvisioner(
    AccessControlDbContext db,
    UnipassApiFactory apiFactory)
{
    public async Task<Result<bool, string>> ApplyAsync(Guid provisioningId, CancellationToken cancellationToken = default)
    {
        PACSSubjectProvisioning? provisioning = await db.PACSSubjectProvisionings
            .SingleOrDefaultAsync(item => item.Id == provisioningId, cancellationToken);
        if (provisioning is null)
            return Result.Failure<bool, string>("Provisioning not found.");

        PACSSubject? subject = await db.PACSSubjects
            .SingleOrDefaultAsync(item => item.Id == provisioning.PACSSubjectId, cancellationToken);
        if (subject is null)
            return Result.Failure<bool, string>("PACS subject not found.");

        AccessControlSystem? system = await db.AccessControlSystems
            .SingleOrDefaultAsync(item => item.Id == subject.AccessControlSystemId, cancellationToken);
        if (system?.UnipassConfig is null)
            return Result.Failure<bool, string>("Access control system not found.");

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);

        try
        {
            await api.ApplyChangeSet(
                PersonChangeSet.Update(int.Parse(subject.NativeSubjectId))
                    .FirstName(provisioning.DesiredFirstName)
                    .LastName(provisioning.DesiredLastName)
                    .Enabled(TranslateEnabled(provisioning.DesiredState)),
                cancellationToken);

            return Result.Success<bool, string>(true);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool, string>(ex.Message);
        }
    }

    private static bool TranslateEnabled(PACSSubjectState state) =>
        state == PACSSubjectState.Active;
}
