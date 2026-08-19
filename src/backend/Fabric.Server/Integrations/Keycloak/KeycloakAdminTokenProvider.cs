using System.Net.Http.Headers;
using System.Collections.Concurrent;
using Fabric.Server.Core;
using Fabric.Server.Tenants.Domain;

namespace Fabric.Server.Integrations.Keycloak;

public sealed class KeycloakAdminTokenProvider(
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider,
    ILogger<KeycloakAdminTokenProvider> logger)
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, CachedToken> _tokens = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<Result<AuthenticationHeaderValue, KeycloakAdminError>> GetAuthorizationHeaderAsync(
        KeycloakAdminApiIntegrationConfig config,
        CancellationToken cancellationToken)
    {
        string cacheKey = BuildCacheKey(config);
        DateTimeOffset now = timeProvider.GetUtcNow();

        if (TryGetCachedToken(cacheKey, now, out CachedToken cachedToken))
        {
            KeycloakAdminTokenProviderLog.TokenCacheHit(logger, config.Url, config.Realm, config.ClientId, cachedToken.ExpiresAt);
            return Result.Success<AuthenticationHeaderValue, KeycloakAdminError>(new AuthenticationHeaderValue("Bearer", cachedToken.AccessToken));
        }

        KeycloakAdminTokenProviderLog.TokenCacheMiss(logger, config.Url, config.Realm, config.ClientId);

        SemaphoreSlim tokenLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await tokenLock.WaitAsync(cancellationToken);

        try
        {
            now = timeProvider.GetUtcNow();
            if (TryGetCachedToken(cacheKey, now, out cachedToken))
            {
                KeycloakAdminTokenProviderLog.TokenCacheHitAfterWait(logger, config.Url, config.Realm, config.ClientId, cachedToken.ExpiresAt);
                return Result.Success<AuthenticationHeaderValue, KeycloakAdminError>(new AuthenticationHeaderValue("Bearer", cachedToken.AccessToken));
            }

            Result<CachedToken, KeycloakAdminError> tokenResult = await RequestTokenAsync(config, cancellationToken);
            if (tokenResult.IsFailure(out KeycloakAdminError error))
                return Result.Failure<AuthenticationHeaderValue, KeycloakAdminError>(error);

            tokenResult.IsSuccess(out CachedToken token);
            _tokens[cacheKey] = token;
            KeycloakAdminTokenProviderLog.TokenCached(logger, config.Url, config.Realm, config.ClientId, token.ExpiresAt);
            return Result.Success<AuthenticationHeaderValue, KeycloakAdminError>(new AuthenticationHeaderValue("Bearer", token.AccessToken));
        }
        finally
        {
            tokenLock.Release();
        }
    }

    public void Invalidate(KeycloakAdminApiIntegrationConfig config) =>
        _tokens.TryRemove(BuildCacheKey(config), out _);

    private async Task<Result<CachedToken, KeycloakAdminError>> RequestTokenAsync(
        KeycloakAdminApiIntegrationConfig config,
        CancellationToken cancellationToken)
    {
        using HttpClient client = httpClientFactory.CreateClient(KeycloakIntegrationServiceCollectionExtensions.HttpClientName);

        using HttpRequestMessage request = new(HttpMethod.Post, BuildTokenEndpoint(config));
        request.Content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", config.ClientId),
            new KeyValuePair<string, string>("client_secret", config.ClientSecret),
        ]);

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string detail = await response.Content.ReadAsStringAsync(cancellationToken);
            KeycloakAdminTokenProviderLog.TokenRequestFailed(
                logger,
                BuildTokenEndpoint(config),
                (int)response.StatusCode,
                detail);

            return Result.Failure<CachedToken, KeycloakAdminError>(
                new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, BuildErrorDetail("Keycloak token request failed.", response.StatusCode, detail)));
        }

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        KeycloakAccessTokenResponse? token = KeycloakJson.Deserialize<KeycloakAccessTokenResponse>(json);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return Result.Failure<CachedToken, KeycloakAdminError>(
                new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, "Keycloak token response did not include an access token."));
        }

        DateTimeOffset expiresAt = timeProvider.GetUtcNow().AddSeconds(Math.Max(token.ExpiresIn, 1));
        return Result.Success<CachedToken, KeycloakAdminError>(new CachedToken(token.AccessToken, expiresAt));
    }

    private bool TryGetCachedToken(string cacheKey, DateTimeOffset now, out CachedToken cachedToken)
    {
        if (_tokens.TryGetValue(cacheKey, out CachedToken? token) && token.ExpiresAt - RefreshSkew > now)
        {
            cachedToken = token;
            return true;
        }

        cachedToken = default!;
        return false;
    }

    private static string BuildCacheKey(KeycloakAdminApiIntegrationConfig config) =>
        $"{config.Url}|{config.Realm}|{config.ClientId}";

    private static string BuildTokenEndpoint(KeycloakAdminApiIntegrationConfig config) =>
        $"{config.Url.TrimEnd('/')}/realms/{Uri.EscapeDataString(config.Realm)}/protocol/openid-connect/token";

    private static string BuildErrorDetail(string prefix, System.Net.HttpStatusCode statusCode, string? detail)
    {
        string normalizedDetail = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail.Trim()}";
        return $"{prefix} Status {(int)statusCode}.{normalizedDetail}".Trim();
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt);
}

internal static partial class KeycloakAdminTokenProviderLog
{
    [LoggerMessage(
        EventId = 18000,
        Level = LogLevel.Debug,
        Message = "Keycloak token cache hit for {BaseUrl} realm {Realm} client {ClientId}. Expires at {ExpiresAt}")]
    public static partial void TokenCacheHit(ILogger logger, string baseUrl, string realm, string clientId, DateTimeOffset expiresAt);

    [LoggerMessage(
        EventId = 18001,
        Level = LogLevel.Debug,
        Message = "Keycloak token request failed for {TokenEndpoint}. Status {StatusCode}. Response body: {ResponseBody}")]
    public static partial void TokenRequestFailed(ILogger logger, string tokenEndpoint, int statusCode, string responseBody);

    [LoggerMessage(
        EventId = 18002,
        Level = LogLevel.Debug,
        Message = "Keycloak token cache miss for {BaseUrl} realm {Realm} client {ClientId}")]
    public static partial void TokenCacheMiss(ILogger logger, string baseUrl, string realm, string clientId);

    [LoggerMessage(
        EventId = 18003,
        Level = LogLevel.Debug,
        Message = "Keycloak token cache filled by concurrent request for {BaseUrl} realm {Realm} client {ClientId}. Expires at {ExpiresAt}")]
    public static partial void TokenCacheHitAfterWait(ILogger logger, string baseUrl, string realm, string clientId, DateTimeOffset expiresAt);

    [LoggerMessage(
        EventId = 18004,
        Level = LogLevel.Debug,
        Message = "Keycloak token cached for {BaseUrl} realm {Realm} client {ClientId}. Expires at {ExpiresAt}")]
    public static partial void TokenCached(ILogger logger, string baseUrl, string realm, string clientId, DateTimeOffset expiresAt);
}
