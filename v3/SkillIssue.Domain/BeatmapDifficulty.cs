namespace SkillIssue.Domain;

public class BeatmapDifficulty
{
    public long BeatmapId { get; set; }
    public IMods Mods { get; set; } = default!;


    /// <summary>
    /// Star rating of the map.
    /// </summary>
    public double StarRating { get; set; }

    /// <summary>
    /// Beats per minute of the map.
    /// </summary>
    public int Bpm { get; set; }

    /// <summary>
    /// The size of the circles in a beatmap.
    /// </summary>
    public double CircleSize { get; set; }

    /// <summary>The difficulty corresponding to the aim skill.</summary>
    public double AimDifficulty { get; set; }

    /// <summary>The difficulty corresponding to the speed skill.</summary>
    public double SpeedDifficulty { get; set; }

    /// <summary>
    /// The number of clickable objects weighted by difficulty.
    /// Related to <see cref="P:osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyAttributes.SpeedDifficulty" />
    /// </summary>
    public double SpeedNoteCount { get; set; }

    /// <summary>The difficulty corresponding to the flashlight skill.</summary>
    public double FlashlightDifficulty { get; set; }

    /// <summary>
    /// Describes how much of <see cref="P:osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyAttributes.AimDifficulty" /> is contributed to by hitcircles or sliders.
    /// A value closer to 1.0 indicates most of <see cref="P:osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyAttributes.AimDifficulty" /> is contributed by hitcircles.
    /// A value closer to 0.0 indicates most of <see cref="P:osu.Game.Rulesets.Osu.Difficulty.OsuDifficultyAttributes.AimDifficulty" /> is contributed by sliders.
    /// </summary>
    public double SliderFactor { get; set; }

    /// <summary>
    /// The perceived approach rate inclusive of rate-adjusting mods (DT/HT/etc).
    /// </summary>
    /// <remarks>
    /// Rate-adjusting mods don't directly affect the approach rate difficulty value, but have a perceived effect as a result of adjusting audio timing.
    /// </remarks>
    public double ApproachRate { get; set; }

    /// <summary>
    /// The perceived overall difficulty inclusive of rate-adjusting mods (DT/HT/etc).
    /// </summary>
    /// <remarks>
    /// Rate-adjusting mods don't directly affect the overall difficulty value, but have a perceived effect as a result of adjusting audio timing.
    /// </remarks>
    public double OverallDifficulty { get; set; }

    /// <summary>
    /// The beatmap's drain rate. This doesn't scale with rate-adjusting mods.
    /// </summary>
    public double DrainRate { get; set; }

    public int MaxCombo { get; set; }
}