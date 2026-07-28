namespace Fabric.Server.Infrastructure.Authentication;

public static class FabricRoleDefaults
{
    public const string AdminRole = "admin";
    public const string HostRole = "host";
    public const string SecurityOfficerRole = "security-officer";

    public const string AdminPolicy = "AdminOnly";
    public const string HostPolicy = "HostOnly";
    public const string SecurityOfficerPolicy = "SecurityOfficerOnly";
    public const string AdminOrSecurityOfficerPolicy = "AdminOrSecurityOfficer";
}
