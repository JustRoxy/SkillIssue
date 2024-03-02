using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ComposableAsync;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateLimiter;

namespace TheGreatMultiplayerLibrary.HttpHandlers;

public class TokenConfiguration
{
    public required int ClientId { get; set; }
    public required string ClientSecret { get; set; } = null!;
}

public class TgsRateLimitConfiguration : TokenConfiguration;

public class TgsRateLimitHandler(
    IOptions<TgsRateLimitConfiguration> configuration,
    ILogger<TokenRateLimitHandler> logger)
    : TokenRateLimitHandler(configuration, logger)
{
    private static readonly TimeLimiter
        RateLimiter = TimeLimiter.GetFromMaxCountByInterval(60, TimeSpan.FromMinutes(1));

    private static string? _accessToken;

    protected override string? AccessToken
    {
        get => _accessToken;
        set => _accessToken = value;
    }

    protected override async Task WaitForRate()
    {
        await RateLimiter;
    }
}

public class TgmlRateLimitConfiguration : TokenConfiguration;

public class TgmlRateLimitHandler(
    IOptions<TgmlRateLimitConfiguration> configuration,
    ILogger<TokenRateLimitHandler> logger)
    : TokenRateLimitHandler(configuration, logger)
{
    private static readonly TimeLimiter
        RateLimiter = TimeLimiter.GetFromMaxCountByInterval(60, TimeSpan.FromMinutes(1));

    private static string? _accessToken;

    protected override string? AccessToken
    {
        get => _accessToken;
        set => _accessToken = value;
    }

    protected override async Task WaitForRate()
    {
        await RateLimiter;
    }
}

public abstract class TokenRateLimitHandler(
    IOptions<TokenConfiguration> configuration,
    ILogger<TokenRateLimitHandler> logger)
    : DelegatingHandler
{
    protected abstract string? AccessToken { get; set; }
    protected abstract Task WaitForRate();

    private async Task RequestToken()
    {
        using var httpClient = new HttpClient();

        var response = await httpClient.PostAsJsonAsync("https://osu.ppy.sh/oauth/token", new
        {
            client_id = configuration.Value.ClientId,
            client_secret = configuration.Value.ClientSecret,
            scope = "public",
            grant_type = "client_credentials"
        });

        var content = await response.Content.ReadFromJsonAsync<JsonObject>();
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden || content is null)
            throw new Exception("get ban'd nerd :D");

        response.EnsureSuccessStatusCode();

        AccessToken = content["access_token"].Deserialize<string>();
    }

    private async Task<HttpResponseMessage> RateLimitSend(HttpRequestMessage requestMessage, CancellationToken token)
    {
        logger.LogInformation("Waiting for time limiter");
        await WaitForRate();
        return await base.SendAsync(requestMessage, token);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (AccessToken is null) await RequestToken();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var response = await RateLimitSend(request, cancellationToken);
        if (response.StatusCode is not HttpStatusCode.Unauthorized) return response;

        await RequestToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        response = await RateLimitSend(request, cancellationToken);

        return response;
    }
}