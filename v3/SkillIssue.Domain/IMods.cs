namespace SkillIssue.Domain;

/// <summary>
///     Should be owned by one project. 
/// </summary>
public interface IMods
{
    // Mod is a something that could be saved to a database
    public object ToDatabase();

    public static abstract IMods FromLegacy(int legacyMods);
}