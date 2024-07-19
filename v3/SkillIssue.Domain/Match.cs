namespace SkillIssue.Domain;

public class Match
{
    public int MatchId { get; set; }
    public string Name { get; set; } = "";
    public Status MatchStatus { get; set; } = Status.Unknown;
    public bool IsTournament { get; set; }
    public long? Cursor { get; set; } = null;

    #region Time tracking

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset LastEventTimestamp { get; set; }

    #endregion

    #region Status

    public enum Status
    {
        Unknown = 0,
        InProgress = 10,
        Completed = 20,
        DataExtracted = 30,
        DataUpdated = 40,
        MetadataValidationFailed = 50, //FAILED, END.
        DataMerged = 60,
        GameValidationFailed = 70, //FAILED, END.
        RatingsCalculatedSuccessfully = 80 //SUCCEDDED, END.
    }

    #endregion
}