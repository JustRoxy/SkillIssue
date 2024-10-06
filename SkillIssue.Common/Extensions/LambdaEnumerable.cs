namespace SkillIssue.Common.Extensions;

public class LambdaEnumerable<T>(
    Func<T, Task<T>> getNext,
    Func<T, bool> hasNext,
    Func<ValueTask> dispose)
    : IAsyncEnumerable<T>
{
    private class LambdaEnumerator(
        Func<T, Task<T>> getNext,
        Func<T, bool> hasNext,
        Func<ValueTask> dispose)
        : IAsyncEnumerator<T>
    {
        public ValueTask DisposeAsync()
        {
            return dispose();
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (!hasNext(Current)) return false;

            Current = await getNext(Current);
            return true;
        }

        public T Current { get; private set; } = default!;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        return new LambdaEnumerator(getNext, hasNext, dispose);
    }
}