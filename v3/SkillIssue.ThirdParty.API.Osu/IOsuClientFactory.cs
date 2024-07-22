namespace SkillIssue.ThirdParty.API.Osu;

public interface IOsuClientFactory
{
    public IOsuClient CreateClient(OsuClientType.Types clientType);
}