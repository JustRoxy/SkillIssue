using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillIssue.Database;
using SkillIssue.Domain.Unfair.Entities;

namespace TheGreatSpy.Services;

public class PlayerService(DatabaseContext context, ILogger<PlayerService> logger, HttpClient client)
{
    private const string UpsertPlayersWithoutStatistics =
        """
        INSERT INTO player(player_id, active_username, country_code, avatar_url, is_restricted)
        VALUES (@PlayerId, @ActiveUsername, @CountryCode, @AvatarUrl, @IsRestricted)
        ON CONFLICT (player_id) DO UPDATE
        SET active_username = excluded.active_username,
            country_code = excluded.country_code,
            avatar_url = excluded.avatar_url,
            is_restricted = excluded.is_restricted
        """;

    private const string UpsertPlayersWithStatistics =
        """
        INSERT INTO player(player_id, active_username, country_code, avatar_url, last_updated, pp, global_rank, country_rank, digit, is_restricted)
        VALUES (@PlayerId, @ActiveUsername, @CountryCode, @AvatarUrl, @LastUpdated, @Pp, @GlobalRank, @CountryRank, @Digit, @IsRestricted)
        ON CONFLICT (player_id) DO UPDATE
        SET active_username = excluded.active_username,
            country_code = excluded.country_code,
            avatar_url = excluded.avatar_url,
            pp = excluded.pp,
            global_rank = excluded.global_rank,
            country_rank = excluded.country_rank,
            digit = excluded.digit,
            last_updated = excluded.last_updated,
            is_restricted = excluded.is_restricted
        """;

    private static readonly SemaphoreSlim UpsertLock = new(1);

    private (Player player, List<string> previousUsernames) ToPlayer(JsonObject playerPayload)
    {
        return (new Player
        {
            PlayerId = playerPayload["id"].Deserialize<int>(),
            ActiveUsername = playerPayload["username"].Deserialize<string>()!,
            CountryCode = playerPayload["country_code"].Deserialize<string>()!,
            AvatarUrl = playerPayload["avatar_url"].Deserialize<string>()!,
            GlobalRank = playerPayload["statistics"]?["global_rank"].Deserialize<int?>(),
            CountryRank = playerPayload["statistics"]?["country_rank"].Deserialize<int?>(),
            Pp = playerPayload["statistics"]?["pp"].Deserialize<double?>(),
            LastUpdated = DateTime.UtcNow,
            IsRestricted = false
        }, playerPayload["previous_usernames"]?.AsArray().Select(x => x.Deserialize<string>()).ToList())!;
    }

    public async Task UpdatePlayerById(int id, CancellationToken stoppingToken)
    {
        async Task SetRestricted()
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(stoppingToken);

            logger.LogInformation("Set as restricted player {PlayerId}", id);
            try
            {
                await UpsertLock.WaitAsync(stoppingToken);
                await connection.ExecuteAsync("UPDATE player SET is_restricted = true WHERE player_id = @Id",
                    new { Id = id });
            }
            finally
            {
                UpsertLock.Release();
            }
        }

        try
        {
            var playerPayload =
                await client.GetFromJsonAsync<JsonObject>($"users/{id}/osu?key=id", stoppingToken);
            if (playerPayload is null)
            {
                await SetRestricted();
                return;
            }

            var player = ToPlayer(playerPayload);

            await UpsertPlayers([player.player],
                player.previousUsernames.Select(x => (player.player.PlayerId, x)).ToList(),
                true);
        }
        catch (HttpRequestException)
        {
            await SetRestricted();
        }
    }

    public async Task<Player?> GetPlayerById(int playerId)
    {
        var player = await context.Players.AsNoTracking().FirstOrDefaultAsync(x => x.PlayerId == playerId);

        if (player?.GlobalRank != 0) return player;

        try
        {
            var playerPayload = await client.GetFromJsonAsync<JsonObject>($"users/{playerId}/osu?key=id");
            if (playerPayload is null) return null;
            (player, var previousUsernames) = ToPlayer(playerPayload);

            await UpsertPlayers([player], previousUsernames.Select(x => (player.PlayerId, x)).ToList(), true);
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e, "Error on GetPlayer({PlayerId})", playerId);
            return null;
        }

        return player;
    }

    public async Task<Player?> GetPlayer(string username)
    {
        var normalizedUsername = Player.NormalizeUsername(username);
        var player =
            await context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Usernames.Any(z => z.NormalizedUsername == normalizedUsername));

        if (player != null) return player;

        try
        {
            var playerPayload = await client.GetFromJsonAsync<JsonObject>($"users/{username}/osu?key=username");
            if (playerPayload is null) return null;
            (player, var previousUsernames) = ToPlayer(playerPayload);

            await UpsertPlayers([player], previousUsernames.Select(x => (player.PlayerId, x)).ToList(), true);
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e, "Error on GetPlayer({Username})", username);
            return null;
        }

        return player;
    }


    public async Task<JsonObject?> GetLeaderboard(string country, int page = 1, CancellationToken token = default)
    {
        return await client.GetFromJsonAsync<JsonObject>(
            $"rankings/osu/performance?country={country}&cursor[page]={page}",
            token);
    }

    public async Task UpsertPlayers(IEnumerable<Player> playersEnum,
        List<(int playerId, string previousUsername)>? previousUsernames = null,
        bool withStatistics = false)
    {
        var players = playersEnum.ToList();
        players.ForEach(x => x.LastUpdated = DateTime.UtcNow);

        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();

        await UpsertLock.WaitAsync();
        var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var sw = Stopwatch.GetTimestamp();
            await connection.ExecuteAsync(withStatistics ? UpsertPlayersWithStatistics : UpsertPlayersWithoutStatistics,
                players);

            await UpsertUsernames(connection, players);
            if (previousUsernames != null && previousUsernames.Count != 0)
                await InsertPreviousUsernames(connection, previousUsernames);

            await transaction.CommitAsync();
            logger.LogInformation("Inserted {PlayerCount} players in {Elapsed:N3}ms", players.Count,
                Stopwatch.GetElapsedTime(sw).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Could not insert into player_username");
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            UpsertLock.Release();
        }
    }

    private async Task InsertPreviousUsernames(DbConnection connection,
        List<(int playerId, string previousUsername)> previousUsernames)
    {
        await connection.ExecuteAsync(
            "INSERT INTO player_username(player_id, normalized_username) VALUES (@PlayerId, @NormalizedUsername) ON CONFLICT DO NOTHING",
            previousUsernames.Distinct().Select(x => new
            {
                PlayerId = x.playerId,
                NormalizedUsername = Player.NormalizeUsername(x.previousUsername)
            }));
    }

    private async Task UpsertUsernames(DbConnection connection, List<Player> players)
    {
        await connection.ExecuteAsync(
            "INSERT INTO player_username(player_id, normalized_username) VALUES (@PlayerId, @NormalizedUsername) ON CONFLICT (normalized_username) DO UPDATE SET player_id = excluded.player_id",
            players.Select(x => new
            {
                x.PlayerId,
                NormalizedUsername = Player.NormalizeUsername(x.ActiveUsername)
            }));
    }
}