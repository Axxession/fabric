using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Expressions.JavaScript.Options;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Tenants;
using Elsa.Tenants.Extensions;
using Elsa.Workflows;
using Fabric.Server.Automation.Kiosk;
using Fabric.Server.Automation.Kiosk.Providers;
using Fabric.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication;

namespace Fabric.Server.Automation;

public static class AutomationServiceCollectionExtensions
{
    public static IServiceCollection SetupAutomation(this IServiceCollection services, IConfiguration configuration)
    {
        string? databaseSection = configuration.GetConnectionString("Database");
        string getDbConnection(IServiceProvider _) => databaseSection ?? throw new InvalidOperationException();

        services.AddScoped<KioskWorkflowAccessor>();
        services.AddScoped<KioskWorkflowStarter>();
        services.AddScoped<KioskWorkflowResumer>();
        services.AddScoped<AutomationTenantScopeRunner>();

        services.AddNotificationHandlersFrom<Program>();

        //Setup providers:
        services.AddScoped<IPropertyUIHandler, EncodingTransformationProvider>();


        services.AddElsa(elsa =>
        {
            elsa.UseTenants(tenants =>
            {
                tenants.Services.AddScoped<ElsaTenantResolver>();
                tenants.Services.AddScoped<ITenantStore, ElsaTenantStore>();

                tenants.ConfigureMultitenancy(x =>
                {
                    // Create a completely new pipeline builder instance
                    x.TenantResolverPipelineBuilder = new TenantResolverPipelineBuilder().Append<ElsaTenantResolver>();
                });

                tenants.UseStoreBasedTenantsProvider();
            });   // Configure Management layer to use EF Core.
            elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef =>
                    ef.UsePostgreSql(getDbConnection)
                ));

            // Configure Runtime layer to use EF Core.
            elsa.UseWorkflowRuntime(runtime =>
            {
                runtime.UseEntityFrameworkCore(ef => ef.UsePostgreSql(getDbConnection));
                runtime.DistributedLockProvider = _ => new NoopDistributedSynchronizationProvider();
            });



            // Configure ASP.NET authentication/authorization.
            //elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());

            // Expose Elsa API endpoints.
            elsa.UseWorkflowsApi();

            // Setup a SignalR hub for real-time updates from the server.
            // elsa.UseRealTimeWorkflows();

            // Enable C# workflow expressions
            elsa.UseCSharp();

            elsa.UseJavaScript(ConfigureJs);

            // Enable JavaScript workflow expressions
            // elsa.UseJavaScript(options => options.AllowClrAccess = true);

            // Enable HTTP activities.
            elsa.UseHttp(options => options.ConfigureHttpOptions = httpOptions => httpOptions.BaseUrl = new("http://localhost:5139"));

            // Use timer activities.
            elsa.UseScheduling();

            // Register custom activities from the application, if any.
            elsa.AddActivitiesFrom<Program>();

            // Register custom workflows from the application, if any.
            elsa.AddWorkflowsFrom<Program>();

            static void ConfigureJs(JintOptions x)
            {
                x.AllowClrAccess = true;
            }
        });


        //For now we cannot have source generation for Elsa endpoints, so we need to add a fallback resolver to the Elsa namespace that uses runtime reflection
        services.ConfigureHttpJsonOptions(options =>
        {

            options.SerializerOptions.TypeInfoResolverChain.Add(ElsaJsonSerializerContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Add(new NamespaceFallbackJsonTypeInfoResolver("Elsa."));
        });


        return services;
    }


    public static WebApplication UseAutomation(this WebApplication app)
    {
        app.UseWorkflowsApi(); // Use Elsa API endpoints.
        app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
        //app.UseWorkflowsSignalRHubs(); // Optional SignalR integration. Elsa Studio uses SignalR to receive real-time updates from the server.
        return app;
    }

}
