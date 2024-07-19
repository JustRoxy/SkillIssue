using Microsoft.Extensions.Logging;
using SkillIssue.Common;
using SkillIssue.Domain;
using SkillIssue.Repository;
using SkillIssue.ThirdParty.Osu;
using SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts.Match;
using SkillIssue.ThirdParty.OsuCalculator;

namespace SkillIssue.Application.Services.MatchData;

public class BeatmapMatchDataExtractor(
    IBeatmapRepository beatmapRepository,
    IOsuClientFactory osuClientFactory,
    IDifficultyCalculator difficultyCalculator,
    ILogger<BeatmapMatchDataExtractor> logger)
    : IMatchDataExtractor
{
    private readonly IOsuClient _osuClient = osuClientFactory.CreateClient(OsuClientType.Types.BNO_CLIENT);

    public async Task ExtractData(IEnumerable<MatchFrame> frames, CancellationToken cancellationToken)
    {
        try
        {
            List<Beatmap> beatmaps = [];
            List<(int beatmapId, int matchId)> matchBeatmaps = [];
            foreach (var frame in frames)
            {
                var frameBeatmaps = ExtractBeatmapsFromFrame(frame);
                matchBeatmaps.AddRange(frameBeatmaps.Select(beatmap => (beatmap.BeatmapId, frame.MatchInfo.MatchId)));
                beatmaps.AddRange(frameBeatmaps);
            }

            await beatmapRepository.InsertBeatmapsIfNotExistWithBulk(beatmaps.Distinct(), cancellationToken);
            await beatmapRepository.JournalizeMatchBeatmaps(matchBeatmaps, cancellationToken);
        }
        catch (Exception exception)
        {
            throw new Exception("Failed to process beatmap frame chunk", exception);
        }
    }

    public async Task UpdateData(int matchId, CancellationToken cancellationToken)
    {
        try
        {
            var beatmapsToUpdate = await FindBeatmapsToUpdate(matchId, cancellationToken);
            foreach (var beatmapId in beatmapsToUpdate.WithProgressLogging(logger, nameof(beatmapsToUpdate)))
            {
                await UpdateBeatmap(beatmapId, cancellationToken);
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to update match beatmap data. matchId: {matchId}", e);
        }
    }

    private Task<IEnumerable<int>> FindBeatmapsToUpdate(int matchId, CancellationToken cancellationToken)
    {
        return beatmapRepository.GetMatchBeatmapsWithStatus(matchId, Beatmap.BeatmapStatus.NeedsUpdate,
            cancellationToken);
    }

    private async Task UpdateBeatmap(int beatmapId, CancellationToken cancellationToken)
    {
        try
        {
            var beatmapContent = await GetBeatmapFromClient(beatmapId, cancellationToken);
            var beatmap = CreateBeatmapWithStatusDependingOnTheContent(beatmapId, beatmapContent);
            beatmap = await CalculateBeatmap(beatmap, cancellationToken);
            beatmap = await CompressBeatmapContent(beatmap, cancellationToken);
            await beatmapRepository.UpdateBeatmap(beatmap, cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to update beatmap. beatmapId: {beatmapId}", e);
        }
    }

    private async Task<Beatmap> CompressBeatmapContent(Beatmap beatmap, CancellationToken cancellationToken)
    {
        if (beatmap.Content is null) return beatmap;

        var compressionLevel = beatmap.Content.SuitableBrotliCompressionLevel();
        beatmap.Content = await beatmap.Content.BrotliCompress(compressionLevel, cancellationToken);

        return beatmap;
    }

    private async Task<byte[]?> GetBeatmapFromClient(int beatmapId, CancellationToken cancellationToken)
    {
        try
        {
            return await _osuClient.GetBeatmapContent(beatmapId, cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to get beatmap content from osuClient. beatmapId: {beatmapId}", e);
        }
    }

    private Beatmap CreateBeatmapWithStatusDependingOnTheContent(int beatmapId, byte[]? content)
    {
        var beatmap = new Beatmap()
        {
            BeatmapId = beatmapId,
            Content = content,
            LastUpdate = DateTimeOffset.Now
        };

        if (content is null || content.Length == 0)
            beatmap.Status = Beatmap.BeatmapStatus.NotFound;

        return beatmap;
    }

    private async Task<Beatmap> CalculateBeatmap(Beatmap beatmap, CancellationToken cancellationToken)
    {
        if (beatmap.Content is null || beatmap.Status == Beatmap.BeatmapStatus.NotFound)
        {
            logger.LogWarning("No content found for {BeatmapId}, skipping calculation", beatmap.BeatmapId);
            return beatmap;
        }

        try
        {
            var difficulties =
                difficultyCalculator.CalculateBeatmapDifficulty(beatmap.BeatmapId, beatmap.Content, cancellationToken);

            await beatmapRepository.InsertDifficultiesWithBulk(difficulties, cancellationToken);
            beatmap.Status = Beatmap.BeatmapStatus.UpdatedSuccessfully;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to calculate beatmap. beatmapId: {BeatmapId}", beatmap.BeatmapId);
            beatmap.Status = Beatmap.BeatmapStatus.FailedToCalculateDifficulty;
        }

        return beatmap;
    }

    /// <summary>
    ///     Beatmaps have no external data to merge.
    /// </summary>
    public Task MergeData(int matchId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private List<Beatmap> ExtractBeatmapsFromFrame(MatchFrame frame)
    {
        try
        {
            var beatmapIds = CollectBeatmapIdsFromFrame(frame);
            return beatmapIds.Select(beatmapId => new Beatmap()
            {
                BeatmapId = beatmapId,
                Content = null,
                LastUpdate = DateTimeOffset.Now,
                Status = Beatmap.BeatmapStatus.NeedsUpdate
            }).ToList();
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to extract beatmap data from frame {frame.MatchInfo.MatchId}", e);
        }
    }

    private HashSet<int> CollectBeatmapIdsFromFrame(MatchFrame matchFrame)
    {
        return matchFrame.Events.Where(ev => ev.Game?.Beatmap is not null)
            .Select(ev => ev.Game!.Beatmap.Id)
            .ToHashSet();
    }
}