using System.Text.Json.Serialization;
using Fabric.Server.Contractors.Contracts;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Contractors;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(CompanyResponse))]
[JsonSerializable(typeof(ContractorResponse))]
[JsonSerializable(typeof(JobTypeResponse))]
[JsonSerializable(typeof(ContractorJobResponse))]
[JsonSerializable(typeof(ContractorJobAssignmentResponse))]
[JsonSerializable(typeof(CreateCompanyRequest))]
[JsonSerializable(typeof(UpdateCompanyRequest))]
[JsonSerializable(typeof(CreateContractorRequest))]
[JsonSerializable(typeof(UpdateContractorRequest))]
[JsonSerializable(typeof(CreateJobTypeRequest))]
[JsonSerializable(typeof(UpdateJobTypeRequest))]
[JsonSerializable(typeof(CreateContractorJobRequest))]
[JsonSerializable(typeof(UpdateContractorJobRequest))]
[JsonSerializable(typeof(CreateContractorJobAssignmentRequest))]
[JsonSerializable(typeof(UpdateContractorJobAssignmentRequest))]
[JsonSerializable(typeof(Page<CompanyResponse>))]
[JsonSerializable(typeof(Page<ContractorResponse>))]
[JsonSerializable(typeof(Page<JobTypeResponse>))]
[JsonSerializable(typeof(Page<ContractorJobResponse>))]
[JsonSerializable(typeof(Page<ContractorJobAssignmentResponse>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ContractorJobStatus[]))]
[JsonSerializable(typeof(ContractorJobAssignmentStatus[]))]
internal sealed partial class ContractorsJsonSerializerContext : JsonSerializerContext;
