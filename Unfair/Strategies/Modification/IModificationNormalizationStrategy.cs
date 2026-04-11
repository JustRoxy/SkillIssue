// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using osu.Game.Beatmaps.Legacy;

namespace Unfair.Strategies.Modification;

public interface IModificationNormalizationStrategy
{
    public LegacyMods Normalize(LegacyMods mod);
}