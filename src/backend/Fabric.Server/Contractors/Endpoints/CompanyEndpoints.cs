using Fabric.Server.Contractors.Application;
using Fabric.Server.Contractors.Contracts;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Contractors.Endpoints;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder companies = app.MapGroup("/api/contractors/companies");

        companies.MapGet("", ListCompanies)
            .WithSummary("List contractor companies")
            .Produces<Page<CompanyResponse>>();
        companies.MapGet("/{id:guid}", GetCompany)
            .WithSummary("Get contractor company")
            .Produces<CompanyResponse>()
            .Produces(StatusCodes.Status404NotFound);
        companies.MapPost("", CreateCompany)
            .WithSummary("Create contractor company")
            .Produces<CompanyResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        companies.MapPut("/{id:guid}", UpdateCompany)
            .WithSummary("Update contractor company")
            .Produces<CompanyResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        companies.MapPost("/{id:guid}/activate", ActivateCompany)
            .WithSummary("Activate contractor company")
            .Produces<CompanyResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        companies.MapPost("/{id:guid}/deactivate", DeactivateCompany)
            .WithSummary("Deactivate contractor company")
            .Produces<CompanyResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListCompanies(
        [AsParameters] ListCompaniesRequest request,
        [FromQuery] Guid[]? ids,
        ContractorsDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Company> query = db.Companies.AsNoTracking();

        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));

        if (request.IsActive.HasValue)
            query = query.Where(item => item.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Code, filter)
                || EF.Functions.ILike(item.Name, filter)
                || item.CompanyNumber != null && EF.Functions.ILike(item.CompanyNumber, filter));
        }

        IPaged<Company> result = await query.OrderBy(item => item.Name).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetCompany(Guid id, ContractorsDbContext db, CancellationToken cancellationToken = default)
    {
        Company? company = await db.Companies.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return company is null ? Results.NotFound() : Results.Ok(company.ToResponse());
    }

    private static async Task<IResult> CreateCompany([FromBody] CreateCompanyRequest request, ContractorsService service, CancellationToken cancellationToken = default)
    {
        Result<Company, CompanyErrors> result = await service.CreateCompanyAsync(request, cancellationToken);
        return result.Match<IResult>(
            company => Results.Created($"/api/contractors/companies/{company.Id}", company.ToResponse()),
            error => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyRequest request, ContractorsService service, CancellationToken cancellationToken = default)
    {
        Result<Company, CompanyErrors> result = await service.UpdateCompanyAsync(id, request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ActivateCompany(Guid id, ContractorsService service, CancellationToken cancellationToken = default)
    {
        Result<Company, CompanyErrors> result = await service.SetCompanyActiveAsync(id, true, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> DeactivateCompany(Guid id, ContractorsService service, CancellationToken cancellationToken = default)
    {
        Result<Company, CompanyErrors> result = await service.SetCompanyActiveAsync(id, false, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(CompanyErrors error) =>
        error switch
        {
            CompanyErrors.CompanyNotFound => Problem(StatusCodes.Status404NotFound, "Company not found."),
            CompanyErrors.CompanyCodeAlreadyExists => Problem(StatusCodes.Status409Conflict, "Company code already exists."),
            CompanyErrors.CompanyNumberAlreadyExists => Problem(StatusCodes.Status409Conflict, "Company number already exists."),
            CompanyErrors.CompanyAlreadyActive => Problem(StatusCodes.Status409Conflict, "Company is already active."),
            CompanyErrors.CompanyAlreadyInactive => Problem(StatusCodes.Status409Conflict, "Company is already inactive."),
            CompanyErrors.CodeRequired => Problem(StatusCodes.Status400BadRequest, "Company code is required."),
            CompanyErrors.NameRequired => Problem(StatusCodes.Status400BadRequest, "Company name is required."),
            _ => Problem(StatusCodes.Status400BadRequest, "Company request is invalid."),
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
