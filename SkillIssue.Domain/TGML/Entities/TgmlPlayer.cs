// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

namespace SkillIssue.Domain.TGML.Entities;

public class TgmlPlayer
{
    public int PlayerId { get; set; }
    public string CurrentUsername { get; set; } = null!;

    public IList<TgmlMatch> Matches { get; set; }
}