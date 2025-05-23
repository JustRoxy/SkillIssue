using osu.Game.Beatmaps;

namespace SkillIssue.Domain.PPC.Entities;

public class BeatmapPerformance
{
    public int BeatmapId { get; set; }
    public int Mods { get; set; }
    public double StarRating { get; set; }

    /// <summary>The difficulty corresponding to the aim skill.</summary>
    public double AimDifficulty { get; set; }

    /// <summary>The difficulty corresponding to the speed skill.</summary>
    public double SpeedDifficulty { get; set; }

    /// <summary>
    ///     The number of clickable objects weighted by difficulty.
    ///     Related to <see cref="P:osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyAttributes.SpeedDifficulty" />
    /// </summary>
    public double SpeedNoteCount { get; set; }

    /// <summary>The difficulty corresponding to the flashlight skill.</summary>
    public double FlashlightDifficulty { get; set; }

    /// <summary>
    ///     Describes how much of <see cref="P:osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyAttributes.AimDifficulty" /> is
    ///     contributed to by hitcircles or sliders.
    ///     A value closer to 1.0 indicates most of
    ///     <see cref="P:osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyAttributes.AimDifficulty" /> is contributed by
    ///     hitcircles.
    ///     A value closer to 0.0 indicates most of
    ///     <see cref="P:osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyAttributes.AimDifficulty" /> is contributed by sliders.
    /// </summary>
    public double SliderFactor { get; set; }

    /// <summary>
    ///     The perceived approach rate inclusive of rate-adjusting mods (DT/HT/etc).
    /// </summary>
    /// <remarks>
    ///     Rate-adjusting mods don't directly affect the approach rate difficulty value, but have a perceived effect as a
    ///     result of adjusting audio timing.
    /// </remarks>
    public float ApproachRate { get; set; }

    /// <summary>
    ///     The perceived overall difficulty inclusive of rate-adjusting mods (DT/HT/etc).
    /// </summary>
    /// <remarks>
    ///     Rate-adjusting mods don't directly affect the overall difficulty value, but have a perceived effect as a result of
    ///     adjusting audio timing.
    /// </remarks>
    public float OverallDifficulty { get; set; }

    /// <summary>
    ///     The beatmap's drain rate. This doesn't scale with rate-adjusting mods.
    /// </summary>
    public double DrainRate { get; set; }

    /// <summary>The number of hitcircles in the beatmap.</summary>
    public int HitCircleCount { get; set; }

    /// <summary>The number of sliders in the beatmap.</summary>
    public int SliderCount { get; set; }

    /// <summary>The number of spinners in the beatmap.</summary>
    public int SpinnerCount { get; set; }

    public int MaxCombo { get; set; }

    public double Bpm { get; set; }

    public float CircleSize { get; set; }
    public double AimDifficultSliderCount { get; set; }

    public double AimDifficultStrainCount { get; set; }

    public double SpeedDifficultStrainCount { get; set; }


    public Beatmap Beatmap { get; set; } = null!;

    public BeatmapDifficulty ConvertToDifficulty()
    {
        return new BeatmapDifficulty
        {
            DrainRate = (float)DrainRate,
            CircleSize = CircleSize,
            OverallDifficulty = OverallDifficulty,
            ApproachRate = ApproachRate,

            //?
            SliderMultiplier = 0,
            SliderTickRate = 0
        };
    }
}