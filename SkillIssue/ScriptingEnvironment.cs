using System.Text.Json;
using MathNet.Numerics.Statistics;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using Histogram = ScottPlot.Statistics.Histogram;

namespace SkillIssue;

public class ScriptingEnvironment(DatabaseContext context, ILogger<ScriptingEnvironment> logger)
{
    public async Task ScriptingMain()
    {
        await FetchPpDelta();
        Environment.Exit(0);
    }

    private async Task AnalyzePpDelta()
    {
        var deltas = JsonSerializer.Deserialize<List<double>>(await File.ReadAllTextAsync("pp_deltas.json"));
        AnalyzePpDelta(deltas!);
        var plot = new ScottPlot.Plot();

        var hist = Histogram.WithFixedBinSize(deltas!.Min(), deltas.Max(), 100);
        hist.AddRange(deltas);
        var sum = hist.Counts.Sum();
        plot.Add.Bars(hist.Bins, hist.Counts.Select(x => x / sum * 100));

        plot.SavePng("pp_deltas_hist.png", 3200, 2400);
    }

    private void AnalyzePpDelta(List<double> deltas)
    {
        var summary = deltas.FiveNumberSummary();
        Console.WriteLine($"Delta length: {deltas.Count}");
        Console.WriteLine(
            $"Five-point summary: [{summary[0]:F0}, {summary[1]:F0}, {summary[2]:F0}, {summary[3]:F0}, {summary[4]:F0}]");
        Console.WriteLine($"80th percentile: {deltas.Percentile(80):F0}");
    }

    private async Task FetchPpDelta()
    {
        foreach (var attribute in RatingAttribute.GetAllUsableAttributes()
                     .Where(x => x.Scoring == ScoringRatingAttribute.PP))
        {
            var data = await context.RatingHistories
                .AsNoTracking()
                .Where(x => x.RatingAttributeId == attribute.AttributeId)
                .Select(x => new
                {
                    x.MatchId,
                    x.PlayerId,
                    x.GameId,
                    x.OldOrdinal,
                    x.Rank
                })
                .ToListAsync();

            var groups = data.GroupBy(x => x.GameId).OrderBy(x => x.Key);

            Dictionary<int, int> playerHistory = [];


            var deltas = new List<double>();
            foreach (var group in groups)
            {
                var g = group.OrderBy(x => x.Rank).DistinctBy(x => x.PlayerId).ToList();
                foreach (var p in g.Select(x => x.PlayerId))
                {
                    playerHistory[p] = playerHistory.GetValueOrDefault(p, 0) + 1;
                }

                for (int i = 0; i < g.Count; i++)
                {
                    for (int j = i + 1; j < g.Count; j++)
                    {
                        var delta = g[i].OldOrdinal - g[j].OldOrdinal;
                        // if (delta < -5000)
                        // {
                        //     logger.LogInformation(
                        //         "Lost: https://osu.ppy.sh/u/{Who} to https://osu.ppy.sh/u/{Whom} in match https://osu.ppy.sh/mp/{MatchId} by {Delta}",
                        //         g[j].PlayerId, g[i].PlayerId, g[i].MatchId, delta);
                        // }

                        deltas.Add(delta);
                    }
                }
            }

            Console.WriteLine(
                $"{RatingAttribute.DescriptionFormat(attribute.Modification)} - {RatingAttribute.DescriptionFormat(attribute.Skillset)}");

            AnalyzePpDelta(deltas);

            Console.WriteLine();
        }

        // await File.WriteAllTextAsync("pp_deltas.json", JsonSerializer.Serialize(deltas));
    }
}