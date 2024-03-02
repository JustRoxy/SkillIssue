using System.ComponentModel.DataAnnotations.Schema;
using osu.Game.Beatmaps.Legacy;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.PPC.Entities;

namespace SkillIssue.Domain.Unfair.Entities;

public enum ScoringType : byte
{
    ScoreV2,
    Accuracy,
    Combo,
    Score
}

public class Score
{
    public int MatchId { get; set; }
    public long GameId { get; set; }
    public int PlayerId { get; set; }

    public int? BeatmapId { get; set; }

    public ScoringType ScoringType { get; set; }
    public int TeamSide { get; set; }
    public int TotalScore { get; set; }

    public double Accuracy { get; set; }
    public int MaxCombo { get; set; }

    public int Count300 { get; set; }
    public int Count100 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }

    public LegacyMods LegacyMods { get; set; }
    public double? Pp { get; set; }

    [NotMapped] public int PerformanceLegacyMods => (int)LegacyMods.NormalizeToPerformance();

    public Player Player { get; set; } = null!;
    public TournamentMatch Match { get; set; } = null!;
    public Beatmap? Beatmap { get; set; }
}