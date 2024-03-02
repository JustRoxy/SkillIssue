using System.Text;

namespace SkillIssue.Domain.Unfair.Entities;

[Flags]
public enum CalculationErrorFlag
{
    NoError = 0,
    NameRegexFailed = 1,
    NotStandardMatchType = 2,
    BannedAcronym = 4,
    TooManyWarmups = 8,
    TooManyHosts = 16,
    NoStandardScores = 32,
    InGameHostedMatch = 64,
    BigHeadOnHeadGame = 128,
    NonSymmetricalTeams = 256,
    TooManyPlayers = 512,
    InsufficientAmountOfGames = 1024,
    TooManyGames = 2048,
    IncorrectAmountOfPlayers = 4096
}

public class CalculationError
{
    private readonly StringBuilder _errorBuilder = new();
    public int MatchId { get; set; }

    public CalculationErrorFlag Flags { get; set; } = CalculationErrorFlag.NoError;
    public string? CalculationErrorLog { get; set; }

    public void AddError(CalculationErrorFlag error, string errorMessage)
    {
        Flags |= error;
        _errorBuilder.Append($"{error.ToString()}: {errorMessage};");
        CalculationErrorLog = _errorBuilder.ToString();
    }
}