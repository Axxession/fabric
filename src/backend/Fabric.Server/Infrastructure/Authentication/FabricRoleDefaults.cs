namespace Fabric.Server.Infrastructure.Authentication;

public static class FabricRoleDefaults
{
    public const string AdminRole = "admin";
    public const string HostRole = "host";
    public const string ManagerRole = "manager";
    public const string SecurityOfficerRole = "security-officer";
    public const string ContractorEnrollmentRole = "contractor-enrollment";
    public const string ContractorPlanningRole = "contractor-planning";

    public const string AdminPolicy = "AdminOnly";
    public const string HostPolicy = "HostOnly";
    public const string ManagerPolicy = "ManagerOnly";
    public const string SecurityOfficerPolicy = "SecurityOfficerOnly";
    public const string AdminOrSecurityOfficerPolicy = "AdminOrSecurityOfficer";
    public const string ContractorEnrollmentPolicy = "ContractorEnrollmentOnly";
    public const string ContractorPlanningPolicy = "ContractorPlanningOnly";
    public const string ContractorEnrollmentOrPlanningPolicy = "ContractorEnrollmentOrPlanning";
}
