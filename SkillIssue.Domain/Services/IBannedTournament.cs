// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.Domain.Services;

public interface IBannedTournament
{
    public bool IsBanned(TournamentMatch match);
}