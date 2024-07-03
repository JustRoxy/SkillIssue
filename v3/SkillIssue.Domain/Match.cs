namespace SkillIssue.Domain;

public class Match
{
    public long MatchId { get; set; }
    public string Name { get; set; } = "";
    public Status MatchStatus { get; set; } = Status.Unknown;
    public bool IsTournament { get; set; }

    public byte[]? Content { get; set; } = null;

    #region Time tracking

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    #endregion

    #region Status

    public enum Status
    {
        Unknown = 0,
        InProgress = 10,
        Completed = 20,
        BeatmapsUpdated = 30,
        PerformancePointsCalculated = 40,
        MetadataValidationFailed = 50, //FAILED, END.
        GameValidationFailed = 60, //FAILED, END.
        RatingsCalculatedSuccessfully = 70 //SUCCEDDED, END.
    }

    #endregion
}