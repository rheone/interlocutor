namespace TodoTasks.Api.Infrastructure;

/// <summary>
/// The default, process-lifetime, in-memory <see cref="ICompletionCounter"/> implementation. Registered
/// as a singleton in <c>Program.cs</c> so counts persist across requests rather than being reset per
/// scope.
/// </summary>
public sealed class InMemoryCompletionCounter : ICompletionCounter
{
    private readonly ConcurrentDictionary<string, int> counts = new();

    /// <inheritdoc/>
    public void Increment(string ownerId) => this.counts.AddOrUpdate(ownerId, 1, (_, count) => count + 1);

    /// <inheritdoc/>
    public int CountFor(string ownerId) => this.counts.GetValueOrDefault(ownerId);
}
