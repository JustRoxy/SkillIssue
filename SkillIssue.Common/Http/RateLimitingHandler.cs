using ComposableAsync;

namespace SkillIssue.Common.Http;

public class RateLimitingHandler(string bucket, int requests, TimeSpan per) : DelegatingHandler
{
    private static readonly Dictionary<string, RateLimiter.TimeLimiter> Buckets = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!Buckets.TryGetValue(bucket, out var limiter))
        {
            limiter = RateLimiter.TimeLimiter.GetFromMaxCountByInterval(requests, per);
            Buckets.Add(bucket, limiter);
        }

        await limiter;

        return await base.SendAsync(request, cancellationToken);
    }
}