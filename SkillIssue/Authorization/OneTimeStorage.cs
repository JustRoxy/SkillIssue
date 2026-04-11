using System.Collections.Concurrent;

namespace SkillIssue.Authorization;

public class OneTimeStorage
{
    private readonly ConcurrentDictionary<object, object?> _oneTimeStorage = [];

    public TValue? Get<TKey, TValue>(TKey key) where TValue : class
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_oneTimeStorage.TryRemove(key, out var value)) return null;

        return value as TValue;
    }

    public void Set<TKey, TValue>(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        _oneTimeStorage[key] = value;
    }

    public bool Contains<TKey>(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _oneTimeStorage.ContainsKey(key);
    }
}