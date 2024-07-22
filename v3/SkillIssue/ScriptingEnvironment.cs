using System.Text.Json;
using Dapper;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using SkillIssue.Common;
using SkillIssue.Common.Utils;
using SkillIssue.Infrastructure;
using SkillIssue.Infrastructure.Repositories.BeatmapRepository.Contracts;
using SkillIssue.Infrastructure.Repositories.MatchFrameRepository.Contracts;
using SkillIssue.Repository;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match;

namespace SkillIssue;

public class ScriptingEnvironment
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IMatchFrameRepository _matchFrameRepository;
    private readonly ILogger<ScriptingEnvironment> _logger;

    public ScriptingEnvironment(IConnectionFactory connectionFactory,
        IMatchFrameRepository matchFrameRepository,
        ILogger<ScriptingEnvironment> logger)
    {
        _connectionFactory = connectionFactory;
        _matchFrameRepository = matchFrameRepository;
        _logger = logger;
    }

    public async Task ExecuteScript()
    {
        var frames = await GetMatchesFrames();

        foreach (var frame in frames)
        {
            var dframe = await frame.Frame.BrotliDecompress(CancellationToken.None);
            var f = JsonSerializer.Deserialize<MatchFrame>(dframe);
            Console.WriteLine(f);
        }
    }

    private async Task<List<BeatmapRecord>> GetBeatmapRecords()
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();
        var records = await connection.QueryAsync<BeatmapRecord>(
            "select * from beatmap where content is not null order by beatmap_id");
        return records.ToList();
    }

    private async Task<List<MatchFrameRecord>> GetMatchesFrames()
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();
        var records = await connection.QueryAsync<MatchFrameRecord>(
            "select * from match_frame order by match_id, cursor");
        return records.ToList();
    }

    private async IAsyncEnumerable<FlatWorkingBeatmap> GetWorkingBeatmaps(List<BeatmapRecord> records)
    {
        foreach (var record in records)
        {
            var decompressedContent = await record.Content!.BrotliDecompress(CancellationToken.None);

            using var memoryStream = new MemoryStream(decompressedContent);
            using var stream = new LineBufferedReader(memoryStream);
            var decoder = Decoder.GetDecoder<Beatmap>(stream);
            var beatmap = decoder.Decode(stream);
            var workingBeatmap = new FlatWorkingBeatmap(beatmap);
            yield return workingBeatmap;
        }
    }
}