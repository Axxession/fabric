using System.Text.Json.Serialization;
using Fabric.Server.Core;
using Fabric.Server.Employees.Contracts;
using Fabric.Server.Employees.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Employees;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ListEmployeesRequest))]
[JsonSerializable(typeof(CreateEmployeeRequest))]
[JsonSerializable(typeof(UpdateEmployeeRequest))]
[JsonSerializable(typeof(EmployeeWorkLocationRequest))]
[JsonSerializable(typeof(ReplaceEmployeeWorkLocationsRequest))]
[JsonSerializable(typeof(ReplaceEmployeePersonasRequest))]
[JsonSerializable(typeof(CreateEmployeePeriodRequest))]
[JsonSerializable(typeof(UpdateEmployeePeriodRequest))]
[JsonSerializable(typeof(ListOrganizationUnitsRequest))]
[JsonSerializable(typeof(ListPersonasRequest))]
[JsonSerializable(typeof(CreateOrganizationUnitRequest))]
[JsonSerializable(typeof(UpdateOrganizationUnitRequest))]
[JsonSerializable(typeof(MoveOrganizationUnitRequest))]
[JsonSerializable(typeof(CreatePersonaRequest))]
[JsonSerializable(typeof(UpdatePersonaRequest))]
[JsonSerializable(typeof(Page<EmployeeResponse>))]
[JsonSerializable(typeof(EmployeeResponse))]
[JsonSerializable(typeof(OrganizationUnitSummaryResponse))]
[JsonSerializable(typeof(PersonaSummaryResponse))]
[JsonSerializable(typeof(EmployeeWorkLocationResponse[]))]
[JsonSerializable(typeof(EmployeeWorkLocationResponse))]
[JsonSerializable(typeof(PersonaSummaryResponse[]))]
[JsonSerializable(typeof(EmployeeLeavePeriodResponse[]))]
[JsonSerializable(typeof(EmployeeLeavePeriodResponse))]
[JsonSerializable(typeof(EmployeeSuspensionPeriodResponse[]))]
[JsonSerializable(typeof(EmployeeSuspensionPeriodResponse))]
[JsonSerializable(typeof(Page<OrganizationUnitResponse>))]
[JsonSerializable(typeof(OrganizationUnitResponse))]
[JsonSerializable(typeof(OrganizationUnitResponse[]))]
[JsonSerializable(typeof(Page<PersonaResponse>))]
[JsonSerializable(typeof(PersonaResponse))]
[JsonSerializable(typeof(EmployeeStatus))]
[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class EmployeesJsonSerializerContext : JsonSerializerContext;
