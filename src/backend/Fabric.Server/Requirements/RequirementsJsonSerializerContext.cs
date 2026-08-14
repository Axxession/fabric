using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Requirements;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(EnforcementZoneResponse))]
[JsonSerializable(typeof(RequirementDefinitionResponse))]
[JsonSerializable(typeof(EnforcementZoneLocationResponse))]
[JsonSerializable(typeof(ZoneRequirementPolicyResponse))]
[JsonSerializable(typeof(ContractorJobRequirementPolicyResponse))]
[JsonSerializable(typeof(EnforcementZoneAccessPolicyResponse))]
[JsonSerializable(typeof(RequirementEvidenceResponse))]
[JsonSerializable(typeof(ZoneComplianceResponse))]
[JsonSerializable(typeof(CreateEnforcementZoneRequest))]
[JsonSerializable(typeof(UpdateEnforcementZoneRequest))]
[JsonSerializable(typeof(CreateRequirementDefinitionRequest))]
[JsonSerializable(typeof(UpdateRequirementDefinitionRequest))]
[JsonSerializable(typeof(CreateEnforcementZoneLocationRequest))]
[JsonSerializable(typeof(CreateZoneRequirementPolicyRequest))]
[JsonSerializable(typeof(CreateContractorJobRequirementPolicyRequest))]
[JsonSerializable(typeof(CreateEnforcementZoneAccessPolicyRequest))]
[JsonSerializable(typeof(CreateRequirementEvidenceRequest))]
[JsonSerializable(typeof(UpdateRequirementEvidenceRequest))]
[JsonSerializable(typeof(EvaluateZoneComplianceRequest))]
[JsonSerializable(typeof(Page<EnforcementZoneResponse>))]
[JsonSerializable(typeof(Page<RequirementDefinitionResponse>))]
[JsonSerializable(typeof(Page<RequirementEvidenceResponse>))]
[JsonSerializable(typeof(Page<ZoneComplianceResponse>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(RequirementEvaluatorKind[]))]
[JsonSerializable(typeof(RequirementSubjectKind[]))]
[JsonSerializable(typeof(RequirementEvidenceKind[]))]
[JsonSerializable(typeof(RequirementEvidenceStatus[]))]
[JsonSerializable(typeof(ZoneComplianceStatus[]))]
[JsonSerializable(typeof(RequirementResultStatus[]))]
internal sealed partial class RequirementsJsonSerializerContext : JsonSerializerContext;
