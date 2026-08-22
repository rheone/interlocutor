namespace Interlocutor.Sample.Handlers.Todos;

/// <summary>The default, thread-safe <see cref="ITodoCreationCounter"/>, registered as a singleton.</summary>
public sealed class TodoCreationCounter : ITodoCreationCounter
{
    private int _count;

    /// <inheritdoc/>
    public int Count => this._count;

    /// <inheritdoc/>
    public void Increment() => Interlocked.Increment(ref this._count);
}
