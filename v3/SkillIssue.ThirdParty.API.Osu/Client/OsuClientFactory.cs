using Microsoft.Extensions.Options;
using SkillIssue.ThirdParty.API.Osu.Configuration;

namespace SkillIssue.ThirdParty.API.Osu.Client;

public class OsuClientFactory : IOsuClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<OsuSecretsOption> _secretMonitor;

    public OsuClientFactory(IHttpClientFactory httpClientFactory, IOptionsMonitor<OsuSecretsOption> secretMonitor)
    {
        _httpClientFactory = httpClientFactory;
        _secretMonitor = secretMonitor;
    }

    public IOsuClient CreateClient(OsuClientType.Types clientType)
    {
        var client = _httpClientFactory.CreateClient(clientType.GetName());
        return new OsuClient(client, clientType, _secretMonitor);
    }
}