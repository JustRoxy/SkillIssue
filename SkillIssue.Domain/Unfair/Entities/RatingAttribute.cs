using SkillIssue.Domain.Unfair.Enums;

namespace SkillIssue.Domain.Unfair.Entities;

public class RatingAttribute
{
    public static readonly IReadOnlyList<int> MajorAttributes =
        GetAllAttributes().Where(x => x.IsMajor).Select(x => x.AttributeId).ToList();

    public int AttributeId { get; set; }

    public ModificationRatingAttribute Modification { get; set; }
    public SkillsetRatingAttribute Skillset { get; set; }
    public ScoringRatingAttribute Scoring { get; set; }

    public bool IsValid => UsableRatingAttribute(Modification, Skillset);
    public bool IsMajor => IsMajorAttribute(Modification, Skillset);

    public string Description =>
        $"{DescriptionFormat(Modification)} ({DescriptionFormat(Skillset)}) [{DescriptionFormat(Scoring)}]";

    private static int ModificationCount => Enum.GetValues<ModificationRatingAttribute>().Length;
    private static int SkillsetCount => Enum.GetValues<SkillsetRatingAttribute>().Length;
    private static int ScoringCount => Enum.GetValues<ScoringRatingAttribute>().Length;

    public static ScoringRatingAttribute[] UnfairScoring { get; } = Enum.GetValues<ScoringRatingAttribute>()
        .Where(x => x != ScoringRatingAttribute.PP).ToArray();

    public static bool IsMajorAttribute(ModificationRatingAttribute modification, SkillsetRatingAttribute skillset)
    {
        if (!UsableRatingAttribute(modification, skillset)) return false;

        if (skillset == SkillsetRatingAttribute.HighAR && modification == ModificationRatingAttribute.DT) return true;

        if (modification == ModificationRatingAttribute.AllMods ||
            skillset == SkillsetRatingAttribute.Overall) return true;

        return false;
    }

    public static string DescriptionFormat(ModificationRatingAttribute modification)
    {
        return modification switch
        {
            ModificationRatingAttribute.AllMods => "All Mods",
            ModificationRatingAttribute.NM => "NoMod",
            ModificationRatingAttribute.HD => "Hidden",
            ModificationRatingAttribute.HR => "Hard Rock",
            ModificationRatingAttribute.DT => "Double Time",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static string DescriptionFormat(SkillsetRatingAttribute skillset)
    {
        return skillset switch
        {
            SkillsetRatingAttribute.Overall => "Overall",
            SkillsetRatingAttribute.Aim => "Aim",
            SkillsetRatingAttribute.Tapping => "Tapping",
            SkillsetRatingAttribute.LowAR => "Low AR",
            SkillsetRatingAttribute.HighAR => "High AR",
            SkillsetRatingAttribute.HighBpm => "High BPM",
            SkillsetRatingAttribute.Precision => "Precision",
            SkillsetRatingAttribute.Technical => "Technical",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static string DescriptionFormat(ScoringRatingAttribute scoring)
    {
        return scoring switch
        {
            ScoringRatingAttribute.Score => "Score",
            ScoringRatingAttribute.Combo => "Combo",
            ScoringRatingAttribute.Accuracy => "Accuracy",
            ScoringRatingAttribute.PP => "PP",
            _ => throw new ArgumentOutOfRangeException()
        };
    }


    public static bool UsableRatingAttribute(ModificationRatingAttribute modification, SkillsetRatingAttribute skillset)
    {
        //High AR can only be in double time
        if (skillset == SkillsetRatingAttribute.HighAR) return modification == ModificationRatingAttribute.DT;

        //Low AR can only be in AllMods, NM, HD
        if (skillset == SkillsetRatingAttribute.LowAR)
            return modification is not (ModificationRatingAttribute.DT or ModificationRatingAttribute.HR);

        //Precision can't be on HD or DT
        if (skillset == SkillsetRatingAttribute.Precision)
            return modification is not (ModificationRatingAttribute.HD or ModificationRatingAttribute.DT);

        return true;
    }

    public static bool UsableRatingAttribute(RatingAttribute attribute)
    {
        return UsableRatingAttribute(attribute.Modification, attribute.Skillset);
    }

    public static IEnumerable<RatingAttribute> GetAllUsableAttributes() =>
        GetAllAttributes().Where(UsableRatingAttribute);

    public static IEnumerable<RatingAttribute> GetAllAttributes()
    {
        return
            from mod in Enum.GetValues<ModificationRatingAttribute>()
            from skillset in Enum.GetValues<SkillsetRatingAttribute>()
            from scoring in Enum.GetValues<ScoringRatingAttribute>()
            select new RatingAttribute
            {
                AttributeId = GetAttributeId(mod, skillset, scoring),
                Modification = mod,
                Skillset = skillset,
                Scoring = scoring
            };
    }

    public static (ModificationRatingAttribute modification, SkillsetRatingAttribute skillset, ScoringRatingAttribute
        scoring)
        GetAttributesFromId(int attributeId)
    {
        var scoringValue = attributeId % ScoringCount;
        var skillsetValue = attributeId / ScoringCount % SkillsetCount;
        var modificationValue = attributeId / (SkillsetCount * ScoringCount);

        return ((ModificationRatingAttribute)modificationValue, (SkillsetRatingAttribute)skillsetValue,
            (ScoringRatingAttribute)scoringValue);
    }

    public static RatingAttribute GetAttribute(int id)
    {
        var (modification, skillset, scoring) = GetAttributesFromId(id);
        return new RatingAttribute
        {
            AttributeId = id,
            Modification = modification,
            Skillset = skillset,
            Scoring = scoring
        };
    }

    public static RatingAttribute GetAttribute(ModificationRatingAttribute modification,
        SkillsetRatingAttribute skillset,
        ScoringRatingAttribute scoring)
    {
        return new RatingAttribute
        {
            AttributeId = GetAttributeId(modification, skillset, scoring),
            Modification = modification,
            Skillset = skillset,
            Scoring = scoring
        };
    }

    public static int GetAttributeId(ModificationRatingAttribute modification,
        SkillsetRatingAttribute skillset,
        ScoringRatingAttribute scoring)
    {
        var modificationValue = (int)modification;
        var skillsetValue = (int)skillset;
        var scoringValue = (int)scoring;

        var attributeId = modificationValue * SkillsetCount * ScoringCount
                          + skillsetValue * ScoringCount
                          + scoringValue;
        return attributeId;
    }

    public static bool IsAttributeSet(int attributeId, ModificationRatingAttribute modificationRatingAttribute)
    {
        var modificationValue = attributeId / (SkillsetCount * ScoringCount);

        return modificationValue == (int)modificationRatingAttribute;
    }

    public static bool IsAttributeSet(int attributeId, SkillsetRatingAttribute skillsetRatingAttribute)
    {
        var skillsetValue = attributeId / ScoringCount % SkillsetCount;

        return skillsetValue == (int)skillsetRatingAttribute;
    }

    public static bool IsAttributeSet(int attributeId, ScoringRatingAttribute scoringRatingAttribute)
    {
        var scoringValue = attributeId % ScoringCount;

        return scoringValue == (int)scoringRatingAttribute;
    }

    public static string GetCsvHeaderValue(RatingAttribute attribute)
    {
        var modification = attribute.Modification switch
        {
            ModificationRatingAttribute.AllMods => "",
            ModificationRatingAttribute.NM => "nm.",
            ModificationRatingAttribute.HD => "hd.",
            ModificationRatingAttribute.HR => "hr.",
            ModificationRatingAttribute.DT => "dt.",
            _ => throw new ArgumentOutOfRangeException()
        };

        var skillset = attribute.Skillset switch
        {
            SkillsetRatingAttribute.Overall => "",
            SkillsetRatingAttribute.Aim => "aim.",
            SkillsetRatingAttribute.Tapping => "tapping.",
            SkillsetRatingAttribute.Technical => "tech.",
            SkillsetRatingAttribute.LowAR => "lowar.",
            SkillsetRatingAttribute.HighBpm => "highbpm.",
            SkillsetRatingAttribute.Precision => "precision.",
            SkillsetRatingAttribute.HighAR => "highar.",
            _ => throw new ArgumentOutOfRangeException()
        };

        var scoring = attribute.Scoring switch
        {
            ScoringRatingAttribute.Score => "",
            ScoringRatingAttribute.Combo => "combo",
            ScoringRatingAttribute.Accuracy => "acc",
            ScoringRatingAttribute.PP => "pp",
            _ => throw new ArgumentOutOfRangeException()
        };

        return $"{modification}{skillset}{scoring}";
    }
}