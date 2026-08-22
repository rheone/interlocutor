namespace Interlocutor.Sample.Handlers.Todos;

/// <summary>
/// A process-lifetime tally of how many to-do items have been created, kept independent of the database so
/// <see cref="TodoCreatedCounterHandler"/> has application state to touch that isn't just another write to
/// <see cref="SampleDbContext"/>.
/// </summary>
public interface ITodoCreationCounter
{
    /// <summary>Gets the number of to-do items created since the process started.</summary>
    /// <value>The number of to-do items created since the process started.</value>
    int Count { get; }

    /// <summary>Increments the count by one.</summary>
    void Increment();
}
