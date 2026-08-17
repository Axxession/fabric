using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Requirements;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(RequirementDefinitionResponse))]
[JsonSerializable(typeof(LocationRequirementPolicyResponse))]
[JsonSerializable(typeof(LocationJobRequirementPolicyResponse))]
[JsonSerializable(typeof(LocationAttachedRequirementResponse[]))]
[JsonSerializable(typeof(LocationJobAttachedRequirementResponse[]))]
[JsonSerializable(typeof(RequirementEvidenceResponse))]
[JsonSerializable(typeof(CreateRequirementDefinitionRequest))]
[JsonSerializable(typeof(UpdateRequirementDefinitionRequest))]
[JsonSerializable(typeof(CreateLocationRequirementPolicyRequest))]
[JsonSerializable(typeof(ListLocationJobRequirementPoliciesRequest))]
[JsonSerializable(typeof(CreateLocationJobRequirementPolicyRequest))]
[JsonSerializable(typeof(UpdateLocationJobRequirementPolicyRequest))]
[JsonSerializable(typeof(CreateRequirementEvidenceFormRequest))]
[JsonSerializable(typeof(UpdateRequirementEvidenceFormRequest))]
[JsonSerializable(typeof(IFormFile))]
[JsonSerializable(typeof(Page<RequirementDefinitionResponse>))]
[JsonSerializable(typeof(Page<RequirementEvidenceResponse>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(RequirementEvaluatorKind[]))]
[JsonSerializable(typeof(RequirementSubjectKind[]))]
[JsonSerializable(typeof(RequirementEvidenceKind[]))]
[JsonSerializable(typeof(RequirementEvidenceStatus[]))]
[JsonSerializable(typeof(RequirementResultStatus[]))]
internal sealed partial class RequirementsJsonSerializerContext : JsonSerializerContext;
