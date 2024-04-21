using ComposableAsync;
using Microsoft.Extensions.Logging;
using RateLimiter;

namespace PlayerPerformanceCalculator.Services;

public class BeatmapLookup(HttpClient client, ILogger<BeatmapLookup> logger)
{
    private static readonly TimeLimiter RateLimit = TimeLimiter.GetFromMaxCountByInterval(2, TimeSpan.FromSeconds(1));

    public async Task<string?> GetBeatmap(int beatmapId)
    {
        logger.LogInformation("Requesting beatmap {BeatmapId}", beatmapId);
        await RateLimit;

        var response = await client.GetAsync($"osu/{beatmapId}");

        var content = await response.Content.ReadAsStringAsync();
        return content;
    }
}