using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Application;

public sealed class RequirementsService(
    RequirementsDbContext db,
    LocationsDbContext locationsDb,
    ContractorsDbContext contractorsDb,
    AccessCatalogDbContext accessCatalogDb,
    AccessGrantComplianceService accessGrantComplianceService,
    TimeProvider timeProvider)
{
    private const long MaxEvidenceFileBytes = 10 * 1024 * 1024;

    public async Task<Result<RequirementDefinition, RequirementDefinitionErrors>> CreateRequirementDefinitionAsync(CreateRequirementDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.RequirementDefinitions.AnyAsync(item => item.Code == request.Code, cancellationToken))
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(RequirementDefinitionErrors.CodeRequired);

        Result<RequirementDefinition, RequirementDefinitionErrors> create = RequirementDefinition.Create(request.Code, request.Name, request.Description, request.EvaluatorKind, request.IsSensitive, timeProvider.GetUtcNow());
        if (create.IsFailure(out RequirementDefinitionErrors error))
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(error);

        create.IsSuccess(out RequirementDefinition definition);
        db.RequirementDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<RequirementDefinition, RequirementDefinitionErrors>(definition);
    }

    public async Task<Result<RequirementDefinition, RequirementDefinitionErrors>> UpdateRequirementDefinitionAsync(Guid id, UpdateRequirementDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        RequirementDefinition? definition = await db.RequirementDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (definition is null)
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(RequirementDefinitionErrors.RequirementDefinitionNotFound);

        Result<RequirementDefinitionErrors> update = definition.Update(request.Code, request.Name, request.Description, request.EvaluatorKind, request.IsSensitive, timeProvider.GetUtcNow());
        if (update.IsFailure(out RequirementDefinitionErrors error))
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<RequirementDefinition, RequirementDefinitionErrors>(definition);
    }

    public async Task<Result<RequirementDefinition, RequirementDefinitionErrors>> DeleteRequirementDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RequirementDefinition? definition = await db.RequirementDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (definition is null)
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(RequirementDefinitionErrors.RequirementDefinitionNotFound);

        bool inUse = await db.LocationRequirementPolicies.AnyAsync(item => item.RequirementDefinitionId == id, cancellationToken)
            || await db.LocationJobRequirementPolicies.AnyAsync(item => item.RequirementDefinitionId == id, cancellationToken)
            || await db.RequirementEvidence.AnyAsync(item => item.RequirementDefinitionId == id, cancellationToken)
            || await accessCatalogDb.GrantRequirements.AnyAsync(item => item.RequirementDefinitionId == id, cancellationToken)
            || await accessCatalogDb.GrantRequirementResults.AnyAsync(item => item.RequirementDefinitionId == id, cancellationToken);
        if (inUse)
            return Result.Failure<RequirementDefinition, RequirementDefinitionErrors>(RequirementDefinitionErrors.RequirementDefinitionInUse);

        db.RequirementDefinitions.Remove(definition);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<RequirementDefinition, RequirementDefinitionErrors>(definition);
    }

    public async Task<Result<LocationRequirementPolicy, RequirementsEvaluationErrors>> CreateLocationRequirementPolicyAsync(CreateLocationRequirementPolicyRequest request, CancellationToken cancellationToken = default)
    {
        if (!await locationsDb.LocationLookups.AnyAsync(item => item.Id == request.LocationId, cancellationToken))
            return Result.Failure<LocationRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.LocationNotFound);

        if (!await db.RequirementDefinitions.AnyAsync(item => item.Id == request.RequirementDefinitionId, cancellationToken))
            return Result.Failure<LocationRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.RequirementDefinitionNotFound);

        LocationRequirementPolicy policy = LocationRequirementPolicy.Create(request.LocationId, request.RequirementDefinitionId, request.SubjectKind, request.IsBlocking, timeProvider.GetUtcNow());
        db.LocationRequirementPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<LocationRequirementPolicy, RequirementsEvaluationErrors>(policy);
    }

    public async Task<Result<LocationRequirementPolicy, RequirementPolicyErrors>> DeleteLocationRequirementPolicyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        LocationRequirementPolicy? policy = await db.LocationRequirementPolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (policy is null)
            return Result.Failure<LocationRequirementPolicy, RequirementPolicyErrors>(RequirementPolicyErrors.LocationRequirementPolicyNotFound);

        db.LocationRequirementPolicies.Remove(policy);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<LocationRequirementPolicy, RequirementPolicyErrors>(policy);
    }

    public async Task<Result<LocationJobRequirementPolicy, RequirementsEvaluationErrors>> CreateLocationJobRequirementPolicyAsync(CreateLocationJobRequirementPolicyRequest request, CancellationToken cancellationToken = default)
    {
        if (!await locationsDb.LocationLookups.AnyAsync(item => item.Id == request.LocationId, cancellationToken))
            return Result.Failure<LocationJobRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.LocationNotFound);

        if (!await db.RequirementDefinitions.AnyAsync(item => item.Id == request.RequirementDefinitionId, cancellationToken))
            return Result.Failure<LocationJobRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.RequirementDefinitionNotFound);

        if (!await contractorsDb.JobTypes.AnyAsync(item => item.Id == request.JobTypeId, cancellationToken))
            return Result.Failure<LocationJobRequirementPolicy, RequirementsEvaluationErrors>(RequirementsEvaluationErrors.JobTypeNotFound);

        LocationJobRequirementPolicy policy = LocationJobRequirementPolicy.Create(request.LocationId, request.JobTypeId, request.RequirementDefinitionId, request.IsBlocking, timeProvider.GetUtcNow());
        db.LocationJobRequirementPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<LocationJobRequirementPolicy, RequirementsEvaluationErrors>(policy);
    }

    public async Task<Result<RequirementEvidence, RequirementEvidenceErrors>> CreateRequirementEvidenceAsync(CreateRequirementEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.RequirementDefinitions.AnyAsync(item => item.Id == request.RequirementDefinitionId, cancellationToken))
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(RequirementEvidenceErrors.SummaryRequired);

        Result<RequirementEvidence, RequirementEvidenceErrors> create = RequirementEvidence.Create(
            request.IdentityId,
            request.RequirementDefinitionId,
            request.EvidenceKind,
            request.Status,
            request.ValidFrom,
            request.ValidUntil,
            request.SourceReference,
            request.Summary,
            request.IsSensitive,
            request.VerifiedAt,
            request.FileName,
            request.Content,
            timeProvider.GetUtcNow());
        if (create.IsFailure(out RequirementEvidenceErrors error))
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(error);

        create.IsSuccess(out RequirementEvidence evidence);
        db.RequirementEvidence.Add(evidence);
        await db.SaveChangesAsync(cancellationToken);
        await accessGrantComplianceService.ReevaluateIdentityRequirementAsync(evidence.IdentityId, evidence.RequirementDefinitionId, cancellationToken);
        return Result.Success<RequirementEvidence, RequirementEvidenceErrors>(evidence);
    }

    public async Task<Result<RequirementEvidence, RequirementEvidenceErrors>> CreateRequirementEvidenceAsync(CreateRequirementEvidenceFormRequest request, CancellationToken cancellationToken = default)
    {
        Result<(string? FileName, byte[]? Content), RequirementEvidenceErrors> file = await ReadFileAsync(request.File, cancellationToken);
        if (file.IsFailure(out RequirementEvidenceErrors error))
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(error);

        file.IsSuccess(out (string? FileName, byte[]? Content) fileValue);
        return await CreateRequirementEvidenceAsync(new CreateRequirementEvidenceRequest(
            request.IdentityId,
            request.RequirementDefinitionId,
            request.EvidenceKind,
            request.Status,
            request.ValidFrom,
            request.ValidUntil,
            request.SourceReference,
            request.Summary,
            request.IsSensitive,
            request.VerifiedAt,
            fileValue.FileName,
            fileValue.Content), cancellationToken);
    }

    public async Task<Result<RequirementEvidence, RequirementEvidenceErrors>> UpdateRequirementEvidenceAsync(Guid id, UpdateRequirementEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        RequirementEvidence? evidence = await db.RequirementEvidence.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (evidence is null)
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(RequirementEvidenceErrors.RequirementEvidenceNotFound);

        Result<RequirementEvidenceErrors> update = evidence.Update(
            request.Status,
            request.ValidFrom,
            request.ValidUntil,
            request.SourceReference,
            request.Summary,
            request.IsSensitive,
            request.VerifiedAt,
            request.FileName,
            request.Content,
            timeProvider.GetUtcNow());
        if (update.IsFailure(out RequirementEvidenceErrors error))
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await accessGrantComplianceService.ReevaluateIdentityRequirementAsync(evidence.IdentityId, evidence.RequirementDefinitionId, cancellationToken);
        return Result.Success<RequirementEvidence, RequirementEvidenceErrors>(evidence);
    }

    public async Task<Result<RequirementEvidence, RequirementEvidenceErrors>> UpdateRequirementEvidenceAsync(Guid id, UpdateRequirementEvidenceFormRequest request, CancellationToken cancellationToken = default)
    {
        RequirementEvidence? existing = await db.RequirementEvidence.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (existing is null)
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(RequirementEvidenceErrors.RequirementEvidenceNotFound);

        Result<(string? FileName, byte[]? Content), RequirementEvidenceErrors> file = await ReadFileAsync(request.File, cancellationToken);
        if (file.IsFailure(out RequirementEvidenceErrors error))
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(error);

        file.IsSuccess(out (string? FileName, byte[]? Content) fileValue);
        return await UpdateRequirementEvidenceAsync(id, new UpdateRequirementEvidenceRequest(
            request.Status,
            request.ValidFrom,
            request.ValidUntil,
            request.SourceReference,
            request.Summary,
            request.IsSensitive,
            request.VerifiedAt,
            fileValue.FileName ?? existing.FileName,
            fileValue.Content ?? existing.Content), cancellationToken);
    }

    public async Task<Result<RequirementEvidence, RequirementEvidenceErrors>> DeleteRequirementEvidenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RequirementEvidence? evidence = await db.RequirementEvidence.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (evidence is null)
            return Result.Failure<RequirementEvidence, RequirementEvidenceErrors>(RequirementEvidenceErrors.RequirementEvidenceNotFound);

        db.RequirementEvidence.Remove(evidence);
        await db.SaveChangesAsync(cancellationToken);
        await accessGrantComplianceService.ReevaluateIdentityRequirementAsync(evidence.IdentityId, evidence.RequirementDefinitionId, cancellationToken);
        return Result.Success<RequirementEvidence, RequirementEvidenceErrors>(evidence);
    }

    private static async Task<Result<(string? FileName, byte[]? Content), RequirementEvidenceErrors>> ReadFileAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Result.Success<(string? FileName, byte[]? Content), RequirementEvidenceErrors>((null, null));

        if (file.Length > MaxEvidenceFileBytes)
            return Result.Failure<(string? FileName, byte[]? Content), RequirementEvidenceErrors>(RequirementEvidenceErrors.FileTooLarge);

        using MemoryStream stream = new();
        await file.CopyToAsync(stream, cancellationToken);
        return Result.Success<(string? FileName, byte[]? Content), RequirementEvidenceErrors>((file.FileName, stream.ToArray()));
    }
}
