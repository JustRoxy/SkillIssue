// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using SkillIssue.Domain.TGML.Entities;

namespace SkillIssue.Domain.Events.Matches;

public class MatchFound : BaseEvent
{
    public required TgmlMatch Match { get; set; }
}