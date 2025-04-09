using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SkillIssue.Database;
using SkillIssue.Domain.Unfair.Entities;
using TheGreatSpy.Services;

namespace SkillIssue.Integrations.Spreadsheet;

public class SpreadsheetIntegration(
    IOptions<SpreadsheetIntegrationSettings> options,
    ILogger<SpreadsheetIntegration> logger,
    DatabaseContext context,
    PlayerService playerService)
{
    private static Regex? _validationRegex;

    public async Task<IResult> GetSIP(HttpRequest request, int userId, bool estimate, CancellationToken token)
    {
        if (!ValidateRequest(request)) return GenerateValidationError();

        logger.LogInformation("Serving {IntegrationName} for {PlayerId} with estimate = {Estimate}", nameof(GetSIP), userId, estimate);

        var rating = await context.Ratings
            .AsNoTracking()
            .Where(x => x.RatingAttributeId == 0 && x.PlayerId == userId)
            .Select(x => x.Ordinal)
            .FirstOrDefaultAsync(token);

        if (rating == 0 && estimate)
        {
            var player = await playerService.GetPlayerById(userId);
            if (player?.GlobalRank is null or 0) return Results.Ok(0);

            var estimatedSip = await context.Ratings
                .AsNoTracking()
                .Where(x => x.RatingAttributeId == 0)
                .OrderBy(x => Math.Abs(player.GlobalRank.Value - x.Player.GlobalRank!.Value))
                .Take(100)
                .Select(x => x.Ordinal)
                .AverageAsync(token);

            return Results.Ok(Math.Round(estimatedSip));
        }

        return Results.Ok(Math.Round(rating));
    }

    /// <summary>
    ///     It was a conscious decision to return only the rating as the result of the `GetSIP` endpoint. But now possibly can't handle the situation where status of the player is required. 
    /// </summary>
    public async Task<IResult> GetPlayerRating(HttpRequest request, int userId, bool estimate, CancellationToken token)
    {
        if (!ValidateRequest(request)) return GenerateValidationError();

        logger.LogInformation("Serving {IntegrationName} for {PlayerId} with estimate = {Estimate}", nameof(GetPlayerRating), userId, estimate);

        var rating = await context.Ratings
            .AsNoTracking()
            .Where(x => x.RatingAttributeId == 0 && x.PlayerId == userId)
            .FirstOrDefaultAsync(token);

        var ordinal = rating?.Ordinal ?? 0;

        if (ordinal == 0 && estimate)
        {
            var player = await playerService.GetPlayerById(userId);
            if (player?.GlobalRank is null or 0) return Results.Ok(0);

            var estimatedSip = await context.Ratings
                .AsNoTracking()
                .Where(x => x.RatingAttributeId == 0)
                .OrderBy(x => Math.Abs(player.GlobalRank.Value - x.Player.GlobalRank!.Value))
                .Take(100)
                .Select(x => x.Ordinal)
                .AverageAsync(token);

            ordinal = estimatedSip;
        }

        return Results.Ok(new
        {
            Status = rating?.Status ?? RatingStatus.Calibration,
            Rating = Math.Round(ordinal)
        });
    }

    private bool ValidateRequest(HttpRequest request)
    {
        _validationRegex ??= new Regex(options.Value.SpreadsheetValidationRegex,
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var match = _validationRegex.Match(request.Headers.UserAgent.ToString());
        if (!match.Success) return false;

        logger.LogInformation("Spreadsheet validation hit for {Id}", match.Groups["id"].Value);
        return true;
    }

    private IResult GenerateValidationError()
    {
        return Results.BadRequest(new
        {
            Error = "Spreadsheet integration can be used only with Google Spreadsheet's Apps Script framework"
        });
    }
}