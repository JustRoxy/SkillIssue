using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SkillIssue.Domain.TGML.Entities;
using TheGreatMultiplayerLibrary.Entities;

namespace TheGreatMultiplayerLibrary.Services;

public class TheGreatArchiver(HttpClient client, ILogger<TheGreatArchiver> logger)
{
    public async Task<List<TgmlMatch>?> GetPage(int startingFrom)
    {
        var historyMatches =
            (await client.GetFromJsonAsync<JsonObject>($"matches?sort=id_asc&cursor[match_id]={startingFrom}"))?
            ["matches"]?.Deserialize<List<HistoryMatch>>();
        if (historyMatches is null)
        {
            logger.LogError("Could not find history page starting from {StartingFrom}", startingFrom);
            return null;
        }

        var matches = historyMatches.Select(x => new TgmlMatch
        {
            MatchId = x.Id,
            Name = x.Name,
            StartTime = x.StartTime.ToUniversalTime(),
            EndTime = x.EndTime?.ToUniversalTime(),
            MatchStatus = TgmlMatchStatus.Ongoing,
            CompressedJson = null
        }).ToList();

        return matches;
    }

    private async Task<JsonObject?> GetMatchByCursorAfter(int matchId, long cursor)
    {
        var response = await client.GetAsync($"matches/{matchId}?after={cursor}");

        try
        {
            return await response.Content.ReadFromJsonAsync<JsonObject?>();
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to deserialize following content: {OriginalResponse}", await response.Content.ReadAsStringAsync());
            
            throw;
        }
    }

    public async Task<JsonObject?> GetMatchRoot(int matchId)
    {
        return BeforeOngoingGame(await GetMatchByCursorAfter(matchId, 0));
    }

    private JsonObject Merge(JsonObject left, JsonObject right)
    {
        if (left["events"] is not null && right["events"] is null) return left;
        if (left["events"] is null && right["events"] is not null) return right;

        //Validate left merge object
        var leftUsers = left["users"]?.AsArray();
        var leftEvents = left["events"]?.AsArray();
        if (leftUsers is null) LogAndThrow(nameof(leftUsers), left);
        if (leftEvents is null) LogAndThrow(nameof(leftEvents), left);

        //Validate right merge object
        var rightUsers = right["users"]?.AsArray();
        if (rightUsers is null) LogAndThrow(nameof(rightUsers), right);
        var rightEvents = right["events"]?.AsArray();
        if (rightEvents is null) LogAndThrow(nameof(rightEvents), right);


        if (rightEvents.Count == 0) return left;
        if (leftEvents.Count == 0) return right;

        //Validate event index progression from left to right
        //TODO: but what if rightEvents.Min in the middle of leftEvents ://///
        if (FindFirstEventId(right) < FindLastEventId(left))
        {
            (leftEvents, rightEvents) = (rightEvents, leftEvents);
            (leftUsers, rightUsers) = (rightUsers, leftUsers);
        }

        //Select one side to merge users
        var leftUsersIds = leftUsers.Select(x => x!["id"].Deserialize<long>()).ToHashSet();

        foreach (var rightUser in rightUsers)
        {
            if (leftUsersIds.Contains(rightUser!["id"].Deserialize<long>())) continue;
            leftUsers.Add(rightUser.DeepClone());
        }


        foreach (var @event in rightEvents) leftEvents.Add(@event!.DeepClone());

        left["match"] = right["match"]!.DeepClone();
        left["latest_event_id"] = right["latest_event_id"]!.DeepClone();
        left["current_game_id"] = right["current_game_id"]?.DeepClone();
        return left;
    }

    public async Task<JsonObject> GetMatchAfterProbe(int matchId, JsonObject previous)
    {
        var nextNode = await GetMatchByCursorAfter(matchId, FindLastEventId(previous));
        if (nextNode?["events"]?.AsArray().Count == 0) return previous;

        if (nextNode is null) LogAndThrow(nameof(nextNode), previous);

        return BeforeOngoingGame(Merge(previous, nextNode))!;
    }


    public async Task<JsonObject> GetMatchAfter(int matchId, JsonObject previous)
    {
        var nextNode = await GetMatchByCursorAfter(matchId, FindLastEventId(previous));
        if (nextNode is null) LogAndThrow(nameof(nextNode), previous);

        return BeforeOngoingGame(Merge(previous, nextNode))!;
    }

    public long? FindOngoingGame(JsonObject jsonObject)
    {
        if (jsonObject["match"]?["end_time"]?.Deserialize<DateTime?>() != null) return null;


        var currentGameId = jsonObject["current_game_id"].Deserialize<long?>();
        if (currentGameId is null) return null;

        var ongoingGameEvent = jsonObject["events"]!.AsArray()
            .Where(x => x?["game"] != null)
            .Where(x => x!["game"]!["end_time"].Deserialize<DateTime?>() == null)
            .ToList();

        return ongoingGameEvent.FirstOrDefault(x => x!["game"]!["id"].Deserialize<long>() == currentGameId)?["id"]
            .Deserialize<long>();
    }

    public JsonObject? BeforeOngoingGame(JsonObject? previous)
    {
        if (previous is null) return previous;

        var ongoingGame = FindOngoingGame(previous);
        if (ongoingGame is null) return previous;

        var events = previous["events"]!.AsArray();
        Trace.Assert(events.Count != 0, "events.Count != 0");

        foreach (var afterNode in events.Where(x => x!["id"].Deserialize<long>() >= ongoingGame).ToList())
            events.Remove(afterNode);

        previous["latest_event_id"] = FindLastEventId(previous);
        return previous;
    }

    private long FindFirstEventId(JsonObject jsonObject)
    {
        var events = jsonObject["events"]?.AsArray();
        if (events is null) LogAndThrow(nameof(events), jsonObject);
        if (events.Count == 0) LogAndThrow("eventIds", jsonObject);

        return events[0]!["id"].Deserialize<long>();
    }

    private long FindLastEventId(JsonObject jsonObject)
    {
        var events = jsonObject["events"]?.AsArray();
        if (events is null) LogAndThrow(nameof(events), jsonObject);
        if (events.Count == 0) LogAndThrow("eventIds", jsonObject);

        return events[^1]!["id"].Deserialize<long>();
    }

    public bool HasMatchNodeAfter(JsonObject jsonObject)
    {
        var lastEventId = jsonObject["latest_event_id"]?.Deserialize<long>();
        if (lastEventId is null) LogAndThrow(nameof(lastEventId), jsonObject);

        return FindLastEventId(jsonObject) < lastEventId;
    }

    [DoesNotReturn]
    private void LogAndThrow(string propertyName, JsonObject jsonObject)
    {
        logger.LogCritical("Could not locate '{Property}' property in json '{@Json}", propertyName,
            jsonObject.ToString());
        throw new Exception();
    }
}