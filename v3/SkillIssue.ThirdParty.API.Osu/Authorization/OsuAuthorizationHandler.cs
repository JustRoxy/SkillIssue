using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace SkillIssue.ThirdParty.API.Osu.Authorization;

public class OsuAuthorizationHandler : DelegatingHandler
{
    private readonly HttpClient _authorizationClient;
    private readonly TokenStore _tokenStore;
    private readonly ILogger<OsuAuthorizationHandler> _logger;

    private static readonly HttpRequestOptionsKey<OsuSecret> CredentialsProperty = new("Credentials");

    public OsuAuthorizationHandler(HttpClient authorizationClient, TokenStore tokenStore,
        ILogger<OsuAuthorizationHandler> logger)
    {
        _authorizationClient = authorizationClient;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public static void RegisterCredentials(HttpRequestMessage requestMessage, OsuSecret secret)
    {
        requestMessage.Options.Set(CredentialsProperty, secret);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Options.TryGetValue(CredentialsProperty, out var credentials))
        {
            throw new HttpRequestException(
                $"Failed to find CredentialsProperty option. Register credentials with {nameof(RegisterCredentials)}");
        }

        var token = _tokenStore.GetToken(credentials.ClientId) ?? await GetNewAuthorizationToken(credentials);
        _tokenStore.SetToken(credentials.ClientId, token);
        SetAuthorizationHeader(request, token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
        {
            _logger.LogWarning("Received {StatusCode} on {Uri}. Forcing token update", response.StatusCode,
                request.RequestUri);
            token = await GetNewAuthorizationToken(credentials);
            _tokenStore.SetToken(credentials.ClientId, token);
            SetAuthorizationHeader(request, token);

            return await base.SendAsync(request, cancellationToken);
        }

        return response;
    }

    private void SetAuthorizationHeader(HttpRequestMessage requestMessage, AuthorizationToken authorizationToken)
    {
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorizationToken.AccessToken);
    }

    private async Task<AuthorizationToken> GetNewAuthorizationToken(OsuSecret credentials)
    {
        _logger.LogInformation("Sending new token authorization request");
        var response = await _authorizationClient.PostAsJsonAsync("https://osu.ppy.sh/oauth/token", new
        {
            client_id = credentials.ClientId,
            client_secret = credentials.ClientSecret,
            grant_type = "client_credentials",
            scope = "public"
        });

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<AuthorizationToken>();

        _logger.LogInformation("Received token authorization response. expires_in: {ExpiresIn}", token!.ExpiresIn);

        return token;
    }
}