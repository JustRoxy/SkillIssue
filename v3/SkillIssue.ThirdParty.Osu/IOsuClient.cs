using SkillIssue.ThirdParty.Osu.Queries.GetMatchPage.Contracts;

namespace SkillIssue.ThirdParty.Osu;

public interface IOsuClient
{
    public Task<GetMatchPageResponse> GetNextMatchPage(long lastMatch, CancellationToken cancellationToken);
}