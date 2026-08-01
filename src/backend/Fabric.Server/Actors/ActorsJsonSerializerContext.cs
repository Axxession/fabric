using System.Text.Json.Serialization;
using Fabric.Server.Actors.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Actors;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(CurrentActorResponse))]
[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class ActorsJsonSerializerContext : JsonSerializerContext;
