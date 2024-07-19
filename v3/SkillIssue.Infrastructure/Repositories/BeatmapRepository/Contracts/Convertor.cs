using SkillIssue.Domain;

namespace SkillIssue.Infrastructure.Repositories.BeatmapRepository.Contracts;

public static class Convertor
{
    public static MatchBeatmapRelationRecord FromDomain(this (int beatmapId, int matchId) matchBeatmap)
    {
        return new MatchBeatmapRelationRecord
        {
            BeatmapId = matchBeatmap.beatmapId,
            MatchId = matchBeatmap.matchId
        };
    }

    public static BeatmapRecord FromDomain(this Beatmap beatmap)
    {
        return new BeatmapRecord
        {
            BeatmapId = beatmap.BeatmapId,
            Status = (int)beatmap.Status,
            Content = beatmap.Content,
            LastUpdate = beatmap.LastUpdate.ToUniversalTime()
        };
    }

    public static Beatmap ToDomain(this BeatmapRecord record)
    {
        return new Beatmap
        {
            BeatmapId = record.BeatmapId,
            Status = (Beatmap.BeatmapStatus)record.Status,
            Content = record.Content,
            LastUpdate = record.LastUpdate
        };
    }
}