using RateLimiter;

namespace SkillIssue.ThirdParty.Osu;

public class RateLimiterHandler : DelegatingHandler
{
    private static readonly TimeLimiter
        RateLimiter = TimeLimiter.GetFromMaxCountByInterval(60, TimeSpan.FromMinutes(1));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return RateLimiter.Enqueue(() => base.SendAsync(request, cancellationToken), cancellationToken);
    }
}