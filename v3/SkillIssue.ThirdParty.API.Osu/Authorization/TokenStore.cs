namespace SkillIssue.ThirdParty.API.Osu.Authorization;

public class TokenStore
{
    private readonly Dictionary<int, AuthorizationToken> _tokens = [];
    private const int EXPIRATION_JITTER_IN_SECONDS = 15;

    public AuthorizationToken? GetToken(int clientId)
    {
        var token = _tokens!.GetValueOrDefault(clientId, null);

        if (IsTokenExpired(token)) return null;
        return token;
    }

    public void SetToken(int clientId, AuthorizationToken token)
    {
        if (IsTokenExpired(token))
            throw new Exception(
                $"Trying to set an expired token. Now: {DateTime.Now}, ExpiresIn: {token.ExpiresIn}, Time: {token.ExpiresInTime}");

        _tokens[clientId] = token;
    }

    private bool IsTokenExpired(AuthorizationToken? authorizationToken)
    {
        if (authorizationToken is null) return false;

        return DateTime.Now.AddSeconds(-EXPIRATION_JITTER_IN_SECONDS) > authorizationToken.ExpiresInTime;
    }
}