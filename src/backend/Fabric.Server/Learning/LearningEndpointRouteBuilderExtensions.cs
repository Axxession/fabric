using Fabric.Server.Learning.Endpoints;

namespace Fabric.Server.Learning;

public static class LearningEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCourseEndpoints();
        app.MapEnrollmentEndpoints();
        app.MapLearningRuntimeEndpoints();
        return app;
    }
}
