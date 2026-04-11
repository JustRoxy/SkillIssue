// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

namespace SkillIssue.Authorization;

public class ApiAuthorizationConfiguration
{
    public List<string> AllowedSources { get; set; }

    public bool IsAllowed(string source)
    {
        return !string.IsNullOrWhiteSpace(source) && AllowedSources.Contains(source);
    }
}