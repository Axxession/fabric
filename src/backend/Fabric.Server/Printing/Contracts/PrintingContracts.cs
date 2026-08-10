using Fabric.Server.Core;
using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Printing.Contracts;

public sealed record ListPrintDesignsRequest : BaseListRequest
{
    public string? Name { get; init; }
    public string? MediaLabel { get; init; }
    public PrintSurfaceKind? SurfaceKind { get; init; }
}

public sealed record RenderProfileRequest(RenderTarget Target, int Dpi, string? Background = null, int? Quality = null);

public sealed record CreatePrintDesignRequest(
    string Name,
    int? Version,
    string? Description,
    PrintSurfaceKind SurfaceKind,
    string DesignJson,
    RenderProfileRequest? DefaultRenderProfile = null);

public sealed record UpdatePrintDesignRequest(
    string Name,
    int Version,
    string? Description,
    PrintSurfaceKind SurfaceKind,
    string DesignJson,
    RenderProfileRequest? DefaultRenderProfile = null);

public sealed record PreviewPrintDesignRequest(
    Dictionary<string, string> Data,
    RenderProfileRequest? RenderProfile = null);

public sealed record PreviewPrintTemplateRequest(
    string DesignJson,
    Dictionary<string, string> Data,
    RenderProfileRequest? RenderProfile = null);

public sealed record RenderProfileResponse(RenderTarget Target, int Dpi, string? Background = null, int? Quality = null);

public sealed record RenderMediaResponse(string Label, double Width, double Height, Orientation Orientation);

public sealed record PrintDesignSummaryResponse(
    Guid Id,
    string Name,
    int Version,
    string? Description,
    PrintSurfaceKind SurfaceKind,
    RenderMediaResponse Media,
    int Dpi,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PrintDesignResponse(
    Guid Id,
    string Name,
    int Version,
    string? Description,
    PrintSurfaceKind SurfaceKind,
    string DesignJson,
    RenderMediaResponse Media,
    int Dpi,
    RenderProfileResponse? DefaultRenderProfile,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
