using Fabric.Server.Contractors.Contracts;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Identities.Domain;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Contractors.Application;

public sealed class ContractorsService(
    ContractorsDbContext db,
    LocationsDbContext locationsDb,
    IdentityService identityService,
    TimeProvider timeProvider)
{
    public async Task<Result<Company, CompanyErrors>> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.Companies.AnyAsync(item => item.Code == request.Code, cancellationToken))
            return Result.Failure<Company, CompanyErrors>(CompanyErrors.CompanyCodeAlreadyExists);

        if (!string.IsNullOrWhiteSpace(request.CompanyNumber)
            && await db.Companies.AnyAsync(item => item.CompanyNumber == request.CompanyNumber, cancellationToken))
            return Result.Failure<Company, CompanyErrors>(CompanyErrors.CompanyNumberAlreadyExists);

        Result<Company, CompanyErrors> result = Company.Create(request.Code, request.Name, request.CompanyNumber, timeProvider.GetUtcNow());
        if (result.IsFailure(out CompanyErrors error))
            return Result.Failure<Company, CompanyErrors>(error);

        result.IsSuccess(out Company company);
        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Company, CompanyErrors>(company);
    }

    public async Task<Result<Company, CompanyErrors>> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        Company? company = await db.Companies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (company is null)
            return Result.Failure<Company, CompanyErrors>(CompanyErrors.CompanyNotFound);

        if (await db.Companies.AnyAsync(item => item.Id != id && item.Code == request.Code, cancellationToken))
            return Result.Failure<Company, CompanyErrors>(CompanyErrors.CompanyCodeAlreadyExists);

        if (!string.IsNullOrWhiteSpace(request.CompanyNumber)
            && await db.Companies.AnyAsync(item => item.Id != id && item.CompanyNumber == request.CompanyNumber, cancellationToken))
            return Result.Failure<Company, CompanyErrors>(CompanyErrors.CompanyNumberAlreadyExists);

        Result<CompanyErrors> update = company.Update(request.Code, request.Name, request.CompanyNumber, timeProvider.GetUtcNow());
        if (update.IsFailure(out CompanyErrors error))
            return Result.Failure<Company, CompanyErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Company, CompanyErrors>(company);
    }

    public async Task<Result<Company, CompanyErrors>> SetCompanyActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        Company? company = await db.Companies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (company is null)
            return Result.Failure<Company, CompanyErrors>(CompanyErrors.CompanyNotFound);

        Result<CompanyErrors> result = isActive
            ? company.Activate(timeProvider.GetUtcNow())
            : company.Deactivate(timeProvider.GetUtcNow());
        if (result.IsFailure(out CompanyErrors error))
            return Result.Failure<Company, CompanyErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Company, CompanyErrors>(company);
    }

    public async Task<Result<JobType, JobTypeErrors>> CreateJobTypeAsync(CreateJobTypeRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.JobTypes.AnyAsync(item => item.Code == request.Code, cancellationToken))
            return Result.Failure<JobType, JobTypeErrors>(JobTypeErrors.JobTypeCodeAlreadyExists);

        Result<JobType, JobTypeErrors> result = JobType.Create(request.Code, request.Name, request.Description, timeProvider.GetUtcNow());
        if (result.IsFailure(out JobTypeErrors error))
            return Result.Failure<JobType, JobTypeErrors>(error);

        result.IsSuccess(out JobType jobType);
        db.JobTypes.Add(jobType);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<JobType, JobTypeErrors>(jobType);
    }

    public async Task<Result<JobType, JobTypeErrors>> UpdateJobTypeAsync(Guid id, UpdateJobTypeRequest request, CancellationToken cancellationToken = default)
    {
        JobType? jobType = await db.JobTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (jobType is null)
            return Result.Failure<JobType, JobTypeErrors>(JobTypeErrors.JobTypeNotFound);

        if (await db.JobTypes.AnyAsync(item => item.Id != id && item.Code == request.Code, cancellationToken))
            return Result.Failure<JobType, JobTypeErrors>(JobTypeErrors.JobTypeCodeAlreadyExists);

        Result<JobTypeErrors> update = jobType.Update(request.Code, request.Name, request.Description, timeProvider.GetUtcNow());
        if (update.IsFailure(out JobTypeErrors error))
            return Result.Failure<JobType, JobTypeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<JobType, JobTypeErrors>(jobType);
    }

    public async Task<Result<JobType, JobTypeErrors>> SetJobTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        JobType? jobType = await db.JobTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (jobType is null)
            return Result.Failure<JobType, JobTypeErrors>(JobTypeErrors.JobTypeNotFound);

        Result<JobTypeErrors> result = isActive
            ? jobType.Activate(timeProvider.GetUtcNow())
            : jobType.Deactivate(timeProvider.GetUtcNow());
        if (result.IsFailure(out JobTypeErrors error))
            return Result.Failure<JobType, JobTypeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<JobType, JobTypeErrors>(jobType);
    }

    public async Task<Result<Contractor, ContractorErrors>> CreateContractorAsync(CreateContractorRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.Companies.AnyAsync(item => item.Id == request.CompanyId, cancellationToken))
            return Result.Failure<Contractor, ContractorErrors>(ContractorErrors.CompanyNotFound);

        if (request.IdentityId.HasValue)
        {
            Identity? identity = await identityService.GetIdentityAsync(request.IdentityId.Value, cancellationToken);
            if (identity is null)
                return Result.Failure<Contractor, ContractorErrors>(ContractorErrors.IdentityNotFound);
        }

        Result<Contractor, ContractorErrors> result = Contractor.Create(
            request.CompanyId,
            request.FirstName,
            request.LastName,
            request.Email,
            timeProvider.GetUtcNow());
        if (result.IsFailure(out ContractorErrors error))
            return Result.Failure<Contractor, ContractorErrors>(error);

        result.IsSuccess(out Contractor contractor);
        db.Contractors.Add(contractor);
        await db.SaveChangesAsync(cancellationToken);

        if (request.IdentityId.HasValue)
        {
            Result<Identity, IdentityErrors> link = await identityService.LinkContractorAsync(request.IdentityId.Value, contractor.Id, cancellationToken);
            if (link.IsFailure(out IdentityErrors identityError))
            {
                ContractorErrors mappedError = identityError switch
                {
                    IdentityErrors.IdentityNotFound => ContractorErrors.IdentityNotFound,
                    IdentityErrors.ContractorAlreadyLinkedToDifferentIdentity => ContractorErrors.ContractorAlreadyLinkedToDifferentIdentity,
                    _ => ContractorErrors.IdentityNotFound,
                };
                return Result.Failure<Contractor, ContractorErrors>(mappedError);
            }
        }

        return Result.Success<Contractor, ContractorErrors>(contractor);
    }

    public async Task<Result<Contractor, ContractorErrors>> UpdateContractorAsync(Guid id, UpdateContractorRequest request, CancellationToken cancellationToken = default)
    {
        Contractor? contractor = await db.Contractors.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (contractor is null)
            return Result.Failure<Contractor, ContractorErrors>(ContractorErrors.ContractorNotFound);

        if (!await db.Companies.AnyAsync(item => item.Id == request.CompanyId, cancellationToken))
            return Result.Failure<Contractor, ContractorErrors>(ContractorErrors.CompanyNotFound);

        Result<ContractorErrors> update = contractor.Update(request.CompanyId, request.FirstName, request.LastName, request.Email, timeProvider.GetUtcNow());
        if (update.IsFailure(out ContractorErrors error))
            return Result.Failure<Contractor, ContractorErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Contractor, ContractorErrors>(contractor);
    }

    public async Task<Result<Contractor, ContractorErrors>> SetContractorArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default)
    {
        Contractor? contractor = await db.Contractors.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (contractor is null)
            return Result.Failure<Contractor, ContractorErrors>(ContractorErrors.ContractorNotFound);

        Result<ContractorErrors> result = isArchived
            ? contractor.Archive(timeProvider.GetUtcNow())
            : contractor.Unarchive(timeProvider.GetUtcNow());
        if (result.IsFailure(out ContractorErrors error))
            return Result.Failure<Contractor, ContractorErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Contractor, ContractorErrors>(contractor);
    }

    public async Task<Result<ContractorJob, ContractorJobErrors>> CreateContractorJobAsync(CreateContractorJobRequest request, Guid createdByIdentityId, CancellationToken cancellationToken = default)
    {
        Result<ContractorJobErrors> dependencies = await ValidateJobDependenciesAsync(request.CompanyId, request.JobTypeId, request.LocationId, cancellationToken);
        if (dependencies.IsFailure(out ContractorJobErrors dependencyError))
            return Result.Failure<ContractorJob, ContractorJobErrors>(dependencyError);

        Result<ContractorJob, ContractorJobErrors> result = ContractorJob.Create(
            request.CompanyId,
            request.JobTypeId,
            request.LocationId,
            createdByIdentityId,
            request.Name,
            request.Description,
            request.PlannedStart,
            request.PlannedEnd,
            timeProvider.GetUtcNow());
        if (result.IsFailure(out ContractorJobErrors error))
            return Result.Failure<ContractorJob, ContractorJobErrors>(error);

        result.IsSuccess(out ContractorJob job);
        db.ContractorJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ContractorJob, ContractorJobErrors>(job);
    }

    public async Task<Result<ContractorJob, ContractorJobErrors>> UpdateContractorJobAsync(Guid id, UpdateContractorJobRequest request, CancellationToken cancellationToken = default)
    {
        ContractorJob? job = await db.ContractorJobs.Include(item => item.Assignments).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (job is null)
            return Result.Failure<ContractorJob, ContractorJobErrors>(ContractorJobErrors.ContractorJobNotFound);

        Result<ContractorJobErrors> dependencies = await ValidateJobDependenciesAsync(request.CompanyId, request.JobTypeId, request.LocationId, cancellationToken);
        if (dependencies.IsFailure(out ContractorJobErrors dependencyError))
            return Result.Failure<ContractorJob, ContractorJobErrors>(dependencyError);

        Result<ContractorJobErrors> update = job.Update(
            request.CompanyId,
            request.JobTypeId,
            request.LocationId,
            request.Name,
            request.Description,
            request.PlannedStart,
            request.PlannedEnd,
            timeProvider.GetUtcNow());
        if (update.IsFailure(out ContractorJobErrors error))
            return Result.Failure<ContractorJob, ContractorJobErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ContractorJob, ContractorJobErrors>(job);
    }

    public async Task<Result<ContractorJob, ContractorJobErrors>> ActivateContractorJobAsync(Guid id, CancellationToken cancellationToken = default) =>
        await UpdateJobStateAsync(id, job => job.Activate(timeProvider.GetUtcNow()), cancellationToken);

    public async Task<Result<ContractorJob, ContractorJobErrors>> CompleteContractorJobAsync(Guid id, CancellationToken cancellationToken = default) =>
        await UpdateJobStateAsync(id, job => job.Complete(timeProvider.GetUtcNow()), cancellationToken);

    public async Task<Result<ContractorJob, ContractorJobErrors>> CancelContractorJobAsync(Guid id, CancellationToken cancellationToken = default) =>
        await UpdateJobStateAsync(id, job => job.Cancel(timeProvider.GetUtcNow()), cancellationToken);

    public async Task<Result<ContractorJobAssignment, ContractorJobErrors>> CreateAssignmentAsync(Guid contractorJobId, CreateContractorJobAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        ContractorJob? job = await db.ContractorJobs.Include(item => item.Assignments).SingleOrDefaultAsync(item => item.Id == contractorJobId, cancellationToken);
        if (job is null)
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(ContractorJobErrors.ContractorJobNotFound);

        Contractor? contractor = await db.Contractors.SingleOrDefaultAsync(item => item.Id == request.ContractorId, cancellationToken);
        if (contractor is null)
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(ContractorJobErrors.ContractorNotFound);

        if (contractor.CompanyId != job.CompanyId)
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(ContractorJobErrors.ContractorCompanyMismatch);

        Result<ContractorJobAssignment, ContractorJobErrors> result = job.AddAssignment(
            request.ContractorId,
            request.AssignedFrom,
            request.AssignedUntil,
            timeProvider.GetUtcNow());
        if (result.IsFailure(out ContractorJobErrors error))
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<Result<ContractorJobAssignment, ContractorJobErrors>> UpdateAssignmentAsync(Guid contractorJobId, Guid assignmentId, UpdateContractorJobAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        ContractorJob? job = await db.ContractorJobs.Include(item => item.Assignments).SingleOrDefaultAsync(item => item.Id == contractorJobId, cancellationToken);
        if (job is null)
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(ContractorJobErrors.ContractorJobNotFound);

        Result<ContractorJobErrors> result = job.UpdateAssignment(assignmentId, request.AssignedFrom, request.AssignedUntil, timeProvider.GetUtcNow());
        if (result.IsFailure(out ContractorJobErrors error))
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(error);

        ContractorJobAssignment assignment = job.Assignments.Single(item => item.Id == assignmentId);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ContractorJobAssignment, ContractorJobErrors>(assignment);
    }

    public async Task<Result<ContractorJobAssignment, ContractorJobErrors>> ActivateAssignmentAsync(Guid contractorJobId, Guid assignmentId, CancellationToken cancellationToken = default) =>
        await UpdateAssignmentStateAsync(contractorJobId, assignmentId, (job, id) => job.ActivateAssignment(id, timeProvider.GetUtcNow()), cancellationToken);

    public async Task<Result<ContractorJobAssignment, ContractorJobErrors>> CompleteAssignmentAsync(Guid contractorJobId, Guid assignmentId, CancellationToken cancellationToken = default) =>
        await UpdateAssignmentStateAsync(contractorJobId, assignmentId, (job, id) => job.CompleteAssignment(id, timeProvider.GetUtcNow()), cancellationToken);

    public async Task<Result<ContractorJobAssignment, ContractorJobErrors>> CancelAssignmentAsync(Guid contractorJobId, Guid assignmentId, CancellationToken cancellationToken = default) =>
        await UpdateAssignmentStateAsync(contractorJobId, assignmentId, (job, id) => job.CancelAssignment(id, timeProvider.GetUtcNow()), cancellationToken);

    private async Task<Result<ContractorJobErrors>> ValidateJobDependenciesAsync(Guid companyId, Guid jobTypeId, Guid locationId, CancellationToken cancellationToken)
    {
        if (!await db.Companies.AnyAsync(item => item.Id == companyId, cancellationToken))
            return Result.Failure(ContractorJobErrors.CompanyNotFound);

        if (!await db.JobTypes.AnyAsync(item => item.Id == jobTypeId, cancellationToken))
            return Result.Failure(ContractorJobErrors.JobTypeNotFound);

        if (!await locationsDb.LocationLookups.AnyAsync(item => item.Id == locationId, cancellationToken))
            return Result.Failure(ContractorJobErrors.LocationNotFound);

        return Result.Success<ContractorJobErrors>();
    }

    private async Task<Result<ContractorJob, ContractorJobErrors>> UpdateJobStateAsync(
        Guid id,
        Func<ContractorJob, Result<ContractorJobErrors>> action,
        CancellationToken cancellationToken)
    {
        ContractorJob? job = await db.ContractorJobs.Include(item => item.Assignments).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (job is null)
            return Result.Failure<ContractorJob, ContractorJobErrors>(ContractorJobErrors.ContractorJobNotFound);

        Result<ContractorJobErrors> result = action(job);
        if (result.IsFailure(out ContractorJobErrors error))
            return Result.Failure<ContractorJob, ContractorJobErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ContractorJob, ContractorJobErrors>(job);
    }

    private async Task<Result<ContractorJobAssignment, ContractorJobErrors>> UpdateAssignmentStateAsync(
        Guid contractorJobId,
        Guid assignmentId,
        Func<ContractorJob, Guid, Result<ContractorJobErrors>> action,
        CancellationToken cancellationToken)
    {
        ContractorJob? job = await db.ContractorJobs.Include(item => item.Assignments).SingleOrDefaultAsync(item => item.Id == contractorJobId, cancellationToken);
        if (job is null)
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(ContractorJobErrors.ContractorJobNotFound);

        Result<ContractorJobErrors> result = action(job, assignmentId);
        if (result.IsFailure(out ContractorJobErrors error))
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(error);

        ContractorJobAssignment assignment = job.Assignments.Single(item => item.Id == assignmentId);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ContractorJobAssignment, ContractorJobErrors>(assignment);
    }
}
