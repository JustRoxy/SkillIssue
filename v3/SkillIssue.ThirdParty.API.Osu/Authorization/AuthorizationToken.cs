using System.Text.Json.Serialization;

namespace SkillIssue.ThirdParty.API.Osu.Authorization;

public class AuthorizationToken
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";

    private int _expiresIn = 0;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn
    {
        get => _expiresIn;
        set
        {
            _expiresIn = value;
            ExpiresInTime = DateTime.Now.AddSeconds(_expiresIn);
        }
    }

    public DateTime ExpiresInTime { get; private set; }
}