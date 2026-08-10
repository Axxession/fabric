using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Contracts;
using Fabric.Server.Printing.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Printing;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(PrintSurfaceKind))]
[JsonSerializable(typeof(Orientation))]
[JsonSerializable(typeof(RenderTarget))]
[JsonSerializable(typeof(ListPrintDesignsRequest))]
[JsonSerializable(typeof(CreatePrintDesignRequest))]
[JsonSerializable(typeof(UpdatePrintDesignRequest))]
[JsonSerializable(typeof(PreviewPrintDesignRequest))]
[JsonSerializable(typeof(PreviewPrintTemplateRequest))]
[JsonSerializable(typeof(RenderProfileRequest))]
[JsonSerializable(typeof(RenderProfileResponse))]
[JsonSerializable(typeof(RenderProfile))]
[JsonSerializable(typeof(RenderMediaResponse))]
[JsonSerializable(typeof(RenderMedia))]
[JsonSerializable(typeof(PrintDesignSummaryResponse))]
[JsonSerializable(typeof(PrintDesignResponse))]
[JsonSerializable(typeof(Page<PrintDesignSummaryResponse>))]
[JsonSerializable(typeof(RenderMediaResponse[]))]
[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class PrintingJsonSerializerContext : JsonSerializerContext;
