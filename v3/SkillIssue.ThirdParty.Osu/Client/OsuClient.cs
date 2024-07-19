using Microsoft.Extensions.Options;
using SkillIssue.ThirdParty.Osu.Configuration;
using SkillIssue.ThirdParty.Osu.Queries.GetBeatmapContent;
using SkillIssue.ThirdParty.Osu.Queries.GetBeatmapContent.Contracts;
using SkillIssue.ThirdParty.Osu.Queries.GetMatch;
using SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts;
using SkillIssue.ThirdParty.Osu.Queries.GetMatchPage;
using SkillIssue.ThirdParty.Osu.Queries.GetMatchPage.Contracts;

namespace SkillIssue.ThirdParty.Osu.Client;

public class OsuClient(
    HttpClient client,
    OsuClientType.Types clientType,
    IOptionsMonitor<OsuSecretsOption> secretMonitor) : IOsuClient
{
    public Task<byte[]?> GetBeatmapContent(int beatmapId, CancellationToken cancellationToken)
    {
        var handler = new GetBeatmapContentHandler(client, GetRequestBuilder());
        return handler.Handle(new GetBeatmapContentRequest()
        {
            BeatmapId = beatmapId
        }, cancellationToken);
    }

    public Task<GetMatchPageResponse> GetNextMatchPage(long lastMatch, CancellationToken cancellationToken)
    {
        var handler = new GetMatchPageHandler(client, GetRequestBuilder());
        return handler.Handle(new GetMatchPageRequest()
        {
            Cursor = lastMatch
        }, cancellationToken);
    }

    public GetMatchResponse GetMatchAsAsyncEnumerable(GetMatchRequest request,
        CancellationToken cancellationToken)
    {
        var handler = new GetMatchHandler(client, GetRequestBuilder());
        return handler.Handle(request, cancellationToken);
    }

    private OsuRequestBuilder GetRequestBuilder()
    {
        var secret = secretMonitor.CurrentValue.OsuSecrets!.GetValueOrDefault(clientType.GetName(), null);
        return new OsuRequestBuilder(clientType, secret);
    }
}