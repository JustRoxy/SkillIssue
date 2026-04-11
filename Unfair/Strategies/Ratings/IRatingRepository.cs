// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies.Ratings;

public interface IRatingRepository
{
    public bool GetRating(int playerId, int ratingAttributeId, out Rating rating);
    public void SetRating(Rating rating);
}