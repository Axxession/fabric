using Fabric.Server.Employees.Application;
using Fabric.Server.Employees.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Employees;

public static class EmployeeServiceCollectionExtensions
{
    public static IServiceCollection SetupEmployees(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<EmployeesDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", EmployeesDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(EmployeesJsonSerializerContext.Default));

        collection.AddScoped<EmployeeService>();
        collection.AddScoped<EmployeeLifecycleService>();
        collection.AddSingleton<EmployeeLifecycleTrigger>();
        collection.AddHostedService<EmployeeLifecycleWorker>();
        return collection;
    }
}
