using MediatR;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using TheGreatSpy.Services;
using Unfair.Strategies;

namespace SkillIssue.API.Commands;

public class GetPlayerRatingsRequest : IRequest<GetPlayerRatingsResponse?>
{
    public int PlayerId { get; set; }
}

public class GetPlayerRatingsResponse
{
    public string ActiveUsername { get; set; }
    public string CountryCode { get; set; }
    public int PlayerId { get; set; }
    public ResponseRating Rating { get; set; }
    public Dictionary<string, ResponseRating> Modifications { get; set; }
    public Dictionary<string, ResponseRating> Skillsets { get; set; }

    public class ResponseRating
    {
        public double Accuracy { get; set; }
        public double Combo { get; set; }
        public int CountryRank { get; set; }
        public int GlobalRank { get; set; }
        public string Name { get; set; }
        public int PP { get; set; }
        public double SR { get; set; }
        public double Value { get; set; }
    }
}

public class GetPlayerRatingsHandler(
    DatabaseContext context,
    PlayerService playerService,
    IOpenSkillCalculator calculator)
    : IRequestHandler<GetPlayerRatingsRequest, GetPlayerRatingsResponse?>
{
    public async Task<GetPlayerRatingsResponse?> Handle(GetPlayerRatingsRequest request,
        CancellationToken cancellationToken)
    {
        var player = await playerService.GetPlayerById(request.PlayerId);
        if (player is null) return null;

        var ratings = await context.Ratings
            .AsNoTracking()
            .Include(x => x.RatingAttribute)
            .Where(x => x.PlayerId == player.PlayerId
                        && RatingAttribute.MajorAttributes.Contains(x.RatingAttributeId))
            .Select(x => new
            {
                Rating = new Rating
                {
                    RatingAttribute = x.RatingAttribute,
                    RatingAttributeId = x.RatingAttributeId,
                    Mu = x.Mu,
                    Sigma = x.Sigma,
                    Ordinal = x.Ordinal,
                    StarRating = x.StarRating
                },
                Ranking = new
                {
                    GlobalRank = x.RatingAttribute.Scoring != ScoringRatingAttribute.Score
                        ? 0
                        : context.Ratings.Count(z =>
                            z.RatingAttributeId == x.RatingAttributeId && z.Ordinal > x.Ordinal) + 1,
                    CountryRank = x.RatingAttribute.Scoring != ScoringRatingAttribute.Score
                        ? 0
                        : context.Ratings.Count(z =>
                            z.RatingAttributeId == x.RatingAttributeId && z.Ordinal > x.Ordinal &&
                            z.Player.CountryCode == player.CountryCode) + 1
                }
            })
            .ToListAsync(cancellationToken);

        if (ratings.Count == 0)
        {
            return new GetPlayerRatingsResponse
            {
                ActiveUsername = player.ActiveUsername,
                CountryCode = player.CountryCode,
                PlayerId = player.PlayerId
            };
        }

        (List<Rating>, (int globalRank, int countryRank), string name, bool isModificationRating) SelectRatings(
            Func<RatingAttribute, bool> selector)
        {
            var localRatings = ratings.Where(x => selector(x.Rating.RatingAttribute)).ToList();
            var scoreRating = localRatings.First(x => x.Rating.RatingAttribute.Scoring == ScoringRatingAttribute.Score);

            var ratingAttribute = scoreRating.Rating.RatingAttribute;

            var modificationMajor = ratingAttribute.IsModificationMajorAttribute;
            var skillsetMajor = ratingAttribute.IsSkillsetMajorAttribute
                                //TODO: did i really shot my knee with HighAR/DT bullshit?
                                || ratingAttribute is
                                {
                                    Skillset: SkillsetRatingAttribute.HighAR,
                                    Modification: ModificationRatingAttribute.DT
                                };
            var name = ratingAttribute.IsGlobalMajorAttribute ? "rating"
                : modificationMajor ? RatingAttribute.DescriptionFormat(ratingAttribute.Modification)
                : skillsetMajor ? RatingAttribute.DescriptionFormat(ratingAttribute.Skillset)
                : throw new ArgumentOutOfRangeException(ratingAttribute.AttributeId.ToString());

            return (localRatings.Select(x => x.Rating).ToList(),
                (scoreRating.Ranking.GlobalRank, scoreRating.Ranking.CountryRank), name,
                modificationMajor);
        }

        var ratingGroupings = ratings
            .GroupBy(x => (x.Rating.RatingAttribute.Modification, x.Rating.RatingAttribute.Skillset))
            .ToList();

        var selectedRatings = ratingGroupings
            .Select(x => SelectRatings(z => z.Modification == x.Key.Modification && z.Skillset == x.Key.Skillset))
            .ToList();

        var globalRatings =
            selectedRatings.FirstOrDefault(x => x.Item1.Any(z => z.RatingAttribute.IsGlobalMajorAttribute));

        var modificationRatings = selectedRatings.Where(x => x.isModificationRating && x != globalRatings)
            .Select(x => ToResponseRating(x.Item1, x.Item2, x.name))
            .ToList();

        var skillsetRatings = selectedRatings.Where(x => !x.isModificationRating && x != globalRatings)
            .Select(x => ToResponseRating(x.Item1, x.Item2, x.name))
            .ToList();

        var response = new GetPlayerRatingsResponse
        {
            PlayerId = player.PlayerId,
            ActiveUsername = player.ActiveUsername,
            CountryCode = player.CountryCode,
            Rating = ToResponseRating(globalRatings.Item1, globalRatings.Item2, globalRatings.name),
            Modifications = modificationRatings.ToDictionary(x => x.Name),
            Skillsets = skillsetRatings.ToDictionary(x => x.Name)
        };

        return response;
    }

    private GetPlayerRatingsResponse.ResponseRating ToResponseRating(List<Rating> ratings,
        (int globalRank, int countryRank) ranking, string name)
    {
        var scoreRating = ratings.First(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.Score);
        var sr = scoreRating.StarRating;
        var value = scoreRating.Ordinal;
        var pp = ratings.FirstOrDefault(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.PP)?.Ordinal ?? 0;

        var accuracy = ratings.First(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.Accuracy);
        var combo = ratings.First(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.Combo);

        var accuracyPrediction = calculator.PredictWinHeadOnHead(accuracy, combo)[0];

        return new GetPlayerRatingsResponse.ResponseRating
        {
            Accuracy = Math.Round(accuracyPrediction, 2),
            Combo = Math.Round(1 - accuracyPrediction, 2),
            CountryRank = ranking.countryRank,
            GlobalRank = ranking.globalRank,
            Name = name,
            PP = (int)Math.Round(pp),
            SR = Math.Round(sr, 2),
            Value = (int)Math.Round(value)
        };
    }
}