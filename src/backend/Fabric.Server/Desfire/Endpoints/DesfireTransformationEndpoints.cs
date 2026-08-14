using System.Text.Json;
using Fabric.Server.Core;
using Fabric.Server.Desfire.Application;
using Fabric.Server.Desfire.Contracts;
using Fabric.Server.Desfire.Domain;
using Fabric.Server.Desfire.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Desfire.Endpoints;

public static class DesfireTransformationEndpoints
{
    public static IEndpointRouteBuilder MapDesfireTransformationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder transformations = app.MapGroup("/api/desfire/transformations");
        transformations.MapGet("", ListTransformations).Produces<Page<TransformationResponse>>();
        transformations.MapPost("", CreateTransformation).Produces<TransformationResponse>(StatusCodes.Status201Created);
        transformations.MapGet("/{id:guid}", GetTransformation).Produces<TransformationResponse>().Produces(StatusCodes.Status404NotFound);
        transformations.MapPut("/{id:guid}", UpdateTransformation).Produces<TransformationResponse>().Produces(StatusCodes.Status404NotFound);
        transformations.MapDelete("/{id:guid}", DeleteTransformation).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
        transformations.MapGet("/{id:guid}/plan", PreviewTransformationPlan).Produces<TransformationPlanResponse>().Produces(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> ListTransformations([AsParameters] BaseListRequest request, DesfireDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<Transformation> result = await db.Transformations.AsNoTracking().OrderBy(transformation => transformation.Name).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(transformation => transformation.ToResponse()));
    }

    private static async Task<IResult> CreateTransformation([FromBody] CreateTransformationRequest request, DesfireDbContext db, DesfireTransformationPlanner planner, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        IResult? sourceFailure = ValidateSource(request.FromChipDesignName, request.FromBlank);
        if (sourceFailure is not null)
            return sourceFailure;

        TransformationPlanMetadata metadata;
        try
        {
            metadata = await planner.GetMetadataAsync(request.FromChipDesignName, request.FromBlank, request.ToChipDesignName, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }

        (IResult? failure, TransformationVariableConfigRequest[] variableConfigs) = await NormalizeVariableConfigsAsync(request.Variables, metadata, db, cancellationToken);
        if (failure is not null)
            return failure;

        DateTimeOffset now = timeProvider.GetUtcNow();
        Transformation transformation = Transformation.Create(
            request.Name,
            request.FromChipDesignName,
            request.FromBlank,
            request.ToChipDesignName,
            JsonSerializer.Serialize(metadata.RequiredVariables, DesfireJson.Options),
            JsonSerializer.Serialize(metadata.RequiredKeyGroups, DesfireJson.Options),
            JsonSerializer.Serialize(variableConfigs, DesfireJson.Options),
            now);

        db.Transformations.Add(transformation);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/desfire/transformations/{transformation.Id}", transformation.ToResponse());
    }

    private static async Task<IResult> GetTransformation(Guid id, DesfireDbContext db, CancellationToken cancellationToken = default)
    {
        Transformation? transformation = await db.Transformations.AsNoTracking().SingleOrDefaultAsync(transformation => transformation.Id == id, cancellationToken);
        return transformation is null ? Results.NotFound() : Results.Ok(transformation.ToResponse());
    }

    private static async Task<IResult> UpdateTransformation(Guid id, [FromBody] UpdateTransformationRequest request, DesfireDbContext db, DesfireTransformationPlanner planner, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        Transformation? transformation = await db.Transformations.SingleOrDefaultAsync(transformation => transformation.Id == id, cancellationToken);
        if (transformation is null)
            return Results.NotFound();

        IResult? sourceFailure = ValidateSource(request.FromChipDesignName, request.FromBlank);
        if (sourceFailure is not null)
            return sourceFailure;

        TransformationPlanMetadata metadata;
        try
        {
            metadata = await planner.GetMetadataAsync(request.FromChipDesignName, request.FromBlank, request.ToChipDesignName, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }

        (IResult? failure, TransformationVariableConfigRequest[] variableConfigs) = await NormalizeVariableConfigsAsync(request.Variables, metadata, db, cancellationToken);
        if (failure is not null)
            return failure;

        transformation.Update(
            request.Name,
            request.FromChipDesignName,
            request.FromBlank,
            request.ToChipDesignName,
            JsonSerializer.Serialize(metadata.RequiredVariables, DesfireJson.Options),
            JsonSerializer.Serialize(metadata.RequiredKeyGroups, DesfireJson.Options),
            JsonSerializer.Serialize(variableConfigs, DesfireJson.Options),
            timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(transformation.ToResponse());
    }

    private static async Task<IResult> DeleteTransformation(Guid id, DesfireDbContext db, CancellationToken cancellationToken = default)
    {
        Transformation? transformation = await db.Transformations.SingleOrDefaultAsync(transformation => transformation.Id == id, cancellationToken);
        if (transformation is null)
            return Results.NotFound();

        bool referenced = await db.BadgeBatches.AnyAsync(batch => batch.TransformationId == id, cancellationToken) || await db.BadgeJobs.AnyAsync(run => run.TransformationId == id, cancellationToken);
        if (referenced)
            return Results.Problem("Cannot delete a transformation referenced by encoding history.", statusCode: StatusCodes.Status409Conflict);

        db.Transformations.Remove(transformation);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> PreviewTransformationPlan(Guid id, DesfireDbContext db, DesfireTransformationPlanner planner, CancellationToken cancellationToken = default)
    {
        Transformation? transformation = await db.Transformations.AsNoTracking().SingleOrDefaultAsync(transformation => transformation.Id == id, cancellationToken);
        if (transformation is null)
            return Results.NotFound();

        TransformationPlanMetadata metadata = await planner.GetMetadataAsync(transformation.FromChipDesignName, transformation.FromBlank, transformation.ToChipDesignName, cancellationToken);
        return Results.Ok(new TransformationPlanResponse(metadata.RequiredVariables, metadata.RequiredKeyGroups, metadata.Errors, metadata.OperationCount, metadata.Operations));
    }

    private static async Task<(IResult? Failure, TransformationVariableConfigRequest[] Configs)> NormalizeVariableConfigsAsync(IReadOnlyList<TransformationVariableConfigRequest> requested, TransformationPlanMetadata metadata, DesfireDbContext db, CancellationToken cancellationToken)
    {
        Dictionary<string, TransformationVariableConfigRequest> requestedByName = requested
            .Where(config => !string.IsNullOrWhiteSpace(config.Name))
            .GroupBy(config => config.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        List<TransformationVariableConfigRequest> configs = [];
        foreach (string variable in metadata.RequiredVariables)
        {
            TransformationVariableConfigRequest config = NormalizeVariableConfig(variable, requestedByName.GetValueOrDefault(variable), metadata);
            IResult? validation = await ValidateSystemProviderReferenceAsync(config, db, cancellationToken);
            if (validation is not null)
                return (validation, []);

            configs.Add(config);
        }

        return (null, [.. configs]);
    }

    private static TransformationVariableConfigRequest NormalizeVariableConfig(string variable, TransformationVariableConfigRequest? requested, TransformationPlanMetadata metadata)
    {
        VariableFormatRequest format = requested?.Format ?? metadata.VariableFormats.GetValueOrDefault(variable) ?? new VariableFormatRequest(DesfireVariableFormatKind.Hex);
        if (requested is null)
            return new TransformationVariableConfigRequest(variable, TransformationVariableKind.UserProvided, format, Field: variable);

        return requested.Kind switch
        {
            TransformationVariableKind.UserProvided => new TransformationVariableConfigRequest(variable, TransformationVariableKind.UserProvided, format, Field: string.IsNullOrWhiteSpace(requested.Field) ? variable : requested.Field),
            TransformationVariableKind.SystemProvided => NormalizeSystemVariableConfig(variable, requested, format),
            _ => new TransformationVariableConfigRequest(variable, TransformationVariableKind.UserProvided, format, Field: variable)
        };
    }

    private static TransformationVariableConfigRequest NormalizeSystemVariableConfig(string variable, TransformationVariableConfigRequest requested, VariableFormatRequest format)
    {
        if (requested.SystemProviderId is null)
            return new TransformationVariableConfigRequest(variable, TransformationVariableKind.SystemProvided, format);

        return new TransformationVariableConfigRequest(variable, TransformationVariableKind.SystemProvided, format, SystemProviderId: requested.SystemProviderId);
    }

    private static async Task<IResult?> ValidateSystemProviderReferenceAsync(TransformationVariableConfigRequest config, DesfireDbContext db, CancellationToken cancellationToken)
    {
        if (config.Kind != TransformationVariableKind.SystemProvided)
            return null;

        if (config.SystemProviderId is null)
            return Results.Problem($"System-provided variable '{config.Name}' must reference a named system provider.", statusCode: StatusCodes.Status400BadRequest);

        bool exists = await db.SystemProviders.AnyAsync(provider => provider.Id == config.SystemProviderId.Value, cancellationToken);
        return exists ? null : Results.Problem($"System provider '{config.SystemProviderId}' was not found.", statusCode: StatusCodes.Status400BadRequest);
    }

    private static IResult? ValidateSource(string? fromChipDesignName, bool fromBlank)
    {
        if (!string.IsNullOrWhiteSpace(fromChipDesignName) == fromBlank)
            return Results.Problem("Transformation source must be exactly one chip design name or blank chip.", statusCode: StatusCodes.Status400BadRequest);

        return null;
    }
}
