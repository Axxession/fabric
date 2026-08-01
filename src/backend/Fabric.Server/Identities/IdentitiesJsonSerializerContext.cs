using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Identities.Contracts;
using Fabric.Server.Identities.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Identities;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ListIdentitiesRequest))]
[JsonSerializable(typeof(CreateIdentityRequest))]
[JsonSerializable(typeof(UpdateIdentityProfileRequest))]
[JsonSerializable(typeof(Page<IdentityResponse>))]
[JsonSerializable(typeof(IdentityResponse))]
[JsonSerializable(typeof(IdentityAffiliationSummaryResponse))]
[JsonSerializable(typeof(IdentityStatus))]
[JsonSerializable(typeof(IdentityStatus?))]
[JsonSerializable(typeof(IdentityAffiliationType))]
[JsonSerializable(typeof(IdentityAffiliationType?))]
[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class IdentitiesJsonSerializerContext : JsonSerializerContext;
