namespace SkillIssue.Infrastructure.Repositories.MatchFrameRepository.Contracts;

public class MatchFrameRecord
{
    public int MatchId { get; set; } = 0;
    public long Cursor { get; set; } = 0;
    public byte[] Frame { get; set; } = [];
}