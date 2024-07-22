using System.Net;
using SkillIssue.ThirdParty.API.Osu.Queries.GetBeatmapContent.Contracts;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetBeatmapContent;

public class GetBeatmapContentHandler(HttpClient client, OsuRequestBuilder requestBuilder)
{
    public async Task<byte[]?> Handle(GetBeatmapContentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var httpRequest = requestBuilder.Create(HttpMethod.Get, $"osu/{request.BeatmapId}");
            var response = await client.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to fetch a beatmap from osu/{request.BeatmapId}", e);
        }
    }
}