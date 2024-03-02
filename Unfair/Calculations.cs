namespace Unfair;

public enum TeamType
{
    Red,
    Blue
}

public static class Calculations
{
    private static List<TeamType> Winners(IEnumerable<TeamScore> teamScores)
    {
        var winners = new List<TeamType>();
        foreach (var game in teamScores.GroupBy(x => x.GameId))
        {
            var blueTeamScore = game.Where(x => x.TeamType == TeamType.Blue).Select(x => x.Score).Sum();
            var redTeamScore = game.Where(x => x.TeamType == TeamType.Red).Select(x => x.Score).Sum();

            if (Math.Abs(blueTeamScore - redTeamScore) < 0.00001d) continue;
            if (blueTeamScore > redTeamScore) winners.Add(TeamType.Blue);
            else winners.Add(TeamType.Red);
        }

        return winners;
    }

    public static (int redTeam, int blueTeam) Points(IEnumerable<TeamScore> teamScores)
    {
        var winners = Winners(teamScores);

        if (winners.Count == 0) return (0, 0);

        var blueTeamPoints = winners.Count(x => x == TeamType.Blue);
        var redTeamPoints = winners.Count(x => x == TeamType.Red);

        if (blueTeamPoints == redTeamPoints)
        {
            // logger.LogInformation("Warmup detected: {gameId}", teamScores.First().GameId);

            var lastGameWinner = winners.Last();
            if (lastGameWinner == TeamType.Blue) redTeamPoints--;
            else blueTeamPoints--;
        }

        return (redTeamPoints, blueTeamPoints);
    }

    public static Dictionary<int, TeamType> CalculateTeams(IEnumerable<IGrouping<int, TeamType>> teams)
    {
        return teams.ToDictionary(x => x.Key, x => { return (TeamType)GetMedian(x.Select(v => (int)v).Order()); });
    }

    //If it becomes a bottleneck then use https://en.wikipedia.org/wiki/Quickselect
    private static double GetMedian(IEnumerable<int> source)
    {
        var temp = source.ToArray();
        Array.Sort(temp);

        var count = temp.Length;
        if (count % 2 != 0) return temp[count / 2];

        var a = temp[count / 2 - 1];
        var b = temp[count / 2];

        return (a + b) / 2d;
    }

    private static double GetMedian(IEnumerable<double> source)
    {
        var temp = source.ToArray();
        Array.Sort(temp);

        var count = temp.Length;
        if (count % 2 != 0) return temp[count / 2];

        var a = temp[count / 2 - 1];
        var b = temp[count / 2];

        return (a + b) / 2d;
    }

    public static double PlayerMatchCost(List<PlayerGameScore> request, int playerId)
    {
        var players = request
            .GroupBy(x => x.PlayerId)
            .ToDictionary(x => x.Key, x => x.Count());

        Dictionary<int, double> matchCosts = new();
        var m = GetMedian(players.Select(x => x.Value));

        var maps = request.GroupBy(x => x.GameId).ToList();

        double MatchCostForPlayer()
        {
            var n = players[playerId];
            var root = Math.Pow(n / m, 1 / 3d);
            var sum = (from map in maps.Where(v => v.Any(x => x.PlayerId == playerId))
                    let mi = GetMedian(map.Select(v => v.Score))
                    let ni = map.First(x => x.PlayerId == playerId).Score
                    select ni / mi)
                .Sum();

            return sum / players[playerId] * root;
        }

        return MatchCostForPlayer();
    }

    public static Dictionary<int, double> MatchCost(IEnumerable<PlayerGameScore> request)
    {
        /*
         * Sum(1_score / median_score) * root^3(amount_of_maps_played_by_1 / average_amount_of_maps_played)
         */

        var players = request
            .GroupBy(x => x.PlayerId)
            .ToDictionary(x => x.Key, x => x.Count());

        Dictionary<int, double> matchCosts = new();
        var m = GetMedian(players.Select(x => x.Value));

        var maps = request.GroupBy(x => x.GameId).ToList();

        double MatchCostForPlayer(int playerId)
        {
            var n = players[playerId];
            var root = Math.Pow(n / m, 1 / 3d);
            double sum = 0;
            foreach (var map in maps.Where(x => x.Any(z => z.PlayerId == playerId)))
            {
                var mi = GetMedian(map.Select(v => v.Score)); // Median score on the map
                var ni = map.First(x => x.PlayerId == playerId).Score; //Player's score on the map
                sum += ni / mi;
            }

            return sum / n * root;
        }

        foreach (var (playerId, _) in players) matchCosts[playerId] = MatchCostForPlayer(playerId);

        return matchCosts;
    }

    public class TeamScore
    {
        public long GameId { get; set; }
        public TeamType TeamType { get; set; }
        public double Score { get; set; }
    }

    public class PlayerGameScore
    {
        public int PlayerId { get; init; }
        public long GameId { get; init; }
        public double Score { get; init; }
    }
}