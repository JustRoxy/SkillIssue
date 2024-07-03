using Microsoft.Extensions.Options;
using SkillIssue.ThirdParty.Osu.Configuration;
using SkillIssue.ThirdParty.Osu.Queries.GetMatchPage;
using SkillIssue.ThirdParty.Osu.Queries.GetMatchPage.Contracts;

namespace SkillIssue.ThirdParty.Osu.Client;

public class OsuClient(
    HttpClient client,
    OsuClientType.Types clientType,
    IOptionsMonitor<OsuSecretsOption> secretMonitor) : IOsuClient
{
    public Task<GetMatchPageResponse> GetNextMatchPage(long lastMatch, CancellationToken cancellationToken)
    {
        var handler = new GetMatchPageHandler(client, GetRequestBuilder());
        return handler.Handle(new GetMatchPageRequest()
        {
            Cursor = lastMatch
        }, cancellationToken);
    }

    private OsuRequestBuilder GetRequestBuilder()
    {
        var secret = secretMonitor.CurrentValue.OsuSecrets[clientType.GetName()];
        return new OsuRequestBuilder(secret);
    }
}