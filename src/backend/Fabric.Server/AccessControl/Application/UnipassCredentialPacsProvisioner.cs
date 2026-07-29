using AccessControl.Unipass.ChangeSets;
using AccessControl.Unipass.Contracts;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.CredentialManagement.Domain;
using Fabric.Server.CredentialManagement.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class UnipassCredentialPacsProvisioner(
    AccessControlDbContext db,
    CredentialManagementDbContext credentialDb,
    PACSSubjectService subjectService,
    UnipassApiFactory apiFactory,
    TimeProvider timeProvider)
{
    public async Task ApplyAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        CredentialPACSAssignment? assignment = await db.CredentialPACSAssignments.SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
        if (assignment is null)
            return;

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == assignment.AccessControlSystemId, cancellationToken);
        CredentialTypeTarget? target = await db.CredentialTypeTargets.SingleOrDefaultAsync(item => item.Id == assignment.CredentialTypeTargetId, cancellationToken);
        Credential? credential = await credentialDb.Credentials.SingleOrDefaultAsync(item => item.Id == assignment.CredentialId, cancellationToken);
        CredentialType? credentialType = credential is null ? null : await credentialDb.CredentialTypes.SingleOrDefaultAsync(item => item.Id == credential.CredentialTypeId, cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();

        if (credential is not null && IsProvisioningNoLongerNeeded(credential, now))
        {
            assignment.MarkRevoked(now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (system?.UnipassConfig is null || target is null || credential is null || credentialType is null)
        {
            assignment.MarkTerminalFailure(CredentialPacsFailureReasons.ProviderConfigurationMissing, "Credential, target, or system not found.", now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (credentialType.Technology == CredentialTechnology.LicensePlate)
        {
            assignment.MarkTerminalFailure(CredentialPacsFailureReasons.CredentialTechnologyNotSupported, "License plate provisioning is not supported for Unipass yet.", now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!int.TryParse(credential.Identifier, out int badgeNumber))
        {
            assignment.MarkTerminalFailure(CredentialPacsFailureReasons.IdentifierNotNumericForUnipass, "Unipass only supports numeric credential identifiers for card provisioning.", now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        Result<PACSSubject, AccessControlErrors> subjectResult;
        try
        {
            subjectResult = await subjectService.GetOrCreateAsync(credential.IdentityId, system, cancellationToken);
        }
        catch (Exception ex)
        {
            assignment.MarkRetryableFailure(CredentialPacsFailureReasons.ProviderUnavailable, ex.Message, GetRetryAt(assignment.AttemptCount + 1, now), now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (subjectResult.IsFailure(out AccessControlErrors error))
        {
            assignment.MarkRetryableFailure(CredentialPacsFailureReasons.PacsSubjectCreationFailed, error.ToString(), GetRetryAt(assignment.AttemptCount + 1, now), now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        subjectResult.IsSuccess(out PACSSubject subject);

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);
        try
        {
            UnipassOperationResponse response = await api.ApplyChangeSet(CardChangeSet.Assign(int.Parse(subject.NativeSubjectId), badgeNumber), cancellationToken);
            if (!response.Success)
            {
                assignment.MarkRetryableFailure(CredentialPacsFailureReasons.ProviderRejected, response.Message ?? "Unipass card assignment failed.", GetRetryAt(assignment.AttemptCount + 1, now), now);
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            assignment.MarkProvisioned(response.Id ?? string.Empty, now);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            assignment.MarkRetryableFailure(CredentialPacsFailureReasons.ProviderUnavailable, ex.Message, GetRetryAt(assignment.AttemptCount + 1, now), now);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        CredentialPACSAssignment? assignment = await db.CredentialPACSAssignments.SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
        if (assignment is null)
            return;

        if (assignment.Status == CredentialPACSAssignmentStatus.Revoked)
            return;

        if (assignment.Status != CredentialPACSAssignmentStatus.Provisioned || string.IsNullOrWhiteSpace(assignment.NativeAssignmentId))
        {
            assignment.MarkRevoked(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == assignment.AccessControlSystemId, cancellationToken);
        Credential? credential = await credentialDb.Credentials.SingleOrDefaultAsync(item => item.Id == assignment.CredentialId, cancellationToken);
        if (system?.UnipassConfig is null || credential is null)
        {
            assignment.MarkRetryableFailure(CredentialPacsFailureReasons.ProviderConfigurationMissing, "Credential or system not found for revocation.", GetRetryAt(assignment.AttemptCount + 1, timeProvider.GetUtcNow()), timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        Result<PACSSubject, AccessControlErrors> subjectResult;
        try
        {
            subjectResult = await subjectService.GetOrCreateAsync(credential.IdentityId, system, cancellationToken);
        }
        catch (Exception ex)
        {
            assignment.MarkRetryableFailure(CredentialPacsFailureReasons.ProviderUnavailable, ex.Message, GetRetryAt(assignment.AttemptCount + 1, timeProvider.GetUtcNow()), timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (subjectResult.IsFailure(out AccessControlErrors error))
        {
            assignment.MarkRetryableFailure(CredentialPacsFailureReasons.PacsSubjectCreationFailed, error.ToString(), GetRetryAt(assignment.AttemptCount + 1, timeProvider.GetUtcNow()), timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        subjectResult.IsSuccess(out PACSSubject subject);

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);
        try
        {
            await api.ApplyChangeSet(CardChangeSet.Revoke(int.Parse(subject.NativeSubjectId), int.Parse(assignment.NativeAssignmentId)), cancellationToken);
            assignment.MarkRevoked(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            assignment.MarkRetryableFailure(CredentialPacsFailureReasons.ProviderUnavailable, ex.Message, GetRetryAt(assignment.AttemptCount + 1, timeProvider.GetUtcNow()), timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static DateTimeOffset GetRetryAt(int attemptCount, DateTimeOffset now)
    {
        TimeSpan delay = attemptCount switch
        {
            <= 1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromMinutes(15)
        };

        return now.Add(delay);
    }

    private static bool IsProvisioningNoLongerNeeded(Credential credential, DateTimeOffset now) =>
        credential.Status is CredentialStatus.Revoked or CredentialStatus.Archived or CredentialStatus.Expired
        || (credential.ValidUntil.HasValue && credential.ValidUntil.Value <= now);
}
