namespace TodoTasks.Api.Infrastructure;

using System.Collections.Concurrent;

/// <summary>The default, process-lifetime, in-memory <see cref="ICompletionCounter"/> implementation.</summary>
public sealed class InMemoryCompletionCounter : ICompletionCounter
{
    private readonly ConcurrentDictionary<string, int> counts = new();

    /// <inheritdoc/>
    public void Increment(string ownerId) => this.counts.AddOrUpdate(ownerId, 1, (_, count) => count + 1);

    /// <inheritdoc/>
    public int CountFor(string ownerId) => this.counts.GetValueOrDefault(ownerId);
}
