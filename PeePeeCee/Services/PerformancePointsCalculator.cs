using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using SkillIssue.Database;

namespace PeePeeCee.Services;

public class PerformanceCalculationScore
{
    public double Accuracy { get; set; }
    public int MaxCombo { get; set; }

    public int Count300 { get; set; }
    public int Count100 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }

    public List<string> ModAcronyms { get; set; } = [];
}

public class PerformanceCalculationAttribute
{
    public required OsuPerformanceAttributes PerformanceAttributes { get; set; }
    public required OsuDifficultyAttributes DifficultyAttributes { get; set; }
}

public class PerformancePointsCalculator(DatabaseContext context)
{
    public static readonly OsuRuleset OsuRuleset = new();

    private static readonly Mod[] DifficultyAdjustmentMods =
    [
        new OsuModTouchDevice(),
        new OsuModDoubleTime(),
        new OsuModHalfTime(),
        new OsuModEasy(),
        new OsuModHardRock(),
        new OsuModFlashlight()
        // [new OsuModFlashlight(), new OsuModHidden()]
    ];

    public Mod[] NormalizeToDifficultyAdjustmentMods(IEnumerable<Mod> mods)
    {
        var adjustedModsList = mods.Where(x => DifficultyAdjustmentMods.Any(z => z.Type == x.Type)).ToList();
        var hiddenIndex = adjustedModsList.FindIndex(z => z.GetType() == typeof(OsuModHidden));
        if (hiddenIndex != -1 && adjustedModsList.All(z => z.GetType() != typeof(OsuModFlashlight)))
            adjustedModsList.RemoveAt(hiddenIndex);

        return adjustedModsList.ToArray();
    }

    public LegacyMods ConvertToNormalizedLegacyMods(PerformanceCalculationScore score)
    {
        return ConvertToLegacyMods(
            NormalizeToDifficultyAdjustmentMods(ModsFromAcronyms(score))
        );
    }

    public LegacyMods ConvertToLegacyMods(IEnumerable<Mod> mods)
    {
        return OsuRuleset.ConvertToLegacyMods(mods.ToArray());
    }

    public Mod[] ModsFromAcronyms(PerformanceCalculationScore score)
    {
        var modsFromAcronyms = new List<Mod>();
        foreach (var acronym in score.ModAcronyms.Select(x => x.ToUpper()))
        {
            var mod = OsuRuleset.CreateModFromAcronym(acronym);
            if (mod is null) throw new Exception($"Unknown mod acronym {acronym}");
            modsFromAcronyms.Add(mod);
        }

        return modsFromAcronyms.ToArray();
    }

    public async Task<PerformanceCalculationAttribute?> CalculateOsu(int beatmapId,
        PerformanceCalculationScore calculationScore)
    {
        var modsFromAcronyms = ModsFromAcronyms(calculationScore);
        var score = new ScoreInfo
        {
            Mods = modsFromAcronyms,
            Statistics = new Dictionary<HitResult, int>
            {
                { HitResult.Great, calculationScore.Count300 },
                { HitResult.Ok, calculationScore.Count100 },
                { HitResult.Meh, calculationScore.Count50 },
                { HitResult.Miss, calculationScore.CountMiss }
            },
            MaxCombo = calculationScore.MaxCombo,
            Accuracy = calculationScore.Accuracy
        };

        var adjustedMods = NormalizeToDifficultyAdjustmentMods(score.Mods);
        var legacyMods = (int)ConvertToLegacyMods(adjustedMods);
        var attributes = await context.BeatmapPerformances
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BeatmapId == beatmapId && x.Mods == legacyMods);

        if (attributes is null) return null;

        var calculator = new OsuPerformanceCalculator();
        var osuDifficultyAttributes = new OsuDifficultyAttributes
        {
            Mods = score.Mods,
            StarRating = attributes.StarRating,
            MaxCombo = attributes.MaxCombo,
            AimDifficulty = attributes.AimDifficulty,
            SpeedDifficulty = attributes.SpeedDifficulty,
            SpeedNoteCount = attributes.SpeedNoteCount,
            FlashlightDifficulty = attributes.FlashlightDifficulty,
            SliderFactor = attributes.SliderFactor,
            ApproachRate = attributes.ApproachRate,
            OverallDifficulty = attributes.OverallDifficulty,
            DrainRate = attributes.DrainRate,
            HitCircleCount = attributes.HitCircleCount,
            SliderCount = attributes.SliderCount,
            SpinnerCount = attributes.SpinnerCount
        };

        return new PerformanceCalculationAttribute
        {
            PerformanceAttributes = (OsuPerformanceAttributes)calculator.Calculate(score, osuDifficultyAttributes),
            DifficultyAttributes = osuDifficultyAttributes
        };
    }
}