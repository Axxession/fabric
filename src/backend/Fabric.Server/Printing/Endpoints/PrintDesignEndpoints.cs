using Fabric.Server.Core;
using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Contracts;
using Fabric.Server.Printing.Domain;
using Fabric.Server.Printing.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Printing.Endpoints;

public static class PrintDesignEndpoints
{
    public static IEndpointRouteBuilder MapPrintDesignEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder designs = app.MapGroup("/api/printing/designs");
        designs.MapGet("", ListPrintDesigns).Produces<Page<PrintDesignSummaryResponse>>();
        designs.MapPost("", CreatePrintDesign).Produces<PrintDesignResponse>(StatusCodes.Status201Created);
        designs.MapGet("/{id:guid}", GetPrintDesign).Produces<PrintDesignResponse>().Produces(StatusCodes.Status404NotFound);
        designs.MapPut("/{id:guid}", UpdatePrintDesign).Produces<PrintDesignResponse>().Produces(StatusCodes.Status404NotFound);
        designs.MapDelete("/{id:guid}", DeletePrintDesign).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
        designs.MapPost("/{id:guid}/preview", PreviewPrintDesign)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/printing/preview", PreviewPrintTemplate)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        RouteGroupBuilder media = app.MapGroup("/api/printing/media");
        media.MapGet("/standard", ListStandardMedia).Produces<RenderMediaResponse[]>();

        return app;
    }

    private static async Task<IResult> ListPrintDesigns(
        [AsParameters] ListPrintDesignsRequest request,
        [FromQuery] Guid[]? ids,
        PrintingDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PrintDesign> query = db.PrintDesigns.AsNoTracking();
        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));

        if (request.SurfaceKind is not null)
            query = query.Where(item => item.SurfaceKind == request.SurfaceKind);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            string filter = $"%{request.Name.Trim()}%";
            query = query.Where(item => EF.Functions.ILike(item.Name, filter));
        }

        if (!string.IsNullOrWhiteSpace(request.MediaLabel))
        {
            string filter = $"%{request.MediaLabel.Trim()}%";
            query = query.Where(item => EF.Functions.ILike(item.MediaLabel, filter));
        }

        IPaged<PrintDesign> result = await query
            .OrderBy(item => item.Name)
            .ThenByDescending(item => item.Version)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(item => item.ToSummaryResponse()));
    }

    private static async Task<IResult> CreatePrintDesign(
        [FromBody] CreatePrintDesignRequest request,
        PrintingDbContext db,
        PrintDesignParser parser,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateRenderProfile(request.DefaultRenderProfile, out IResult? renderProfileError))
            return renderProfileError!;

        if (!TryBuildDesignMetadata(request.DesignJson, parser, out PrintTemplate? template, out IResult? errorResult))
            return errorResult!;

        int version = request.Version ?? await GetNextVersionAsync(request.Name, db, cancellationToken);
        bool exists = await db.PrintDesigns.AnyAsync(item => item.Name == request.Name && item.Version == version, cancellationToken);
        if (exists)
            return Results.Problem("Print design version already exists.", statusCode: StatusCodes.Status409Conflict);

        DateTimeOffset now = timeProvider.GetUtcNow();
        RenderProfile? defaultRenderProfile = request.DefaultRenderProfile?.ToDomain();
        PrintDesign design = PrintDesign.Create(
            request.Name,
            version,
            request.Description,
            request.SurfaceKind,
            request.DesignJson,
            template!.Media.Label,
            template.Media.Width,
            template.Media.Height,
            template.Media.Orientation,
            template.Dpi,
            defaultRenderProfile,
            now);

        db.PrintDesigns.Add(design);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/printing/designs/{design.Id}", design.ToResponse());
    }

    private static async Task<IResult> GetPrintDesign(Guid id, PrintingDbContext db, CancellationToken cancellationToken = default)
    {
        PrintDesign? design = await db.PrintDesigns.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return design is null ? Results.NotFound() : Results.Ok(design.ToResponse());
    }

    private static async Task<IResult> UpdatePrintDesign(
        Guid id,
        [FromBody] UpdatePrintDesignRequest request,
        PrintingDbContext db,
        PrintDesignParser parser,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        PrintDesign? design = await db.PrintDesigns.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (design is null)
            return Results.NotFound();

        if (!TryValidateRenderProfile(request.DefaultRenderProfile, out IResult? renderProfileError))
            return renderProfileError!;

        if (!TryBuildDesignMetadata(request.DesignJson, parser, out PrintTemplate? template, out IResult? errorResult))
            return errorResult!;

        bool exists = await db.PrintDesigns.AnyAsync(item => item.Id != id && item.Name == request.Name && item.Version == request.Version, cancellationToken);
        if (exists)
            return Results.Problem("Print design version already exists.", statusCode: StatusCodes.Status409Conflict);

        RenderProfile? defaultRenderProfile = request.DefaultRenderProfile?.ToDomain();

        design.Update(
            request.Name,
            request.Version,
            request.Description,
            request.SurfaceKind,
            request.DesignJson,
            template!.Media.Label,
            template.Media.Width,
            template.Media.Height,
            template.Media.Orientation,
            template.Dpi,
            defaultRenderProfile,
            timeProvider.GetUtcNow());

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(design.ToResponse());
    }

    private static async Task<IResult> DeletePrintDesign(Guid id, PrintingDbContext db, CancellationToken cancellationToken = default)
    {
        PrintDesign? design = await db.PrintDesigns.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (design is null)
            return Results.NotFound();

        db.PrintDesigns.Remove(design);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> PreviewPrintDesign(
        Guid id,
        [FromBody] PreviewPrintDesignRequest request,
        PrintingDbContext db,
        PrintDesignParser parser,
        RenderProfileResolver renderProfileResolver,
        RenderServiceFactory renderServiceFactory,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateRenderProfile(request.RenderProfile, out IResult? renderProfileError, "Render profile"))
            return renderProfileError!;

        PrintDesign? design = await db.PrintDesigns.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (design is null)
            return Results.NotFound();

        if (!TryBuildDesignMetadata(design.DesignJson, parser, out PrintTemplate? template, out IResult? templateError))
            return templateError!;

        RenderProfile effectiveProfile = renderProfileResolver.Resolve(request.RenderProfile?.ToDomain(), design.DefaultRenderProfile);
        return await RenderPreviewAsync(request.Data, template!, effectiveProfile, renderServiceFactory, cancellationToken);
    }

    private static async Task<IResult> PreviewPrintTemplate(
        [FromBody] PreviewPrintTemplateRequest request,
        PrintDesignParser parser,
        RenderProfileResolver renderProfileResolver,
        RenderServiceFactory renderServiceFactory,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateRenderProfile(request.RenderProfile, out IResult? renderProfileError, "Render profile"))
            return renderProfileError!;

        if (!TryBuildDesignMetadata(request.DesignJson, parser, out PrintTemplate? template, out IResult? templateError))
            return templateError!;

        RenderProfile effectiveProfile = renderProfileResolver.Resolve(request.RenderProfile?.ToDomain(), null);
        return await RenderPreviewAsync(request.Data, template!, effectiveProfile, renderServiceFactory, cancellationToken);
    }

    private static IResult ListStandardMedia() => Results.Ok(StandardMedia.All.Select(item => item.ToResponse()).ToArray());

    private static bool TryBuildDesignMetadata(string designJson, PrintDesignParser parser, out PrintTemplate? template, out IResult? errorResult)
    {
        errorResult = null;
        if (!parser.TryParse(designJson, out template, out string? error))
        {
            errorResult = Results.Problem(error ?? "Print design JSON is invalid.", statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        if (template is null)
        {
            errorResult = Results.Problem("Print design JSON is invalid.", statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        if (template.Media.Width <= 0 || template.Media.Height <= 0)
        {
            errorResult = Results.Problem("Print design media must define positive width and height.", statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
    }

    private static bool TryValidateRenderProfile(RenderProfileRequest? profile, out IResult? errorResult, string profileLabel = "Default render profile")
    {
        errorResult = null;

        if (profile is null)
            return true;

        if (profile.Dpi <= 0)
        {
            errorResult = Results.Problem($"{profileLabel} DPI must be positive.", statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        if (profile.Quality is < 0 or > 100)
        {
            errorResult = Results.Problem($"{profileLabel} quality must be between 0 and 100.", statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
    }

    private static async Task<IResult> RenderPreviewAsync(
        IReadOnlyDictionary<string, string> data,
        PrintTemplate template,
        RenderProfile effectiveProfile,
        RenderServiceFactory renderServiceFactory,
        CancellationToken cancellationToken)
    {
        IRenderService renderService = renderServiceFactory.Create(effectiveProfile);
        RenderedDocument document = await renderService.RenderAsync(data, template, cancellationToken);
        return Results.File(document.Content, document.ContentType, document.FileName);
    }

    private static async Task<int> GetNextVersionAsync(string name, PrintingDbContext db, CancellationToken cancellationToken)
    {
        int? current = await db.PrintDesigns.Where(item => item.Name == name).MaxAsync(item => (int?)item.Version, cancellationToken);
        return (current ?? 0) + 1;
    }
}
