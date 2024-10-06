using SkillIssue.Beatmaps.Models;

namespace SkillIssue.Beatmaps.Services;

public interface IBeatmapProvider
{
    public Task<Beatmap> GetBeatmap(string id);
}