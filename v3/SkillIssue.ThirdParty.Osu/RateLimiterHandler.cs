using RateLimiter;

namespace SkillIssue.ThirdParty.Osu;

public class RateLimiterHandler : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<string> RATE_LIMITER_KEY = new HttpRequestOptionsKey<string>("RATE_LIMITER_KEY");

    private static readonly Dictionary<string, TimeLimiter> RateLimiters = [];

    private static readonly TimeLimiter
        DefaultRateLimiter = TimeLimiter.GetFromMaxCountByInterval(60, TimeSpan.FromMinutes(1));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Options.TryGetValue(RATE_LIMITER_KEY, out var rateLimiterKey);
        var rateLimiter = RateLimiters!.GetValueOrDefault(rateLimiterKey, DefaultRateLimiter);

        return rateLimiter.Enqueue(() => base.SendAsync(request, cancellationToken), cancellationToken);
    }

    public static void RegisterMessage(HttpRequestMessage message, OsuClientType.Types clientType)
    {
        message.Options.Set(RATE_LIMITER_KEY, clientType.GetName());
    }

    public static void SetRateLimiterForClient(string clientType, int requests, TimeSpan perSpan)
    {
        RateLimiters[clientType] = TimeLimiter.GetFromMaxCountByInterval(requests, perSpan);
    }
}