using System.Net;
using ComposableAsync;
using Google;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using RateLimiter;
using SkillIssue.Discord.Commands.RatingCommands;

namespace SkillIssue;

public class SpreadsheetProvider(string apiKey)
{
    private const string ApplicationName = "SkillIssue";

    private static readonly TimeLimiter TimeLimiter =
        TimeLimiter.GetFromMaxCountByInterval(60, TimeSpan.FromMinutes(1));

    private readonly SheetsService _service = new(new BaseClientService.Initializer
    {
        ApiKey = apiKey,
        ApplicationName = ApplicationName
    });

    private async Task<T> Retry<T>(Func<Task<T>> method)
    {
        var retries = 0;
        while (true)
            try
            {
                await TimeLimiter;
                return await method.Invoke();
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode is HttpStatusCode.TooManyRequests)
            {
                retries++;
                Console.WriteLine($"Retry attempt {retries} due to HTTP {ex.HttpStatusCode} error... ({ex.Message})");

                var retryTime = Math.Min(Math.Pow(2, retries) * 1000 + Random.Shared.Next(0, 1000),
                    64000);
                await Task.Delay((int)retryTime);
            }
            catch (Exception ex)
            {
                throw new UserInteractionException(ex.Message);
            }
    }

    public async Task<Dictionary<string, List<string>>> ExtractTeams(string spreadsheetId, string table,
        string columns, int skipRowsToNextTeam = 1)
    {
        var values = await Retry(() =>
            _service.Spreadsheets.Values.Get(spreadsheetId, $"{table}!{columns}").ExecuteAsync());
        if (values?.Values is null)
            throw new UserInteractionException($"No values had been found at {table}!{columns}");

        Dictionary<string, List<string>> teams = new();

        List<int> indexes = new();
        for (var i = 0; i < values.Values.Count; i++)
        {
            var first = values.Values[i].OfType<string>().FirstOrDefault(x => !string.IsNullOrEmpty(x));
            if (first is null) continue;

            indexes.Add(values.Values[i].IndexOf(first));
        }

        if (indexes.Count == 0)
            throw new UserInteractionException("Found no text in the selected columns");

        var minIndex = indexes.Min();

        for (var i = 0; i < values.Values.Count; i += skipRowsToNextTeam)
        {
            var teamName = values.Values[i][minIndex].ToString();
            if (teamName is null || string.IsNullOrEmpty(teamName)) continue;

            teams[teamName] = [];

            for (var j = minIndex + 1; j < values.Values[i].Count; j++)
            {
                var value = values.Values[i][j];
                if (value is not string s || string.IsNullOrEmpty(s)) continue;

                teams[teamName].Add(s);
            }
        }

        return teams;
    }

    public async Task<List<string>> ExtractUsername(string spreadsheetId, string table, string columns)
    {
        var extractedUsernames = new List<string>();
        var values = await Retry(() =>
            _service.Spreadsheets.Values.Get(spreadsheetId, $"{table}!{columns}").ExecuteAsync());
        if (values?.Values is null)
            throw new UserInteractionException($"No values had been found at {table}!{columns}");

        foreach (var value in values.Values.SelectMany(x => x).OfType<string>())
            extractedUsernames.AddRange(value.Split(","));

        return extractedUsernames;
    }
}