using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PlayerPerformanceCalculator.Services;

public class ScoreProcessing
{
    private static readonly Ruleset Ruleset = new OsuRuleset();

    //TODO: it shouldn't be here but it's 5AM and im tired
    public IEnumerable<string> GetModificationAcronym(LegacyMods legacyMods)
    {
        return Ruleset.ConvertFromLegacyMods(legacyMods).Select(x => x.Acronym).Order();
    }

    public ScoreRank GetGrade(double accuracy, LegacyMods legacyMods, Dictionary<HitResult, int> hits)
    {
        var processor = Ruleset.CreateScoreProcessor();

        var rank = processor.RankFromScore(accuracy, hits);
        var mods = Ruleset.ConvertFromLegacyMods(legacyMods);

        foreach (var mod in mods.OfType<IApplicableToScoreProcessor>())
            rank = mod.AdjustRank(rank, accuracy);

        return rank;
    }
}