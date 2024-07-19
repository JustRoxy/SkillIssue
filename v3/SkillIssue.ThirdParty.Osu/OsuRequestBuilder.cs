using SkillIssue.ThirdParty.Osu.Authorization;

namespace SkillIssue.ThirdParty.Osu;

public class OsuRequestBuilder(OsuClientType.Types clientType, OsuSecret? secret)
{
    public HttpRequestMessage Create(HttpMethod method, string requestUri)
    {
        var message = new HttpRequestMessage(method, requestUri);
        if (secret is not null)
            OsuAuthorizationHandler.RegisterCredentials(message, secret);

        RateLimiterHandler.RegisterMessage(message, clientType);
        return message;
    }
}