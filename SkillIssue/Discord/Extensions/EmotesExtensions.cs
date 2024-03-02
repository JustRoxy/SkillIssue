using Discord;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;

namespace SkillIssue.Discord.Extensions;

public static class EmotesExtensions
{
    public static readonly Emote RankedEmoji = "<:ranked:1102128270545272892>";
    public static readonly Emote StarRatingEmoji = "<:SR:1208371967116181515>";
    public static readonly Emote AimEmoji = "<:aim:1102212779269701694>";
    public static readonly Emote DoubleTimeEmoji = "<:doubletime:1102096043119755265>";
    public static readonly Emote HardRockEmoji = "<:hardrock:1102096039999193128>";
    public static readonly Emote HiddenEmoji = "<:hidden:1102096038266945606>";
    public static readonly Emote HighBpmEmoji = "<:highBPM:1102203809004011571>";
    public static readonly Emote NomodEmoji = "<:nomod:1102095000608722994>";
    public static readonly Emote OverallEmoji = "<:overall:1208368189696643114>";
    public static readonly Emote PrecisionEmoji = "<:precision:1208365783844130848>";
    public static readonly Emote LowArEmoji = "<:reading:1102212067588591716>";
    public static readonly Emoji HighArEmoji = ":nerd:";
    public static readonly Emote TappingEmoji = "<:tapping:1208366935029321768>";
    public static readonly Emote TechnicalEmoji = "<:technical:1208366057736245288>";

    public static string ToEmote(this RatingAttribute ratingAttribute)
    {
        if (ratingAttribute.Skillset == SkillsetRatingAttribute.HighAR) return ratingAttribute.Skillset.ToEmote();
        if (ratingAttribute.Modification == ModificationRatingAttribute.AllMods)
            return ratingAttribute.Skillset.ToEmote();
        return ratingAttribute.Modification.ToEmote();
    }

    public static string ToEmote(this ModificationRatingAttribute modificationRatingAttribute)
    {
        return modificationRatingAttribute switch
        {
            ModificationRatingAttribute.AllMods => RankedEmoji.ToString(),
            ModificationRatingAttribute.HR => HardRockEmoji.ToString(),
            ModificationRatingAttribute.DT => DoubleTimeEmoji.ToString(),
            ModificationRatingAttribute.HD => HiddenEmoji.ToString(),
            ModificationRatingAttribute.NM => NomodEmoji.ToString(),

            _ => throw new ArgumentOutOfRangeException(nameof(modificationRatingAttribute), modificationRatingAttribute,
                null)
        };
    }

    public static string ToEmote(this SkillsetRatingAttribute skillsetRatingAttribute)
    {
        return skillsetRatingAttribute switch
        {
            SkillsetRatingAttribute.Tapping => TappingEmoji.ToString(),
            SkillsetRatingAttribute.LowAR => LowArEmoji.ToString(),
            SkillsetRatingAttribute.Aim => AimEmoji.ToString(),
            SkillsetRatingAttribute.Overall => OverallEmoji.ToString(),
            SkillsetRatingAttribute.Technical => TechnicalEmoji.ToString(),
            SkillsetRatingAttribute.HighBpm => HighBpmEmoji.ToString(),
            SkillsetRatingAttribute.Precision => PrecisionEmoji.ToString(),
            SkillsetRatingAttribute.HighAR => HighArEmoji.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(skillsetRatingAttribute), skillsetRatingAttribute,
                null)
        };
    }
}