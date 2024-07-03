using SkillIssue.ThirdParty.Osu.Authorization;

namespace SkillIssue.ThirdParty.Osu;

public class OsuRequestBuilder(OsuSecret secret)
{
    public HttpRequestMessage Create(HttpMethod method, string requestUri)
    {
        var message = new HttpRequestMessage(method, requestUri);
        OsuAuthorizationHandler.RegisterCredentials(message, secret);

        return message;
    }
}