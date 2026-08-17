using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Locations.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Locations;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AddBuildingRequest))]
[JsonSerializable(typeof(AddRoomRequest))]
[JsonSerializable(typeof(BuildingResponse[]))]
[JsonSerializable(typeof(CreateSiteRequest))]
[JsonSerializable(typeof(LocationResponse[]))]
[JsonSerializable(typeof(LocationResponse))]
[JsonSerializable(typeof(Page<SiteResponse>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(RoomResponse[]))]
[JsonSerializable(typeof(UpdateBuildingRequest))]
[JsonSerializable(typeof(UpdateRoomRequest))]
[JsonSerializable(typeof(UpdateSiteRequest))]
internal sealed partial class LocationsJsonSerializerContext : JsonSerializerContext;
