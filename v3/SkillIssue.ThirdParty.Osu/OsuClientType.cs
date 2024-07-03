using SkillIssue.Common;

namespace SkillIssue.ThirdParty.Osu;

public static class OsuClientType
{
    public enum Types
    {
        TGML_CLIENT,
        TGS_CLIENT
    }

    public static readonly HashSet<string> AllowedClients =
        EnumExtensions.GetAllValues<Types, string>(type => type.GetName());

    public static string GetName(this Types type)
    {
        return type switch
        {
            Types.TGML_CLIENT => "TGML",
            Types.TGS_CLIENT => "TGS",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}