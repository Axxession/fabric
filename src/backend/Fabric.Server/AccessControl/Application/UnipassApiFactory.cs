using System.Text;
using System.Text.Json;
using AccessControl.Unipass.Contracts;
using AccessControl.Unipass.Infrastructure;
using Fabric.Server.AccessControl.Domain;

namespace Fabric.Server.AccessControl.Application;

public sealed class UnipassApiFactory
{
    public IUnipassApi Create(UnipassSystemConfig config)
    {
        HttpClientHandler handler = new();
        if (!config.SslValidation)
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        HttpClient client = new(new UnipassAuthorizationHeader(CreateAuthorizationHeader(config))
        {
            InnerHandler = handler
        })
        {
            BaseAddress = new Uri(config.Endpoint)
        };

        return new UnipassWebApi(
            client,
            TimeZoneInfo.Local,
            new JsonSerializerOptions { PropertyNamingPolicy = null });
    }

    private static string CreateAuthorizationHeader(UnipassSystemConfig config)
    {
        string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}"));
        return $"Basic {credentials}";
    }
}
