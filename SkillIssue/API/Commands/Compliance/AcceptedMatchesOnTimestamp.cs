using MediatR;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.API.Commands.Compliance;

public class EnumerateMatchesOnTimestampRequest : IRequest<EnumerateMatchesOnTimestampResponse>
{
    public enum AcceptanceStatus
    {
        Accepted,
        Rejected
    }

    public required DateTime Timestamp { get; init; }
    public required AcceptanceStatus Status { get; set; }
}

public class EnumerateMatchesOnTimestampResponse
{
    public class AcceptedMatch
    {
        public required int MatchId { get; set; }
        public required string Name { get; set; }
        public required DateTime EndTime { get; set; }
        public required string? Reason { get; set; }
    }

    public required IAsyncEnumerable<AcceptedMatch> AcceptedMatchesStream { get; init; }
}

public class EnumerateMatchesOnTimestampHandler : IRequestHandler<EnumerateMatchesOnTimestampRequest, EnumerateMatchesOnTimestampResponse>
{
    private readonly DatabaseContext _context;
    public EnumerateMatchesOnTimestampHandler(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<EnumerateMatchesOnTimestampResponse> Handle(EnumerateMatchesOnTimestampRequest request, CancellationToken cancellationToken)
    {
        var requestTimestamp = DateTime.SpecifyKind(request.Timestamp.Date, DateTimeKind.Utc);
        var lastLobbyAtTheDate = await _context.TgmlMatches.Where(x => x.EndTime != null && x.EndTime.Value.Date <= requestTimestamp)
            .Select(x => x.MatchId)
            .OrderByDescending(x => x)
            .FirstAsync(cancellationToken: cancellationToken);

        return new EnumerateMatchesOnTimestampResponse
        {
            AcceptedMatchesStream = GenerateResponses(lastLobbyAtTheDate, request.Status)
        };
    }

    private IAsyncEnumerable<EnumerateMatchesOnTimestampResponse.AcceptedMatch> GenerateResponses(int lastMatch, EnumerateMatchesOnTimestampRequest.AcceptanceStatus requestStatus)
    {
        return _context.TgmlMatches
            // Filter by last lobby at requested timestamp
            .Where(x => x.MatchId <= lastMatch)
            .Where(x => x.EndTime != null)
            .OrderBy(x => x.MatchId)
            .Join(_context.CalculationErrors, x => x.MatchId, x => x.MatchId, (match, error) => new
            {
                match,
                error
            })
            .IfTransforming(requestStatus == EnumerateMatchesOnTimestampRequest.AcceptanceStatus.Accepted, q =>
            {
                // If accepted drop calculation log
                return q
                    .Where(z => (z.error.Flags & CalculationErrorFlag.MatchIsSkipped) == 0)
                    .Select(x => new EnumerateMatchesOnTimestampResponse.AcceptedMatch
                    {
                        MatchId = x.match.MatchId,
                        Name = x.match.Name,
                        EndTime = x.match.EndTime!.Value,
                        Reason = null
                    });
            }, q =>
            {
                // If rejected append calculation log
                return q
                    .Where(z => (z.error.Flags & CalculationErrorFlag.MatchIsSkipped) != 0)
                    .Select(x => new EnumerateMatchesOnTimestampResponse.AcceptedMatch
                    {
                        MatchId = x.match.MatchId,
                        Name = x.match.Name,
                        EndTime = x.match.EndTime!.Value,
                        Reason = x.error.CalculationErrorLog
                    });
            })
            .ToAsyncEnumerable();
    }
}