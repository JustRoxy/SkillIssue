using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillIssue.Database;
using SkillIssue.Domain.TGML.Entities;
using TheGreatMultiplayerLibrary.Services;

namespace TheGreatMultiplayerLibrary;

public class AlphaMigration(DatabaseContext context, ILogger<AlphaMigration> logger, TheGreatArchiver theGreatArchiver)
{
    public async Task NullGames()
    {
        var from = DateTime.SpecifyKind(new DateTime(2024, 01, 17), DateTimeKind.Utc);
        var games = context.TgmlMatches
            .OrderBy(x => x.MatchId)
            .Where(x => x.CompressedJson != null)
            // .Where(x => x.MatchId > 112365007)
            .Where(x => x.StartTime > from)
            // .Where(x => x.StartTime > to)
            .AsAsyncEnumerable();

        List<(string url, int gamecount, int hits)> foundHits = [];
        var xx = 0;
        await foreach (var game in games)
        {
            ++xx;
            if (xx % 10000 == 0) Console.WriteLine($"Processed: {xx}");

            var decompress = await game.Deserialize();
            var gamedGames = decompress!["events"]?.AsArray()
                .Where(x => x?["game"] != null)
                .Where(x => x!["game"]!["end_time"]?.Deserialize<DateTime?>() != null)
                .Select(x => x!["game"]["id"].Deserialize<long>())
                .ToList();

            var notFound = decompress["events"].AsArray()
                .Where(x => x?["game"]?["id"] != null)
                .Where(x => x!["game"]!["end_time"].Deserialize<DateTime?>() == null)
                .Where(x => !gamedGames!.Contains(x["id"].Deserialize<long>()))
                .ToList();

            if (gamedGames.Count == 0) continue;

            var gamecount = decompress!["events"]?.AsArray()
                .Count(x => x?["game"] != null);
            if (notFound.Count == 0) continue;

            game.EndTime = null;
            game.MatchStatus = TgmlMatchStatus.Ongoing;
            game.CompressedJson = null;
            foundHits.Add(($"{game.StartTime}: https://osu.ppy.sh/mp/{game.MatchId}", gamedGames.Count,
                notFound.Count));

            //
            // decompress = theGreatArchiver.BeforeOngoingGame(decompress!);
            // await game.Serialize(decompress);
            // game.MatchStatus = TgmlMatchStatus.Ongoing;
        }

        await context.SaveChangesAsync();

        var sb = new StringBuilder();
        foreach (var (url, gamecount, hits) in foundHits)
        {
            sb.AppendLine($"{url}: {gamecount} | {hits}");

            Console.WriteLine($"{url}: {gamecount} | {hits}");
        }

        await File.WriteAllTextAsync("hits.txt", sb.ToString());

        Console.WriteLine(foundHits.Count);
    }
    // private T LogAndThrow<T>(string message, params object[] param)
    // {
    //     // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
    //     logger.LogCritical(message, param);
    //     throw new Exception(string.Format(message, param));
    // }
    //
    // public async Task FixBrokenEndTimes()
    // {
    //     int count = 0;
    //     int xx = 0;
    //     var matches = context.TgmlMatches.OrderBy(x => x.MatchId)
    //         .Where(x => x.EndTime != null && x.CompressedJson != null);
    //     await foreach (var match in matches.AsAsyncEnumerable())
    //     {
    //         ++xx;
    //         logger.LogInformation("Processing: {Amount}, Bugged: {Count}", xx, count);
    //         var decompressed = JsonSerializer.Deserialize<JsonObject>(await Decompress(match.CompressedJson!));
    //         if (decompressed?["match"]?["end_time"]?.Deserialize<DateTime>() is not null) continue;
    //
    //         match.EndTime = null;
    //         count++;
    //     }
    //
    //     logger.LogInformation("Bugged: {Amount} matches", count);
    //     await context.SaveChangesAsync();
    // }
    //
    // public async Task CheckEventUsersConsistency()
    // {
    //     int count = 0;
    //     var matches = context.TgmlMatches
    //         .AsSplitQuery()
    //         .OrderBy(x => x.MatchId)
    //         .Where(x => x.MatchId > 106747470)
    //         .Where(x => x.CompressedJson != null)
    //         .Include(x => x.Players);
    //
    //     int xx = 0;
    //     await foreach (var match in matches.AsAsyncEnumerable())
    //     {
    //         xx++;
    //         if (xx % 1000 == 0) logger.LogInformation("Processed: {Amount}", xx);
    //
    //         var players = match.Players.Select(x => x.PlayerId).ToHashSet();
    //         var events = JsonSerializer.Deserialize<JsonObject>(await Decompress(match.CompressedJson!))!["events"]!
    //             .AsArray();
    //
    //         var eventPlayers = events.Select(x => x?["user_id"])
    //             .Where(x => x is not null)
    //             .Select(x => x.Deserialize<int>())
    //             .Where(x => x != 0)
    //             .Distinct()
    //             .ToList();
    //
    //         var nonExisting = eventPlayers.Where(x => !players.Contains(x)).ToList();
    //         if (nonExisting.Count == 0) continue;
    //
    //         count++;
    //         match.EndTime = null;
    //         match.CompressedJson = null;
    //         logger.LogError("Couldn't find in {Name} ({Id}) players: {Players}", match.Name, match.MatchId,
    //             nonExisting);
    //     }
    //
    //     logger.LogInformation("Bugged: {Amount} matches", count);
    //     await context.SaveChangesAsync();
    //     context.ChangeTracker.Clear();
    // }
    //
    // private async Task<TgmlMatch?> GetMatch(string content, int id)
    // {
    //     using var compressionInputStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
    //     using var compressionOutputStream = new MemoryStream();
    //     await using (var compressor = new BrotliStream(compressionOutputStream, CompressionLevel.SmallestSize))
    //     {
    //         await compressionInputStream.CopyToAsync(compressor);
    //     }
    //
    //     var compressedBytes = compressionOutputStream.ToArray();
    //
    //     JsonObject? json;
    //     try
    //     {
    //         json = JsonSerializer.Deserialize<JsonObject>(content);
    //     }
    //     catch (Exception)
    //     {
    //         logger.LogCritical("Could not deserialize file {File}", id);
    //         return null;
    //     }
    //
    //     if (json is null)
    //     {
    //         logger.LogCritical("Unable to parse file json {File}", id);
    //         return null;
    //     }
    //
    //     var usersArray = json["users"]?.AsArray();
    //     if (usersArray is null)
    //     {
    //         logger.LogCritical("Unable to find 'Users' in {File}", id);
    //         return null;
    //     }
    //
    //     var users = usersArray.Select(x => new TgmlPlayer
    //     {
    //         PlayerId = x?["id"]?.Deserialize<int>() ??
    //                    LogAndThrow<int>("Unable to find property [id] int 'User' at file {File}", id),
    //         CurrentUsername = x?["username"]?.Deserialize<string>() ??
    //                           LogAndThrow<string>("Unable to find property [username] int 'User' at file {File}",
    //                               id)
    //     }).ToList();
    //
    //     var match = new TgmlMatch
    //     {
    //         MatchId = id,
    //         Name = json["match"]?["name"].Deserialize<string>() ??
    //                LogAndThrow<string>("Match name for file {File} does not exist", id),
    //         StartTime = (json["match"]?["start_time"].Deserialize<DateTime>() ??
    //                      LogAndThrow<DateTime>("Unable to parse start_time",
    //                          id)).ToUniversalTime(),
    //         CompressedJson = compressedBytes
    //     };
    //
    //     match.Players = users;
    //
    //     return match;
    // }
    //
    // public async Task Migrate(string matchesPath)
    // {
    //     var migrated = (await context.Matches.AsNoTracking().Select(x => x.MatchId).ToListAsync()).ToHashSet();
    //
    //     var files = Directory.GetFiles(matchesPath)
    //         .Where(x => !string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(x)))
    //         .Select((x, i) => new
    //             { MatchId = int.Parse(Path.GetFileNameWithoutExtension(x)), Path = x, Index = i })
    //         .Where(x => !migrated.Contains(x.MatchId))
    //         .OrderBy(x => x.MatchId)
    //         .ToList();
    //
    //
    //     if (files.Count == 0)
    //     {
    //         logger.LogInformation("No alpha-migration required");
    //         return;
    //     }
    //
    //     var migratedUsers = await context.TgmlPlayers.ToDictionaryAsync(x => x.PlayerId);
    //     var xx = 0;
    //     var stopwatch = Stopwatch.GetTimestamp();
    //     var elapsedTimes = new List<double>();
    //     foreach (var file in files)
    //     {
    //         xx++;
    //         var content = await File.ReadAllTextAsync(file.Path);
    //
    //         using var compressionInputStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
    //         using var compressionOutputStream = new MemoryStream();
    //         await using (var compressor = new BrotliStream(compressionOutputStream, CompressionLevel.SmallestSize))
    //         {
    //             await compressionInputStream.CopyToAsync(compressor);
    //         }
    //
    //         var compressedBytes = compressionOutputStream.ToArray();
    //
    //         JsonObject? json;
    //         try
    //         {
    //             json = JsonSerializer.Deserialize<JsonObject>(content);
    //         }
    //         catch (Exception)
    //         {
    //             logger.LogCritical("Could not deserialize file {File}", file.Path);
    //             return;
    //         }
    //
    //         if (json is null)
    //         {
    //             logger.LogCritical("Unable to parse file json {File}", file);
    //             return;
    //         }
    //
    //         var usersArray = json["users"]?.AsArray();
    //         if (usersArray is null)
    //         {
    //             logger.LogCritical("Unable to find 'Users' in {File}", file.Path);
    //             return;
    //         }
    //
    //         var users = usersArray.Select(x => new TgmlPlayer
    //         {
    //             PlayerId = x?["id"]?.Deserialize<int>() ??
    //                        LogAndThrow<int>("Unable to find property [id] int 'User' at file {File}", file.Path),
    //             CurrentUsername = x?["username"]?.Deserialize<string>() ??
    //                               LogAndThrow<string>("Unable to find property [username] int 'User' at file {File}",
    //                                   file.Path)
    //         }).ToList();
    //
    //         for (var i = 0; i < users.Count; i++)
    //         {
    //             var user = users[i];
    //             if (migratedUsers.TryGetValue(user.PlayerId, out var player))
    //             {
    //                 player.CurrentUsername = user.CurrentUsername;
    //                 users[i] = player;
    //             }
    //             else
    //             {
    //                 context.TgmlPlayers.Add(user);
    //                 migratedUsers.Add(user.PlayerId, user);
    //             }
    //         }
    //
    //         var match = new TgmlMatch
    //         {
    //             MatchId = file.MatchId,
    //             Name = json["match"]?["name"].Deserialize<string>() ??
    //                    LogAndThrow<string>("Match name for file {File} does not exist", file.Path),
    //             StartTime = (json["match"]?["start_time"].Deserialize<DateTime>() ??
    //                          LogAndThrow<DateTime>("Unable to parse start_time",
    //                              file.Path)).ToUniversalTime(),
    //             CompressedJson = compressedBytes
    //         };
    //
    //         context.Add(match);
    //
    //         match.Players = users;
    //
    //         if (xx % 1000 == 0)
    //         {
    //             await context.SaveChangesAsync();
    //             var players = context.ChangeTracker.Entries<TgmlPlayer>().ToList();
    //             context.ChangeTracker.Clear();
    //             foreach (var player in players)
    //             {
    //                 context.Entry(player.Entity).State = EntityState.Unchanged;
    //             }
    //
    //             var elapsed = Stopwatch.GetElapsedTime(stopwatch).TotalSeconds;
    //             elapsedTimes.Add(elapsed);
    //             var elapsedAverage = elapsedTimes.Average();
    //             logger.LogInformation("Saved {Index} / {MaxIndex} in {Elapsed:F2}s. Expected to take: {Expected}h",
    //                 xx,
    //                 files.Count,
    //                 elapsed, TimeSpan.FromSeconds((files.Count - xx) / 1000.0 * elapsedAverage).ToString(@"h\:mm"));
    //             stopwatch = Stopwatch.GetTimestamp();
    //         }
    //     }
    //
    //     await context.SaveChangesAsync();
    // }
    //
    // public async Task MigratePagesToSqlite(string pageLocation, PageCachingContext pageContext)
    // {
    //     var migrated = (await pageContext.HistoryMatches.Select(x => x.Id).ToListAsync()).ToHashSet();
    //     var files = Directory.GetFiles(pageLocation)
    //         .Where(x => !string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(x)))
    //         .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
    //         .ToList();
    //
    //     if (files.Count == 0)
    //     {
    //         return;
    //     }
    //
    //     var xx = 0;
    //
    //     await using var connection = pageContext.Database.GetDbConnection();
    //     await connection.OpenAsync();
    //     var transaction = await connection.BeginTransactionAsync();
    //     var stopwatch = Stopwatch.GetTimestamp();
    //     List<double> elapsedTimes = [];
    //     foreach (var file in files)
    //     {
    //         xx++;
    //         await using var stream = File.OpenRead(file);
    //         var matches = JsonSerializer.Deserialize<JsonObject>(stream)?["matches"]?.Deserialize<List<HistoryMatch>>();
    //         if (matches is null)
    //         {
    //             throw new Exception($"Oops {file} is borked");
    //         }
    //
    //         foreach (var match in matches)
    //         {
    //             match.StartTime = match.StartTime.ToUniversalTime();
    //             match.EndTime = match.EndTime?.ToUniversalTime();
    //         }
    //
    //         // matches = matches.Where(x => !migrated.Contains(x.Id)).ToList();
    //
    //         try
    //         {
    //             await connection.ExecuteAsync(
    //                 "INSERT INTO HistoryMatches(id, startTime, endTime, name) VALUES (@Id, @StartTime, @EndTime, @Name) ON CONFLICT(id) DO UPDATE SET name=excluded.name, endTime=excluded.endTime;",
    //                 matches);
    //         }
    //         catch (Exception)
    //         {
    //             await transaction.RollbackAsync();
    //             throw;
    //         }
    //
    //         if (xx % 100 == 0)
    //         {
    //             await transaction.CommitAsync();
    //             transaction = await connection.BeginTransactionAsync();
    //
    //             var elapsed = Stopwatch.GetElapsedTime(stopwatch).TotalSeconds;
    //             elapsedTimes.Add(elapsed);
    //             logger.LogInformation("Saved {Index} / {MaxIndex} in {Elapsed:F2}s. Expected to take: {Expected}h",
    //                 xx,
    //                 files.Count,
    //                 elapsed,
    //                 TimeSpan.FromSeconds((files.Count - xx) / 100.0 * elapsedTimes.Average()).ToString(@"h\:mm:ss"));
    //             stopwatch = Stopwatch.GetTimestamp();
    //         }
    //     }
    //
    //     await transaction.CommitAsync();
    // }
    //
    //
    // public async Task SyncFromTheStartOf2024(TheGreatArchiver theGreatArchiver, PageCachingContext cachingContext)
    // {
    //     const int firstMatch = 111999118;
    //
    //     var fetched = (await context.Matches.Where(x => x.MatchId > firstMatch).Select(x => x.MatchId).ToListAsync())
    //         .ToHashSet();
    //
    //     var unfetched = (await cachingContext.HistoryMatches.Where(x => x.Id > firstMatch)
    //             .Select(x => x.Id)
    //             .ToListAsync())
    //         .Where(x => !fetched.Contains(x))
    //         .ToList();
    //
    //     var connection = context.Database.GetDbConnection();
    //     await connection.OpenAsync();
    //
    //     int xx = 0;
    //     foreach (var id in unfetched)
    //     {
    //         xx++;
    //
    //         logger.LogInformation("Processing {Current} / {Amount}. {Time:N2} left ", xx, unfetched.Count,
    //             TimeSpan.FromMinutes((unfetched.Count - xx) / 60.0).ToString("g"));
    //         JsonObject? result;
    //         try
    //         {
    //             result = await theGreatArchiver.GetMatchRoot(id);
    //         }
    //         catch (HttpRequestException exception)
    //         {
    //             if (exception.StatusCode != HttpStatusCode.NotFound) throw;
    //
    //             logger.LogWarning("Not found {MatchId}", id);
    //             continue;
    //         }
    //
    //         if (result is null)
    //         {
    //             logger.LogCritical("Unable to fetch {Id} root", id);
    //             return;
    //         }
    //
    //         while (theGreatArchiver.HasMatchNodeBefore(result))
    //         {
    //             result = await theGreatArchiver.GetMatchBefore(id, result);
    //         }
    //
    //
    //         await using var transaction = await connection.BeginTransactionAsync();
    //         var match = await GetMatch(result.ToString(), id);
    //
    //         try
    //         {
    //             await connection.ExecuteAsync(
    //                 "INSERT INTO match (match_id, name, start_time, compressed_json) VALUES (@MatchId, @Name, @StartTime, @CompressedJson)",
    //                 match);
    //
    //             await connection.ExecuteAsync(
    //                 "INSERT INTO player (player_id, current_username) VALUES (@PlayerId, @CurrentUsername) ON CONFLICT(player_id) DO UPDATE SET current_username = excluded.current_username",
    //                 match.Players);
    //
    //             await connection.ExecuteAsync(
    //                 "INSERT INTO match_player(matches_match_id, players_player_id) VALUES (@match_id, @player_id)",
    //                 match.Players.Select(x => new { match_id = match.MatchId, player_id = x.PlayerId }));
    //         }
    //         catch (Exception)
    //         {
    //             await transaction.RollbackAsync();
    //             throw;
    //         }
    //
    //         await transaction.CommitAsync();
    //     }
    // }
    //
    // public async Task MigrateEndTime()
    // {
    //     var matches = await context.TgmlMatches
    //         .Where(x => x.EndTime == null)
    //         .OrderBy(x => x.MatchId)
    //         .ToListAsync();
    //
    //     var count = matches.Count;
    //     var xx = 0;
    //     foreach (var match in matches)
    //     {
    //         ++xx;
    //         var decompressed = JsonSerializer.Deserialize<JsonObject>(await Decompress(match.CompressedJson));
    //         var endTime = decompressed!["match"]!["end_time"].Deserialize<DateTime?>()?.ToUniversalTime();
    //         match.EndTime = endTime;
    //
    //         logger.LogInformation("Processed: {Amount} / {Total}", xx, count);
    //     }
    //
    //     await context.SaveChangesAsync();
    // }
    //
    // private async Task<byte[]> Decompress(byte[] compressed)
    // {
    //     using var input = new MemoryStream(compressed);
    //     using var output = new MemoryStream();
    //     await using (var brotli = new BrotliStream(input, CompressionMode.Decompress))
    //     {
    //         await brotli.CopyToAsync(output);
    //     }
    //
    //     return output.ToArray();
    // }
}