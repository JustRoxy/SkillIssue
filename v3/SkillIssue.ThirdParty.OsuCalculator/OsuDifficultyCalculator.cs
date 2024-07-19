using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Utils;
using SkillIssue.Common;
using BeatmapDifficulty = SkillIssue.Domain.BeatmapDifficulty;

namespace SkillIssue.ThirdParty.OsuCalculator;

public class DifficultyCalculator : IDifficultyCalculator
{
    private static readonly OsuRuleset Ruleset = new();

    private static readonly List<List<Mod>> ModCombinations =
        new OsuDifficultyCalculator(Ruleset.RulesetInfo, null)
            .CreateDifficultyAdjustmentModCombinations()
            .Select(x => ModUtils.FlattenMod(x).ToList())
            .ToList();

    public IEnumerable<BeatmapDifficulty> CalculateBeatmapDifficulty(int beatmapId, byte[] content, CancellationToken cancellationToken)
    {
        try
        {
            var beatmap = GetBeatmap(content);
            return CalculateDifficultyAttributes(beatmapId, beatmap, cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Failed to calculate beatmap. beatmapId: {beatmapId}, content: {content.GetPhysicalSizeInMegabytes():N2}mb",
                e);
        }
    }

    private static List<BeatmapDifficulty> CalculateDifficultyAttributes(int beatmapId, IWorkingBeatmap beatmap,
        CancellationToken cancellationToken)
    {
        var mostCommonBeatLength = beatmap.Beatmap.GetMostCommonBeatLength();

        var attributesList = ModCombinations
            //Calculate all attributes for ModCombination
            .Select(mod =>
                new
                {
                    Mod = mod,
                    Attribute =
                        (OsuDifficultyAttributes)
                        new OsuDifficultyCalculator(Ruleset.RulesetInfo, beatmap).Calculate(mod, cancellationToken)
                })
            .Select(x =>
            {
                //Apply doubletime/halftime
                var rate = x.Mod.OfType<IApplicableToRate>()
                    .Aggregate(1d, (current, mod) => mod.ApplyToRate(0, current));

                var attribute = x.Attribute;
                var applicableToDifficulties = x.Mod.OfType<IApplicableToDifficulty>().ToList();
                var difficultyCopy = applicableToDifficulties.Count == 0
                    ? beatmap.Beatmap.Difficulty
                    : beatmap.Beatmap.Difficulty.Clone();

                //Apply hardrock/easy
                foreach (var applicableToDifficulty in applicableToDifficulties)
                    applicableToDifficulty.ApplyToDifficulty(difficultyCopy);

                return new BeatmapDifficulty
                {
                    BeatmapId = beatmapId,
                    Mods = new LegacyModsDomainProxy(Ruleset.ConvertToLegacyMods(attribute.Mods)),
                    StarRating = attribute.StarRating,
                    AimDifficulty = attribute.AimDifficulty,
                    SpeedDifficulty = attribute.SpeedDifficulty,
                    SpeedNoteCount = attribute.SpeedNoteCount,
                    FlashlightDifficulty = attribute.FlashlightDifficulty,
                    SliderFactor = attribute.SliderFactor,
                    ApproachRate = attribute.ApproachRate,
                    OverallDifficulty = attribute.OverallDifficulty,
                    DrainRate = attribute.DrainRate,
                    MaxCombo = attribute.MaxCombo,
                    Bpm = (int)GetBpm(mostCommonBeatLength, rate),
                    CircleSize = difficultyCopy.CircleSize
                };
            })
            .ToList();

        return attributesList;
    }

    private static IWorkingBeatmap GetBeatmap(byte[] beatmapBytes)
    {
        using var memoryStream = new MemoryStream(beatmapBytes);
        using var stream = new LineBufferedReader(memoryStream);
        var decoder = Decoder.GetDecoder<Beatmap>(stream);
        var beatmap = decoder.Decode(stream);
        var workingBeatmap = new FlatWorkingBeatmap(beatmap);
        return workingBeatmap;
    }

    private static double GetBpm(double commonBeatLength, double rate)
    {
        return 60000d / commonBeatLength * rate;
    }
}