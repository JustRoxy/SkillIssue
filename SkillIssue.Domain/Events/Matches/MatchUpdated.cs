// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using System.Text.Json.Nodes;
using SkillIssue.Domain.TGML.Entities;

namespace SkillIssue.Domain.Events.Matches;

public class MatchUpdated : BaseEvent
{
    public required TgmlMatch Match { get; set; }
    public required JsonObject DeserializedMatch { get; set; }
}