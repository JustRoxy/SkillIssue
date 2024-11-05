using ImageMagick;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using ScottPlot;
using SkillIssue.Common.Extensions;

#pragma warning disable CS7022 // The entry point of the program is global code; ignoring entry point
namespace SkillIssue.Sandbox.Scenarios;

public class S2_OsuReplayPlayground
{
    public static void Main()
    {
        var file = "Resources/zenpen.osu";

        var ruleset = new OsuRuleset().RulesetInfo;
        var beatmap = new FlatWorkingBeatmap(file).GetPlayableBeatmap(ruleset, new[]
        {
            new ModNoMod()
        });


        var hitcircles = beatmap.HitObjects.ToList();
        using var collection = new MagickImageCollection();
        Console.WriteLine("Starting generation");
        foreach (var triplets in hitcircles.OfType<OsuHitObject>().Where(x => x is not Spinner).LinearOverlapChunk(3))
        {
            var plot = new Plot();
            plot.Axes.SetLimits(0, 512, 0, 384);
            var list = triplets.ToList();
            DrawLines(plot, list);
            DrawObject(plot, list);
            var bytes = plot.GetImageBytes(512, 512);
            collection.Add(new MagickImage(bytes)
            {
                AnimationDelay = 10,
            });
        }

        Console.WriteLine("Starting optimization");
        collection.Optimize();


        Console.WriteLine("Starting saving");
        using var fileStream = File.OpenWrite("S2_Replay.gif");
        collection.Write(fileStream, MagickFormat.Gif);
    }


    private static void DrawObject(Plot plot, List<OsuHitObject> hitObjects)
    {
        for (int i = 0; i < hitObjects.Count; i++)
        {
            var hitObject = hitObjects[i];
            var paths = GetPaths(hitObject);
            if (paths.Count == 1)
            {
                var ellipse = plot.Add.Circle(hitObject.X, hitObject.Y, hitObject.Radius);
                ellipse.FillColor = new Color(128, 128, 128);
                ellipse.LineColor = new Color(0, 0, 0);
                var text = plot.Add.Text((hitObject.IndexInCurrentCombo + 1).ToString(), hitObject.X, hitObject.Y);
                text.Alignment = Alignment.MiddleCenter;
            }
            else
            {
                var head = hitObject.Position;
                var tail = hitObject.EndPosition;
                var headCircle = plot.Add.Circle(head.X, head.Y, hitObject.Radius);
                headCircle.FillColor = new Color(128, 128, 128);
                headCircle.LineColor = new Color(0, 0, 0);
                var text = plot.Add.Text((hitObject.IndexInCurrentCombo + 1).ToString(), head.X, head.Y);
                text.Alignment = Alignment.MiddleCenter;

                var tailCircle = plot.Add.Circle(tail.X, tail.Y, hitObject.Radius);
                tailCircle.FillColor = new Color(128, 128, 128);
                tailCircle.LineColor = new Color(0, 0, 0);
                DrawSliderBody(plot, paths);
            }
        }
    }

    private static void DrawLines(Plot plot, List<OsuHitObject> circles)
    {
        for (int i = 0; i < circles.Count - 1; i++)
        {
            var curr = GetTailPosition(circles[i]);
            var next = GetHeadPosition(circles[i + 1]);
            var line = plot.Add.Line(curr.x, curr.y, next.x, next.y);

            line.Color = new Color(128, 128, 128);
            line.LineWidth = 8;
        }
    }

    private static void DrawSliderBody(Plot plot, List<Vector2> paths)
    {
        for (int i = 0; i < paths.Count - 1; i++)
        {
            var curr = paths[i];
            var next = paths[i + 1];

            var line = plot.Add.Line(curr.X, curr.Y, next.X, next.Y);
            line.Color = new Color(100, 100, 100);
            line.LineWidth = 20;
        }
    }

    private static List<Vector2> GetPaths(OsuHitObject hitObject)
    {
        var list = new List<Vector2>();
        if (hitObject is HitCircle circle) list.Add(circle.Position);
        if (hitObject is Slider slider) slider.Path.GetPathToProgress(list, 0, 1);

        var result = new List<Vector2>();
        foreach (var vector2 in list)
            result.Add(hitObject.Position + vector2);

        return result;
    }

    private static (float x, float y) GetHeadPosition(HitObject hitObject)
    {
        if (hitObject is HitCircle circle) return (circle.X, circle.Y);
        if (hitObject is Slider slider) return (slider.X, slider.Y);

        if (hitObject is Spinner) return (0, 0);
        throw new Exception("Unknown type");
    }

    private static (float x, float y) GetTailPosition(HitObject hitObject)
    {
        if (hitObject is HitCircle circle) return (circle.X, circle.Y);
        if (hitObject is Slider slider) return (slider.EndPosition.X, slider.EndPosition.Y);
        if (hitObject is Spinner) return (0, 0);

        throw new Exception("Unknown type");
    }
}