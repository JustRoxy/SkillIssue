using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SkillIssue.Common.Http;

public class OsuAuthorizationHandler(
    HttpClient authorizationClient,
    IOptionsMonitor<OsuAuthorizationCredentials> credentials,
    ILogger<OsuAuthorizationHandler> logger,
    string clientName)
    : DelegatingHandler
{
    private static readonly ConcurrentDictionary<string, AuthorizationToken> CachedTokens = new();
    private readonly OsuAuthorizationCredentials _credentials = credentials.CurrentValue;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!CachedTokens.TryGetValue(clientName, out var token))
        {
            token = await GetNewAuthorizationToken(_credentials);
        }

        CachedTokens[clientName] = token;
        logger.LogDebug("Saved token with expiration {Expiration} for {ClientName}", token.ExpiresInTime, clientName);

        SetAuthorizationHeader(request, token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)) return response;

        logger.LogWarning("Received {StatusCode} on {Uri}. Forcing token update", response.StatusCode,
            request.RequestUri);
        token = await GetNewAuthorizationToken(_credentials);
        CachedTokens[clientName] = token;
        SetAuthorizationHeader(request, token);

        return await base.SendAsync(request, cancellationToken);
    }

    private static void SetAuthorizationHeader(HttpRequestMessage requestMessage, AuthorizationToken authorizationToken)
    {
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorizationToken.AccessToken);
    }

    private async Task<AuthorizationToken> GetNewAuthorizationToken(OsuAuthorizationCredentials credentials)
    {
        logger.LogInformation("Sending new token authorization request");
        var response = await authorizationClient.PostAsJsonAsync("https://osu.ppy.sh/oauth/token", new
        {
            client_id = credentials.ClientId,
            client_secret = credentials.ClientSecret,
            grant_type = "client_credentials",
            scope = "public"
        });

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<AuthorizationToken>();

        logger.LogInformation("Received token authorization response for {ClientName}. expires_in: {ExpiresIn}", clientName, token!.ExpiresIn);

        return token;
    }
}