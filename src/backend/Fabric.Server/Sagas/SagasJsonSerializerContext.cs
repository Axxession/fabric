using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Fabric.Server.Sagas.ContractorJobs;
using Fabric.Server.Sagas.EmployeeLifecycle;
using Fabric.Server.Sagas.Kiosk;
using Fabric.Server.Sagas.LearningRequirements;
using Fabric.Server.Sagas.VisitorPreOnboarding;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Sagas;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<AccessGrantProvisioningSaga>))]
[JsonSerializable(typeof(List<OrganizationalUnitPackageRule>))]
[JsonSerializable(typeof(List<PersonaPackageRule>))]
[JsonSerializable(typeof(List<ContractorJobPackageRule>))]
[JsonSerializable(typeof(List<LearningRequirementRule>))]
[JsonSerializable(typeof(List<VisitorPreOnboardingSaga>))]
[JsonSerializable(typeof(List<KioskSaga>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(AccessGrantProvisioningSaga))]
[JsonSerializable(typeof(EmployeeLifecycleAutomationSettingsResponse))]
[JsonSerializable(typeof(OrganizationalUnitPackageRuleResponse))]
[JsonSerializable(typeof(PersonaPackageRuleResponse))]
[JsonSerializable(typeof(ContractorJobPackageRuleResponse))]
[JsonSerializable(typeof(LearningRequirementRuleResponse))]
[JsonSerializable(typeof(UpdateEmployeeLifecycleAutomationSettingsRequest))]
[JsonSerializable(typeof(CreateOrganizationalUnitPackageRuleRequest))]
[JsonSerializable(typeof(CreatePersonaPackageRuleRequest))]
[JsonSerializable(typeof(CreateContractorJobPackageRuleRequest))]
[JsonSerializable(typeof(CreateLearningRequirementRuleRequest))]
[JsonSerializable(typeof(UpdateLearningRequirementRuleRequest))]
[JsonSerializable(typeof(SetRuleEnabledRequest))]
[JsonSerializable(typeof(SetContractorJobPackageRuleEnabledRequest))]
[JsonSerializable(typeof(SetLearningRequirementRuleEnabledRequest))]
[JsonSerializable(typeof(Page<OrganizationalUnitPackageRuleResponse>))]
[JsonSerializable(typeof(Page<PersonaPackageRuleResponse>))]
[JsonSerializable(typeof(Page<ContractorJobPackageRuleResponse>))]
[JsonSerializable(typeof(Page<LearningRequirementRuleResponse>))]
[JsonSerializable(typeof(VisitorPreOnboardingSaga))]
[JsonSerializable(typeof(KioskSaga))]
[JsonSerializable(typeof(VisitorPreOnboardingSagaConfig))]
[JsonSerializable(typeof(VisitorPreOnboardingSagaConfigRequest))]
internal sealed partial class SagasJsonSerializerContext : JsonSerializerContext;
