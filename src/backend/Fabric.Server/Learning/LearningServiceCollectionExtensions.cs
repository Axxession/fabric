using Fabric.Server.Learning.Application;
using Fabric.Server.Learning.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Learning;

public static class LearningServiceCollectionExtensions
{
    public static IServiceCollection SetupLearning(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<LearningDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", LearningDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(LearningJsonSerializerContext.Default));

        collection.AddScoped<LearningManifestParser>();
        collection.AddScoped<ILearningPackageStorage, LearningPackageStorage>();
        collection.AddScoped<CourseService>();
        collection.AddScoped<EnrollmentService>();
        collection.AddScoped<LearningRuntimeService>();
        return collection;
    }
}
