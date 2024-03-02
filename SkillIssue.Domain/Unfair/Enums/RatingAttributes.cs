// ReSharper disable InconsistentNaming

namespace SkillIssue.Domain.Unfair.Enums;

public enum ModificationRatingAttribute : short
{
    AllMods,
    NM,
    HD,
    HR,
    DT
}

public enum SkillsetRatingAttribute : short
{
    Overall,
    Aim,
    Tapping,
    Technical,
    LowAR,
    HighAR,
    HighBpm,
    Precision
}

public enum ScoringRatingAttribute : short
{
    Score,
    Combo,
    Accuracy,
    PP
}