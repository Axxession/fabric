using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Visitors.Contracts;
using Fabric.Server.Visitors.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Visitors;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ConfirmInvitationRequest))]
[JsonSerializable(typeof(CreateVisitRequest))]
[JsonSerializable(typeof(HostSettingsResponse))]
[JsonSerializable(typeof(HostResponse))]
[JsonSerializable(typeof(InviteVisitRequest))]
[JsonSerializable(typeof(List<VisitStatus>))]
[JsonSerializable(typeof(Page<HostResponse>))]
[JsonSerializable(typeof(Page<VisitResponse>))]
[JsonSerializable(typeof(Page<VisitorResponse>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(RelocateVisitRequest))]
[JsonSerializable(typeof(RescheduleVisitRequest))]
[JsonSerializable(typeof(UpdateHostSettingsRequest))]
[JsonSerializable(typeof(UpdateVisitSummaryRequest))]
[JsonSerializable(typeof(VisitInvitationResponse))]
[JsonSerializable(typeof(VisitConfirmationResponse))]
[JsonSerializable(typeof(VisitConfirmationVisitorResponse))]
[JsonSerializable(typeof(VisitResponse))]
[JsonSerializable(typeof(VisitorResponse))]
[JsonSerializable(typeof(VisitStatus[]))]
internal sealed partial class VisitorsJsonSerializerContext : JsonSerializerContext;
