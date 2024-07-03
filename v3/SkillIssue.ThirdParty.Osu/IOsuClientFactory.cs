namespace SkillIssue.ThirdParty.Osu;

public interface IOsuClientFactory
{
    public IOsuClient CreateClient(OsuClientType.Types clientType);
}