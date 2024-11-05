using ScottPlot;
using SkillIssue.Math;

namespace SkillIssue.Sandbox.Scenarios;

public class S1_CalculateDrawProbability
{
    const double drawMargin = 0.03;
    const double significantWinMargin = 0.5;

    private class CompletionInfo
    {
        public int BeatmapId { get; set; }
        public double TotalScore { get; set; }
        public double Completion { get; set; }
    }

    public static void Main()
    {
        var path = "/home/deityoxa/skillissue_public_score.csv";
        var content = File.ReadAllLines(path)
            .Select(x =>
            {
                var fields = x.Split(",");

                return new CompletionInfo
                {
                    BeatmapId = int.Parse(fields[1]),
                    TotalScore = int.Parse(fields[2]),
                    Completion = 0d
                };
            })
            .Where(x => x.TotalScore is > 1000 and < 1_400_000)
            .ToList();

        CalculateCompletion(content);
        SaveCompletionPlot(content.Select(x => x.Completion));
        SaveScorev2ScalingPlot();

        for (int i = 0; i < 1_000_000; i += 1_000)
        {
            Console.WriteLine($"{i:N0}: {Completion.CalculateScoreV2Completion(i):N6}");
        }

        var total = 0;
        var draws = 0;
        var wins = 0;
        var significantWins = 0;

        foreach (var group in content.GroupBy(x => x.BeatmapId))
        {
            var scores = group
                .Select(x => x.Completion)
                .OrderBy(_ => Random.Shared.Next())
                .ToList();

            for (var i = 0; i < scores.Count - 1; i++)
            {
                total++;
                var current = scores[i];
                var next = scores[i + 1];

                var margin = double.Max(next, current) - double.Min(next, current);

                if (margin <= drawMargin) draws++;
                else if (next / current >= 2) significantWins++;
                else wins++;
            }
        }

        Console.WriteLine($"Total: {total}");
        Console.WriteLine($"SignificantWins: {significantWins}");
        Console.WriteLine($"Wins: {wins}");
        Console.WriteLine($"Draws: {draws}");

        Console.WriteLine($"SignificantWinsP: {significantWins / (double)total:P2}");
        Console.WriteLine($"WinsP: {wins / (double)total:P2}");
        Console.WriteLine($"DrawsP: {draws / (double)total:P2}");
    }

    private static void SaveScorev2ScalingPlot()
    {
        var plot = new Plot();
        var function = plot.Add.Function(Completion.CalculateScoreV2Completion);
        function.LegendText = "ScoreV2 Scaling";
        plot.Axes.SetLimitsX(0, 1_000_000);
        plot.Axes.SetLimitsY(0, 1);
        plot.SavePng("S1_ScoreV2.png", 1920, 1080);
    }

    private static void SaveCompletionPlot(IEnumerable<double> completions)
    {
        var totalCount = (double)completions.Count();
        var plot = new Plot();
        var histogram = completions.GroupBy(x => double.Round(x, 2))
            .ToDictionary(x => x.Key, x => x.Count())
            .Select(x => new Coordinates(x.Key, x.Value / totalCount))
            .ToList();

        var draw = plot.Add.HorizontalSpan(1 - drawMargin, 1);
        var fcPercentage = 1 - histogram.Where(x => x.X < 1 - drawMargin).Sum(x => x.Y);
        draw.LegendText = $"P(draw) = {fcPercentage:P2}";

        var win = plot.Add.HorizontalSpan(1 - significantWinMargin, 1);
        var winPercentage = 1 - histogram.Where(x => x.X < 1 - significantWinMargin).Sum(x => x.Y);
        win.LegendText = $"P(win) = {winPercentage:P2} ({1 - significantWinMargin})";

        plot.Axes.SetLimitsX(0, 1);
        plot.Add.ScatterLine(histogram.OrderBy(x => x.X).ToArray());

        plot.SavePng("S1_Completions.png", 1920, 1080);
    }

    private static void CalculateCompletion(List<CompletionInfo> infos)
    {
        foreach (var info in infos)
        {
            info.Completion = Completion.CalculateScoreV2Completion(info.TotalScore);
        }
    }
}