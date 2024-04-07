namespace SkillIssue.Authorization;

public class ApiAuthorizationConfiguration
{
    public List<string> AllowedSources { get; set; }

    public bool IsAllowed(string source) =>
        !string.IsNullOrWhiteSpace(source) && AllowedSources.Contains(source);
}