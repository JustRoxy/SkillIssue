using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenSkill;
using OpenSkill.Models;
using OpenSkill.Types;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Osu;
using SkillIssue.Database;
using SkillIssue.Domain.PPC.Entities;
using SkillIssue.Domain.Services;
using SkillIssue.Domain.TGML.Entities;
using SkillIssue.Domain.Unfair;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using Unfair.Strategies.Beatmaps;
using Unfair.Strategies.Freemods;
using Unfair.Strategies.Modification;
using Unfair.Strategies.Ratings;
using Rating = SkillIssue.Domain.Unfair.Entities.Rating;

namespace Unfair;

public class CalculationResult
{
    public TournamentMatch? Match { get; set; }
    public List<Rating>? Ratings { get; set; }
    public List<RatingHistory>? RatingHistories { get; set; }
    public List<PlayerHistory>? PlayerHistories { get; set; }

    public CalculationError CalculationError { get; } = new();

    public void AddError(CalculationErrorFlag flag, string message)
    {
        CalculationError.AddError(flag, message);
    }
}

public class UnfairContext(
    DatabaseContext context,
    ILogger<UnfairContext> logger,
    IPerformancePointsCalculator performancePointsCalculator,
    IBannedTournament bannedTournamentAcronym)
{
    private static readonly OsuRuleset OsuRuleset = new();

    private readonly OpenSkill.OpenSkill _openSkill = new(new Options
    {
        Gamma = x => 1d / x.K,
        Model = new BradleyTerryFull()
    });

    private List<int> Hosts(JsonArray events)
    {
        var currentHostId = 0;
        List<int> hosts = [];

        foreach (var @event in events)
        {
            var detailType = @event!["detail"]?["type"]?.Deserialize<string>();

            switch (detailType)
            {
                //On blatant !mp clearhost clear the host
                case "host-changed":
                    currentHostId = @event["user_id"].Deserialize<int>();
                    break;

                //When host leaves clear the host
                case "player-left":
                {
                    var playerLeftId = @event["detail"]!["user_id"]?.Deserialize<int?>();

                    if (playerLeftId == currentHostId) currentHostId = 0;

                    break;
                }
            }


            if (@event["game"]?["scores"] is null) continue;

            hosts.Add(currentHostId);
        }

        return hosts.Distinct().ToList();
    }

    private CalculationResult GetCalculationResult(TgmlMatch match, JsonObject rawMatch)
    {
        var calculationResult = new CalculationResult
        {
            CalculationError =
            {
                MatchId = match.MatchId
            }
        };
        var tournamentMatchInfo = TournamentMatch.GetTournamentMatchInfoByName(match.Name);

        if (tournamentMatchInfo is null)
        {
            logger.LogInformation("Skipping {MatchName}", match.Name);
            calculationResult.AddError(CalculationErrorFlag.NameRegexFailed, match.Name);
            return calculationResult;
        }

        calculationResult.Match = new TournamentMatch
        {
            MatchId = match.MatchId,
            Name = match.Name,
            StartTime = match.StartTime,
            EndTime = match.EndTime!.Value,
            Acronym = tournamentMatchInfo.Value.acronym,
            RedTeam = tournamentMatchInfo.Value.redTeam,
            BlueTeam = tournamentMatchInfo.Value.blueTeam
        };

        var tournamentMatchType = TournamentMatch.GetTournamentMatchType(calculationResult.Match);

        if (tournamentMatchType != TournamentMatchType.Standard)
        {
            logger.LogInformation("Detected {MatchName} as {Type}, skipping...", match.Name, tournamentMatchType);
            calculationResult.AddError(CalculationErrorFlag.NotStandardMatchType, tournamentMatchType.ToString());
            return calculationResult;
        }

        if (bannedTournamentAcronym.IsBanned(calculationResult.Match))
        {
            logger.LogInformation("Banned acronym for {MatchId} ({Name})", match.MatchId, match.Name);
            calculationResult.AddError(CalculationErrorFlag.BannedAcronym, calculationResult.Match.Acronym);
            return calculationResult;
        }


        var events = rawMatch["events"]!.AsArray();
        var hosts = Hosts(events);

        if (hosts.Count > 3)
        {
            calculationResult.AddError(CalculationErrorFlag.TooManyHosts, "Only three hosts at maximum can set a game");
            return calculationResult;
        }

        if (events.Count > 2)
            //The second event can be "host-changed" only if game is created in-game
            if (events[1]?["detail"]?["type"]?.Deserialize<string>() == "host-changed")
            {
                // bug when !mp clearhost is executed as first command
                var userId = events[1]?["detail"]?["user_id"].Deserialize<int?>();
                if (userId is null) logger.LogInformation("In-game mp make self-hosted match for {MatchId} ({Name})", match.MatchId, match.Name);
                if (userId != 0) logger.LogInformation("Self-hosted match for {MatchId} ({Name})", match.MatchId, match.Name);
                
                if (userId is not 0)
                {
                    calculationResult.AddError(CalculationErrorFlag.InGameHostedMatch, "Self-Hosted match");
                    return calculationResult;
                }
            }

        List<Score> scoreList = [];

        var currentHostId = 0;
        var warmups = 0;

        foreach (var @event in events)
        {
            var detailType = @event!["detail"]?["type"]?.Deserialize<string>();

            switch (detailType)
            {
                //On blatant !mp clearhost clear the host
                case "host-changed":
                    currentHostId = @event["user_id"].Deserialize<int>();
                    break;

                //When host leaves clear the host
                case "player-left":
                {
                    var playerLeftId = @event["detail"]!["user_id"]?.Deserialize<int?>();

                    if (playerLeftId == currentHostId) currentHostId = 0;

                    break;
                }
            }


            if (@event["game"]?["scores"] is null) continue;

            if (currentHostId != 0)
            {
                warmups++;

                switch (warmups)
                {
                    case <= 2: //Only two warmups are allowed
                        continue;
                    case 3:
                        calculationResult.AddError(CalculationErrorFlag.TooManyWarmups,
                            "Ignoring host after the second game");
                        break;
                }
            }

            //If not standard, if beatmap is not standard, or if it's not head-to-head or team-vs
            if (@event["game"]?["mode_int"]?.Deserialize<int>() != 0 ||
                @event["game"]?["beatmap"]?["mode"]?.Deserialize<string>()?.ToLower() != "osu" ||
                @event["game"]?["team_type"].Deserialize<string>()?.ToLower() is not ("head-to-head" or "team-vs"))
                continue;

            var scores = @event["game"]!["scores"]?.AsArray();
            if (scores is null) continue;

            if (scores.Count < 2)
            {
                calculationResult.AddError(CalculationErrorFlag.IncorrectAmountOfPlayers,
                    $"Skipped a game with {scores.Count} player(s)");
                continue;
            }

            var scoringType = @event["game"]!["scoring_type"]!.Deserialize<string>()!.ToLower() switch
            {
                "accuracy" => ScoringType.Accuracy,
                "combo" => ScoringType.Combo,
                "scorev2" => ScoringType.ScoreV2,
                "score" => ScoringType.Score,
                _ => ScoringType.Score
            };

            var gameId = @event["game"]!["id"].Deserialize<long>();

            foreach (var score in scores)
            {
                var playerId = score["user_id"].Deserialize<int>();
                var statistics = score["statistics"];
                var scoreMods = OsuRuleset.ConvertToLegacyMods(score["mods"].Deserialize<List<string>>()!
                    .Select(z => OsuRuleset.CreateModFromAcronym(z)).ToArray()!);
                var gameMods = OsuRuleset.ConvertToLegacyMods(
                    score.Parent!.Parent!["mods"].Deserialize<List<string>>()!
                        .Select(z => OsuRuleset.CreateModFromAcronym(z)).ToArray()!);
                var scoreObject = new Score
                {
                    MatchId = match.MatchId,
                    GameId = gameId,
                    PlayerId = playerId,
                    BeatmapId = score.Parent.Parent["beatmap"]?["id"]?.Deserialize<int>(),
                    TeamSide = score["match"]!["team"].Deserialize<string>()!.ToLower() switch
                    {
                        "red" => 1,
                        "blue" => 2,
                        _ => 0
                    },
                    ScoringType = scoringType,
                    TotalScore = score["score"].Deserialize<int>(),
                    Accuracy = score["accuracy"].Deserialize<double>(),
                    MaxCombo = score["max_combo"].Deserialize<int>(),
                    Count300 = statistics!["count_300"].Deserialize<int>(),
                    Count100 = statistics["count_100"].Deserialize<int>(),
                    Count50 = statistics["count_50"].Deserialize<int>(),
                    CountMiss = statistics["count_miss"].Deserialize<int>(),
                    LegacyMods = scoreMods | gameMods
                };

                scoreList.Add(scoreObject);
            }
        }

        if (scoreList.Count == 0)
        {
            calculationResult.AddError(CalculationErrorFlag.NoStandardScores,
                "Non-standard gamemodes are not supported");
            return calculationResult;
        }

        calculationResult.Match.Scores = scoreList;

        calculationResult.Ratings = [];
        calculationResult.PlayerHistories = [];
        calculationResult.RatingHistories = [];
        return calculationResult;
    }

    private static bool IsModForbidden(LegacyMods mod)
    {
        return mod.HasFlag(LegacyMods.Relax) ||
               mod.HasFlag(LegacyMods.Autopilot) ||
               mod.HasFlag(LegacyMods.Autoplay) ||
               mod.HasFlag(LegacyMods.SpunOut);
    }

    private static List<IGrouping<long, Score>> GetGameGroups(CalculationResult calculationResult)
    {
        ArgumentNullException.ThrowIfNull(calculationResult.Match?.Scores);
        var match = calculationResult.Match;
        List<Score> scores = [];

        foreach (var game in match!.Scores
                     .Where(x => x.Accuracy > 0.4d)
                     .Where(x => !IsModForbidden(x.LegacyMods))
                     .GroupBy(x => x.GameId))
        {
            if (game.Any(z => z.TeamSide == 0))
                //Exclude huge head on head games
                if (game.Count() > 2)
                {
                    calculationResult.AddError(CalculationErrorFlag.BigHeadOnHeadGame, $"Ignoring {game.Key}");
                    continue;
                }

            //Exclude non-symmetrical teams
            if (game.Count(x => x.TeamSide == 1) != game.Count(x => x.TeamSide == 2))
            {
                calculationResult.AddError(CalculationErrorFlag.NonSymmetricalTeams, $"Ignoring {game.Key}");
                continue;
            }

            if (game.Count() > 8)
            {
                calculationResult.AddError(CalculationErrorFlag.TooManyPlayers,
                    $"Ignoring {game.Key}, expected less than 8, got {game.Count()}");
                continue;
            }

            scores.AddRange(game);
        }

        return scores.GroupBy(x => x.GameId).ToList();
    }

    private static void CheckDoubleTimeBug(IEnumerable<Score> scores)
    {
        var games = scores.GroupBy(x => x.GameId).ToList();

        foreach (var game in games)
        {
            //On double time conflict
            if (game.All(x => !x.LegacyMods.HasFlag(LegacyMods.DoubleTime)) ||
                game.All(x => x.LegacyMods.HasFlag(LegacyMods.DoubleTime))) continue;

            foreach (var score in game)
                //get sad if it was really played with double time :(((
                score.LegacyMods &= ~(LegacyMods.DoubleTime | LegacyMods.Nightcore);
        }
    }

    private async Task<CalculationResult> CalculateMatch(CalculationResult previousCalculation,
        IBeatmapLookup? beatmapLookup = null,
        IRatingRepository? ratingRepository = null)
    {
        var match = previousCalculation.Match;
        if (match is null) throw new ArgumentNullException(nameof(match));

        var games = GetGameGroups(previousCalculation);

        switch (games.Count)
        {
            case < 3:
                previousCalculation.AddError(CalculationErrorFlag.InsufficientAmountOfGames,
                    $"Expected more than 3 games, got {games.Count}");
                return previousCalculation;
            case >= 22:
                previousCalculation.AddError(CalculationErrorFlag.TooManyGames,
                    $"Expected less than 22 games, got {games.Count}");
                return previousCalculation;
        }

        ratingRepository ??=
            new CachedRatingRepository(await GetRatings(match), onCreation: r => context.Ratings.Add(r));
        beatmapLookup ??= new PrecachedBeatmapLookup(await GetBeatmapAttributes(match));

        CheckDoubleTimeBug(match.Scores!);

        var scores = games.SelectMany(x => x).ToList();
        await CalculatePerformancePoints(beatmapLookup, scores, CancellationToken.None);


        HashSet<Rating> localRatings = [];
        previousCalculation.RatingHistories = [];
        previousCalculation.PlayerHistories = [];

        foreach (var game in games)
        {
            var buckets =
                new GroupingFreemodStrategy(game.ToList(),
                    new TrimmingModificationStrategy(),
                    beatmapLookup);

            foreach (var bucket in buckets.Groups)
            {
                var attributeId = RatingAttribute.GetAttributeId(bucket.ModificationAttribute,
                    bucket.Skillset.Attribute, bucket.ScoringAttribute);

                if (bucket.ScoringAttribute == ScoringRatingAttribute.PP)
                {
                    for (var i = 0; i < bucket.Scores.Count; i++)
                    {
                        var score = bucket.Scores[i];
                        ratingRepository.GetRating(score.PlayerId, attributeId, out var rating);
                        var oldOrdinal = rating.OrdinalShort;
                        var oldStarRating = rating.StarRating;
                        var oldMu = rating.Mu;
                        var oldSigma = rating.Sigma;

                        localRatings.Add(rating);
                        rating.GamesPlayed++;

                        var newStarRating = oldStarRating;

                        if (bucket.Skillset.BeatmapPerformance is not null &&
                            bucket.Skillset.BeatmapPerformance.StarRating < 10)
                        {
                            rating.AddStarRating(bucket.Skillset.BeatmapPerformance.StarRating);
                            newStarRating = rating.StarRating;
                        }

                        var pps = score.Pp ?? 0;
                        var ppRank = bucket.Scores.Count(z => (z.Pp ?? 0) >= pps);

                        if (score.Pp is not null)
                        {
                            rating.PerformancePoints.Add(pps);
                            rating.Ordinal =
                                (short)Math.Round(CalculateTotalPps(rating), MidpointRounding.AwayFromZero);
                            rating.WinAmount += bucket.Scores.Count - ppRank;
                            rating.TotalOpponentsAmount += bucket.Scores.Count - 1;
                        }

                        previousCalculation.RatingHistories.Add(new RatingHistory
                        {
                            GameId = score.GameId,
                            PlayerId = score.PlayerId,
                            MatchId = match.MatchId,
                            RatingAttributeId = attributeId,
                            OldStarRating = (float)oldStarRating,
                            NewStarRating = (float)newStarRating,
                            OldOrdinal = oldOrdinal,
                            NewOrdinal = rating.OrdinalShort,
                            OldMu = (float)oldMu,
                            NewMu = (float)rating.Mu,
                            OldSigma = (float)oldSigma,
                            NewSigma = (float)rating.Sigma,
                            PredictedRank = 0,
                            Rank = (byte)ppRank
                        });
                    }

                    continue;
                }

                List<Team> teams = [];

                foreach (var score in bucket.Scores)
                {
                    ratingRepository.GetRating(score.PlayerId, attributeId, out var rating);
                    localRatings.Add(rating);
                    teams.Add(Team.With(new OpenSkill.Types.Rating(rating.Mu, rating.Sigma, score)));
                }

                var predictedRank = _openSkill
                    .PredictRank(teams)
                    .Zip(teams)
                    .OrderByDescending(x => x.First.probability)
                    .Zip(Enumerable.Range(1, teams.Count))
                    .ToDictionary(x => (Score)x.First.Second.Ratings[0].Reference!, x => (byte)x.Second);

                var rank = 1;

                foreach (var newRating in _openSkill.Rate(teams.ToList()).SelectMany(x => x.Ratings))
                {
                    var score = (Score)newRating.Reference!;
                    ratingRepository.GetRating(score.PlayerId, attributeId, out var rating);

                    var oldOrdinal = rating.OrdinalShort;
                    var oldStarRating = rating.StarRating;
                    var oldMu = rating.Mu;
                    var oldSigma = rating.Sigma;

                    rating.Mu = newRating.Mu;
                    rating.Sigma = newRating.Sigma;

                    rating.GamesPlayed++;
                    rating.WinAmount += bucket.Scores.Count - rank;
                    rating.TotalOpponentsAmount += bucket.Scores.Count - 1;

                    var newStarRating = oldStarRating;

                    if (bucket.Skillset.BeatmapPerformance is not null &&
                        bucket.Skillset.BeatmapPerformance.StarRating < 10)
                    {
                        rating.AddStarRating(bucket.Skillset.BeatmapPerformance.StarRating);
                        newStarRating = rating.StarRating;
                    }

                    var scaledMu = rating.Mu + 25d / 12 * newStarRating;
                    var newOrdinal = (scaledMu - 3 * rating.Sigma) * 150;

                    rating.Ordinal = newOrdinal;

                    previousCalculation.RatingHistories.Add(new RatingHistory
                    {
                        GameId = score.GameId,
                        PlayerId = score.PlayerId,
                        RatingAttributeId = rating.RatingAttributeId,
                        MatchId = score.MatchId,
                        OldStarRating = (float)oldStarRating,
                        NewStarRating = (float)newStarRating,
                        OldOrdinal = oldOrdinal,
                        OldMu = (float)oldMu,
                        OldSigma = (float)oldSigma,
                        NewSigma = (float)rating.Sigma,
                        NewMu = (float)rating.Mu,
                        NewOrdinal = rating.OrdinalShort,
                        Rank = (byte)rank,
                        PredictedRank = predictedRank[score]
                    });
                    rank++;
                }
            }
        }


        previousCalculation.Ratings = localRatings.ToList();
        previousCalculation.PlayerHistories = GetPlayerHistories(match);

        return previousCalculation;
    }

    private static double CalculateTotalPps(Rating rating)
    {
        if (rating.PerformancePoints.Count == 0) return 0;

        double totalPps = 0;
        var x = 0;
        const double epsilon = 0.001d;

        foreach (var pp in rating.PerformancePoints.OrderDescending())
        {
            var increase = pp * Math.Pow(0.95, x);
            totalPps += increase;
            if (increase < epsilon) return totalPps;

            x++;
        }

        const double initialScaling = 417 - 1d / 3;
        var bonusPp = initialScaling * (1 - Math.Pow(0.995, Math.Min(1000, rating.PerformancePoints.Count)));
        return totalPps + bonusPp;
    }

    private async Task CalculatePerformancePoints(IBeatmapLookup beatmapLookup,
        IEnumerable<Score> scores,
        CancellationToken token)
    {
        await Parallel.ForEachAsync(scores, token, async (score, cancellationToken) =>
        {
            var beatmapPerformance = beatmapLookup.LookupPerformance(score.BeatmapId, score.LegacyMods);
            if (beatmapPerformance is null) return;

            //Beatmap Attribute Sanity Check
            if (beatmapPerformance.ApproachRate > 11 || beatmapPerformance.StarRating > 10) return;

            var pp = await performancePointsCalculator.CalculatePerformancePoints(beatmapPerformance, score,
                cancellationToken);
            if (pp < 1500) score.Pp = pp;
        });
    }

    private List<PlayerHistory> GetPlayerHistories(TournamentMatch match)
    {
        ArgumentNullException.ThrowIfNull(match.Scores, nameof(match.Scores));

        var maxScore = match.Scores.Max(x => x.TotalScore);
        var matchcost = Calculations.MatchCost(match.Scores.Select(x => new Calculations.PlayerGameScore
        {
            PlayerId = x.PlayerId,
            GameId = x.GameId,
            Score = x.ScoringType switch
            {
                ScoringType.ScoreV2 => x.TotalScore,
                ScoringType.Accuracy => Math.Min(0, 1 + Math.Log(x.Accuracy, 1.3)) * 1_000_000,
                ScoringType.Combo => x.MaxCombo,
                ScoringType.Score => x.TotalScore / (double)maxScore * 1_000_000,
                _ => throw new ArgumentOutOfRangeException()
            }
        }));

        return match.Scores.Select(x => x.PlayerId).Distinct().Select(x => new PlayerHistory
        {
            PlayerId = x,
            MatchId = match.MatchId,
            MatchCost = matchcost.GetValueOrDefault(x, -1)
        }).ToList();
    }

    private async Task<Dictionary<(int BeatmapId, int Mods), BeatmapPerformance>> GetBeatmapAttributes(
        TournamentMatch match)
    {
        ArgumentNullException.ThrowIfNull(match.Scores, nameof(match.Scores));

        var beatmaps = match.Scores
            .Where(x => x.BeatmapId is not null)
            .Select(x => x.BeatmapId)
            .Distinct()
            .ToList();

        return await context.BeatmapPerformances
            .AsNoTracking()
            .Where(x => beatmaps.Contains(x.BeatmapId))
            .ToDictionaryAsync(x => (x.BeatmapId, x.Mods));
    }

    private async Task<Dictionary<(int PlayerId, int RatingAttributeId), Rating>> GetRatings(TournamentMatch match)
    {
        var playerIds = match.Scores!.Select(x => x.PlayerId).Distinct().ToList();

        var playerRatings = await context.Ratings
            .Where(x => playerIds.Contains(x.PlayerId))
            .ToDictionaryAsync(x => (x.PlayerId, x.RatingAttributeId));

        return playerRatings;
    }

    public async Task<CalculationResult> CalculateMatch(TgmlMatch completedMatch, IBeatmapLookup? lookup = null,
        IRatingRepository? ratingRepository = null,
        JsonObject? rawMatch = null)
    {
        logger.LogInformation("CalculateMatch({MatchId}, {MatchName})", completedMatch.MatchId, completedMatch.Name);
        var insertCalculationResult =
            GetCalculationResult(completedMatch, (rawMatch ?? await completedMatch.Deserialize())!);
        if (insertCalculationResult.Match?.Scores is not null &&
            insertCalculationResult.Match.Scores.Count != 0)
            return await CalculateMatch(insertCalculationResult, lookup, ratingRepository);

        logger.LogInformation("InsertMatch returned null, skipping...");
        return insertCalculationResult;
    }
}