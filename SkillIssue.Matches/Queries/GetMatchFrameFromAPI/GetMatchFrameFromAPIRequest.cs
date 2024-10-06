using MediatR;

namespace SkillIssue.Matches.Queries.GetMatchFrameFromAPI;

public class GetMatchFrameFromAPIRequest : IRequest<GetMatchFrameFromAPIResponse>
{
    public int MatchId { get; set; }
    public long Cursor { get; set; }
}