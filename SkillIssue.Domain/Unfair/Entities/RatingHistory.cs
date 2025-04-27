using System.ComponentModel.DataAnnotations.Schema;

namespace SkillIssue.Domain.Unfair.Entities;

public class RatingHistory
{
    public long GameId { get; set; }
    public int PlayerId { get; set; }
    public int RatingAttributeId { get; set; }

    public int MatchId { get; set; }

    public float OldMu { get; set; }
    public float NewMu { get; set; }
    [NotMapped] public float MuDelta => NewMu - OldMu;

    public float OldSigma { get; set; }
    public float NewSigma { get; set; }
    [NotMapped] public float SigmaDelta => NewSigma - OldSigma;

    public float NewStarRating { get; set; }
    public float OldStarRating { get; set; }
    [NotMapped] public float StarRatingDelta => NewStarRating - OldStarRating;

    public short NewOrdinal { get; set; }
    public short OldOrdinal { get; set; }

    [NotMapped] public int OrdinalDelta => NewOrdinal - OldOrdinal;
    public byte Rank { get; set; }
    public byte PredictedRank { get; set; }
    public PlayerHistory PlayerHistory { get; set; } = null!;
    public RatingAttribute RatingAttribute { get; set; } = null!;
    public TournamentMatch Match { get; set; } = null!;
    public Score Score { get; set; } = null!;
}