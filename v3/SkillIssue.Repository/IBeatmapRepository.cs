using SkillIssue.Domain;

namespace SkillIssue.Repository;

public interface IBeatmapRepository
{
    public Task InsertBeatmapsIfNotExistWithBulk(IEnumerable<Beatmap> beatmaps, CancellationToken cancellationToken);

    public Task JournalizeMatchBeatmaps(IEnumerable<(int beatmapId, int matchId)> matchBeatmaps,
        CancellationToken cancellationToken);

    public Task<IEnumerable<int>> GetMatchBeatmapsWithStatus(int matchId, Beatmap.BeatmapStatus status,
        CancellationToken cancellationToken);

    public Task InsertDifficultiesWithBulk(IEnumerable<BeatmapDifficulty> difficulties,
        CancellationToken cancellationToken);

    public Task UpdateBeatmap(Beatmap beatmap, CancellationToken cancellationToken);
}