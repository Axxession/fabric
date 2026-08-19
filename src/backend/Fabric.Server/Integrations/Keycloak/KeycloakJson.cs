using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Fabric.Server.Integrations.Keycloak;

internal static class KeycloakJson
{
    public static StringContent CreateJsonContent<T>(T value)
    {
        string json = JsonSerializer.Serialize(value, GetTypeInfo<T>());
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize(json, GetTypeInfo<T>());

    private static JsonTypeInfo<T> GetTypeInfo<T>() =>
        (JsonTypeInfo<T>?)KeycloakJsonSerializerContext.Default.GetTypeInfo(typeof(T))
        ?? throw new InvalidOperationException($"Type {typeof(T).FullName} is not registered in {nameof(KeycloakJsonSerializerContext)}.");
}
