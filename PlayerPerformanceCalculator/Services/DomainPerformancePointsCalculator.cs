using Microsoft.Extensions.Logging;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using SkillIssue.Domain.PPC.Entities;
using SkillIssue.Domain.Unfair;
using Score = SkillIssue.Domain.Unfair.Entities.Score;

namespace PlayerPerformanceCalculator.Services;

public class DomainPerformancePointsCalculator(ILogger<DomainPerformancePointsCalculator> logger)
    : IPerformancePointsCalculator
{
    private static readonly OsuRuleset OsuRuleset = new();

    public async Task<double?> CalculatePerformancePoints(BeatmapPerformance beatmapPerformance, Score score,
        CancellationToken token)
    {
        var performanceCalculator = new OsuPerformanceCalculator();
        var mods = OsuRuleset.ConvertFromLegacyMods((LegacyMods)beatmapPerformance.Mods).ToArray();

        var difficultyAttributes = new OsuDifficultyAttributes
        {
            Mods = mods,
            StarRating = beatmapPerformance.StarRating,
            MaxCombo = beatmapPerformance.MaxCombo,
            AimDifficulty = beatmapPerformance.AimDifficulty,
            AimDifficultSliderCount = beatmapPerformance.AimDifficultSliderCount,
            SpeedDifficulty = beatmapPerformance.SpeedDifficulty,
            SpeedNoteCount = beatmapPerformance.SpeedNoteCount,
            FlashlightDifficulty = beatmapPerformance.FlashlightDifficulty,
            SliderFactor = beatmapPerformance.SliderFactor,
            AimDifficultStrainCount = beatmapPerformance.AimDifficultStrainCount,
            SpeedDifficultStrainCount = beatmapPerformance.SpeedDifficultStrainCount,
            DrainRate = beatmapPerformance.DrainRate,
            HitCircleCount = beatmapPerformance.HitCircleCount,
            SliderCount = beatmapPerformance.SliderCount,
            SpinnerCount = beatmapPerformance.SpinnerCount,
        };

        var scoreInfo = new ScoreInfo
        {
            Statistics = new Dictionary<HitResult, int>
            {
                {
                    HitResult.Great, score.Count300
                },
                {
                    HitResult.Ok, score.Count100
                },
                {
                    HitResult.Meh, score.Count50
                },
                {
                    HitResult.Miss, score.CountMiss
                }
            },
            MaxCombo = score.MaxCombo,
            Accuracy = score.Accuracy,
            Mods = mods,
            BeatmapInfo = new BeatmapInfo
            {
                Difficulty = beatmapPerformance.ConvertToDifficulty()
            }
        };

        try
        {
            return (await performanceCalculator.CalculateAsync(scoreInfo, difficultyAttributes, token)).Total;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An exception happened at DomainPerformancePointsCalculator");
            return null;
        }
    }
}