using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SkillIssue.Database;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using TheGreatSpy.Services;

namespace SkillIssue.Integrations.Spreadsheet;

public class SpreadsheetIntegration(
    IOptions<SpreadsheetIntegrationSettings> options,
    ILogger<SpreadsheetIntegration> logger,
    DatabaseContext context,
    PlayerService playerService)
{
    private static Regex? _validationRegex;

    /// <summary>
    ///     It was a conscious decision to return only the rating as the result of the `GetSIP` endpoint.
    /// </summary>
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

            var estimatedSip = await GetEstimation(0, player.GlobalRank.Value, token);
            return Results.Ok(Math.Round(estimatedSip));
        }

        return Results.Ok(Math.Round(rating));
    }

    public async Task<IResult> GetPlayerRating(HttpRequest request, int userId, bool estimate, CancellationToken token)
    {
        if (!ValidateRequest(request)) return GenerateValidationError();

        logger.LogInformation("Serving {IntegrationName} for {PlayerId} with estimate = {Estimate}", nameof(GetPlayerRating), userId, estimate);

        var rating = await context.Ratings
            .AsNoTracking()
            .Where(x => x.RatingAttributeId == 0 && x.PlayerId == userId)
            .FirstOrDefaultAsync(token);

        var ordinal = rating?.Ordinal ?? 0;

        var isEstimated = false;

        if (ordinal == 0 && estimate)
        {
            var player = await playerService.GetPlayerById(userId);
            if (player?.GlobalRank is null or 0)
                return Results.Ok(new
                {
                    status = RatingStatus.Calibration,
                    rating = 0d
                });

            isEstimated = true;
            ordinal = await GetEstimation(0, player.GlobalRank.Value, token);
        }

        return Results.Ok(new
        {
            status = rating?.Status ?? RatingStatus.Calibration,
            rating = Math.Round(ordinal),
            is_estimated = isEstimated
        });
    }

    public async Task<IResult> GetPlayerRatings(HttpRequest request, string username, bool estimate, CancellationToken token)
    {
        if (!ValidateRequest(request)) return GenerateValidationError();
        var getUserIdResult = await GetUserId(username);
        if (getUserIdResult == default) return Results.NotFound();
        var (userId, player) = getUserIdResult;

        logger.LogInformation("Serving {IntegrationName} for {Username} with estimate = {Estimate}", nameof(GetPlayerRatings), username, estimate);

        var rating = await context.Ratings
            .AsNoTracking()
            .Where(x => RatingAttribute.MajorAttributes.Contains(x.RatingAttributeId))
            .Where(x => x.PlayerId == userId)
            .ToDictionaryAsync(x => x.RatingAttributeId, x => x.Ordinal, token);

        var slots = RatingAttribute.MajorAttributes.ToDictionary(RatingAttribute.GetAttribute, x => new
            {
                Ordinal = rating.GetValueOrDefault(x),
                IsEstimated = false
            }
        );

        if (estimate)
        {
            player ??= await playerService.GetPlayerById(userId);

            if (player?.GlobalRank is not null)
            {
                var globalRank = player.GlobalRank.Value;

                foreach (var (attribute, ordinal) in slots.ToList())
                {
                    if (ordinal.Ordinal != 0) continue;

                    slots[attribute] = new
                    {
                        Ordinal = await GetEstimation(attribute.AttributeId, globalRank, token),
                        IsEstimated = true
                    };
                }
            }
        }

        return Results.Ok(slots
            .OrderBy(x => x.Key.AttributeId)
            .Select(x => new
            {
                attribute_id = x.Key.AttributeId,
                modification = x.Key.Modification.ToString(),
                skillset = x.Key.Skillset.ToString(),
                scoring = x.Key.Scoring.ToString(),
                rating = Math.Round(x.Value.Ordinal),
                is_estimated = x.Value.IsEstimated
            }));
    }

    private async Task<(int userId, Player? player)> GetUserId(string username)
    {
        if (!username.StartsWith('@'))
        {
            if (!int.TryParse(username, out var userId))
                return default;
            return (userId, null);
        }

        username = username[1..].Trim();

        var player = await playerService.GetPlayerByUsername(username);

        if (player is null) return default;

        return (player.PlayerId, player);
    }

    private async Task<double> GetEstimation(int ratingAttributeId, int globalRank, CancellationToken token)
    {
        return await context.Ratings
            .AsNoTracking()
            .Where(x => x.RatingAttributeId == ratingAttributeId)
            .OrderBy(x => Math.Abs(globalRank - x.Player.GlobalRank!.Value))
            .Take(100)
            .Select(x => x.Ordinal)
            .AverageAsync(token);
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