namespace SkillIssue.Common.Http;

public class OsuAuthorizationCredentials
{
    public int ClientId { get; set; }
    public required string ClientSecret { get; set; }
}