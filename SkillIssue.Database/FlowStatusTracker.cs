// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using SkillIssue.Domain;

namespace SkillIssue.Database;

public class FlowStatusTracker : BaseEntity
{
    public int MatchId { get; set; }
    public FlowStatus Status { get; set; }
}

public enum FlowStatus
{
    Created,
    TgmlFetched,
    Done
}
