using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.CredentialManagement.Contracts;
using Fabric.Server.CredentialManagement.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.CredentialManagement;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ListCredentialTypesRequest))]
[JsonSerializable(typeof(ListCredentialsRequest))]
[JsonSerializable(typeof(CreateCredentialTypeRequest))]
[JsonSerializable(typeof(UpdateCredentialTypeRequest))]
[JsonSerializable(typeof(CreateCredentialRangeRequest))]
[JsonSerializable(typeof(UpdateCredentialRangeRequest))]
[JsonSerializable(typeof(IssueCredentialRequest))]
[JsonSerializable(typeof(Page<CredentialTypeResponse>))]
[JsonSerializable(typeof(Page<CredentialResponse>))]
[JsonSerializable(typeof(CredentialTypeResponse))]
[JsonSerializable(typeof(CredentialRangeResponse))]
[JsonSerializable(typeof(CredentialRangeResponse[]))]
[JsonSerializable(typeof(CredentialResponse))]
[JsonSerializable(typeof(CredentialTechnology))]
[JsonSerializable(typeof(CredentialAllocationMode))]
[JsonSerializable(typeof(CredentialRecyclePolicy))]
[JsonSerializable(typeof(CredentialIdentifierPaddingDirection))]
[JsonSerializable(typeof(CredentialTypeStatus))]
[JsonSerializable(typeof(CredentialCapacityState))]
[JsonSerializable(typeof(CredentialDurationKind))]
[JsonSerializable(typeof(CredentialStatus))]
[JsonSerializable(typeof(CredentialPurpose))]
[JsonSerializable(typeof(CredentialSourceKind))]
[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class CredentialManagementJsonSerializerContext : JsonSerializerContext;
