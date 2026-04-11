// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

namespace SkillIssue.Domain.Discord;

public class InteractionState
{
    public ulong CreatorId { get; set; }
    public ulong MessageId { get; set; }

    public int? PlayerId { get; set; }
    public DateTime CreationTime { get; set; }

    public string StatePayload { get; set; } = null!;
}