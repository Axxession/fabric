using Fabric.Server.Actors;
using Fabric.Server.Actors.Endpoints;
using Fabric.Server.AccessCatalog;
using Fabric.Server.AccessCatalog.Endpoints;
using Fabric.Server.AccessControl;
using Fabric.Server.AccessControl.Endpoints;
using Fabric.Server.Automation;
using Fabric.Server.CredentialManagement;
using Fabric.Server.CredentialManagement.Endpoints;
using Fabric.Server.Desfire;
using Fabric.Server.Desfire.Endpoints;
using Fabric.Server.Employees;
using Fabric.Server.Employees.Endpoints;
using Fabric.Server.Hardware;
using Fabric.Server.Hardware.Endpoints;
using Fabric.Server.Identities;
using Fabric.Server.Identities.Endpoints;
using Fabric.Server.Infrastructure;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Kiosk;
using Fabric.Server.Kiosk.Endpoints;
using Fabric.Server.Locations;
using Fabric.Server.Locations.Endpoints;
using Fabric.Server.Notifications;
using Fabric.Server.Reception;
using Fabric.Server.Reception.Endpoints;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.EmployeeLifecycle;
using Fabric.Server.Sagas.VisitorPreOnboarding;
using Fabric.Server.Tenants;
using Fabric.Server.Tenants.Endpoints;
using Fabric.Server.Visitors;
using Fabric.Server.Visitors.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfigurationSection openApiSection = builder.Configuration.GetSection("EnableOpenApi");
bool enableOpenApi = openApiSection.Exists() && openApiSection.Get<bool>();

IConfigurationSection automationSection = builder.Configuration.GetSection("EnableAutomation");
bool enableAutomation = automationSection.Exists() && automationSection.Get<bool>();


if (enableOpenApi)
{
    builder.Services.AddOpenApi();
}

builder.Services.AddTransient(_ => TimeProvider.System);
builder.Services.AddSingleton<IApplicationVersionProvider, ApplicationVersionProvider>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Clear());
builder.Services.AddTenancy(builder.Configuration);
builder.Services.AddFabricAuthentication();

builder.Services.AddCors(options =>
{
    string[] origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];

    options.AddPolicy("ApiCors", policy =>
    {
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
                .WithExposedHeaders("x-elsa-workflow-instance-id");
    });
});

builder.Services
    .SetupActors(builder.Configuration)
    .SetupTenants(builder.Configuration)
    .SetupIdentities(builder.Configuration)
    .SetupEmployees(builder.Configuration)
    .SetupCredentialManagement(builder.Configuration)
    .SetupAccessControl(builder.Configuration)
    .SetupAccessCatalog(builder.Configuration)
    .SetupVisitors(builder.Configuration)
    .SetupSagas(builder.Configuration)
    .SetupDesfire(builder.Configuration)
    .SetupHardware(builder.Configuration)
    .SetupKiosk(builder.Configuration)
    .SetupLocations(builder.Configuration)
    .SetupReception(builder.Configuration)
    .SetupNotifications(builder.Configuration);


if (enableAutomation)
{
    builder.Services.SetupAutomation(builder.Configuration);
}

builder.Services.AddHostedService<MigrationsRunner>();

WebApplication app = builder.Build();
string frontendIndexPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");

// Configure the HTTP request pipeline.
if (enableOpenApi)
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseCors("ApiCors");
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

if (File.Exists(frontendIndexPath))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapTenantEndpoints();
app.MapActorEndpoints();
app.MapIdentityEndpoints();
app.MapEmployeeEndpoints();
app.MapCredentialManagementEndpoints();
app.MapAccessControlEndpoints();
app.MapCatalogEndpoints();
app.MapPackageEndpoints();
app.MapAccessGrantEndpoints();
app.MapApprovalGroupEndpoints();
app.MapApprovalDefinitionEndpoints();
app.MapApprovalInboxEndpoints();
app.MapPackageRequestEndpoints();
app.MapApprovalRequirementEndpoints();
app.MapLocationEndpoints();
app.MapReceptionEndpoints();
app.MapReceptionKioskEndpoints();
app.MapReceptionAccessRuleAssignmentEndpoints();
app.MapHardwareManagementEndpoints();
app.MapHardwareOperationEndpoints();
app.MapHardwareAgentEndpoints();
app.MapDesfireChipDesignEndpoints();
app.MapDesfireTransformationEndpoints();
app.MapDesfireSystemProviderEndpoints();
app.MapDesfireEncoderEndpoints();
app.MapDesfireKeyDiversificationStrategyEndpoints();
app.MapDesfireKeyGroupEndpoints();
app.MapDesfireEncodingEndpoints();
app.MapKioskProfileEndpoints();
app.MapKioskEndpoints(enableAutomation);

if (enableAutomation)
{
    app.MapKioskRuntimeEndpoints();
}
app.MapVisitorEndpoints();
app.MapOrganizerEndpoints();
app.MapVisitorPreOnboardingSagaEndpoints();
app.MapEmployeeLifecycleAutomationEndpoints();


if (enableAutomation)
{
    app.UseAutomation();
}


if (File.Exists(frontendIndexPath))
{
    app.MapFallbackToFile("index.html");
}


app.Run();
