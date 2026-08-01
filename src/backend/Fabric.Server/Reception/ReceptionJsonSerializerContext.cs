using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Reception.Contracts;
using Fabric.Server.Reception.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Reception;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ArrivalType?))]
[JsonSerializable(typeof(ListArrivalsRequest))]
[JsonSerializable(typeof(OnboardArrivalRequest))]
[JsonSerializable(typeof(OnboardArrivalFromKioskRequest))]
[JsonSerializable(typeof(OnboardingStatus?))]
[JsonSerializable(typeof(IdentityVerificationMethod?))]
[JsonSerializable(typeof(ReceptionActorType))]
[JsonSerializable(typeof(ReceptionAccessPolicyTrigger))]
[JsonSerializable(typeof(CreateReceptionKioskRequest))]
[JsonSerializable(typeof(UpdateReceptionKioskRequest))]
[JsonSerializable(typeof(ReceptionKioskResponse))]
[JsonSerializable(typeof(ReceptionKioskKeyResponse))]
[JsonSerializable(typeof(CreateReceptionDeskWorkstationRequest))]
[JsonSerializable(typeof(UpdateReceptionDeskWorkstationRequest))]
[JsonSerializable(typeof(ReceptionDeskWorkstationResponse))]
[JsonSerializable(typeof(ReceptionDeskWorkstationKeyResponse))]
[JsonSerializable(typeof(ReceptionKioskExpectedArrivalResponse))]
[JsonSerializable(typeof(ReceptionKioskOnboardingRequirementsResponse))]
[JsonSerializable(typeof(ReceptionKioskVisitorDetailsResponse))]
[JsonSerializable(typeof(ReceptionKioskVisitDetailsResponse))]
[JsonSerializable(typeof(ReceptionKioskContractorDetailsResponse))]
[JsonSerializable(typeof(CreateAccessRuleAssignmentRequest))]
[JsonSerializable(typeof(UpdateAccessRuleAssignmentRequest))]
[JsonSerializable(typeof(AccessRuleAssignmentResponse))]
[JsonSerializable(typeof(Page<ReceptionKioskResponse>))]
[JsonSerializable(typeof(Page<ReceptionDeskWorkstationResponse>))]
[JsonSerializable(typeof(Page<AccessRuleAssignmentResponse>))]
[JsonSerializable(typeof(Page<ArrivalResponse>))]
[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class ReceptionJsonSerializerContext : JsonSerializerContext;
