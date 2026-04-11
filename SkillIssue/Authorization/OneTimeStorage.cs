// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using System.Collections.Concurrent;

namespace SkillIssue.Authorization;

public class OneTimeStorage(ILogger<OneTimeStorage> logger)
{
    private readonly ConcurrentDictionary<object, object?> _oneTimeStorage = [];

    public TValue? Get<TKey, TValue>(TKey key) where TValue : class
    {
        ArgumentNullException.ThrowIfNull(key);
        logger.LogInformation("Requesting one-time storage key {Key}", key.ToString());

        if (!_oneTimeStorage.TryRemove(key, out var value)) return null;

        return value as TValue;
    }

    public void Set<TKey, TValue>(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        logger.LogInformation("Setting one-time storage key {Key}. Current amount: {Count}", key.ToString(), _oneTimeStorage.Count);
        _oneTimeStorage[key] = value;
    }

    public bool Contains<TKey>(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        logger.LogInformation("Checking if key {Key} exist in the pool of {Count}", key.ToString(), _oneTimeStorage.Count);
        return _oneTimeStorage.ContainsKey(key);
    }
}