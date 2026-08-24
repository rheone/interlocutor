namespace Interlocutor.Sample.Handlers.Tags;

/// <summary>
/// A label a to-do item could be organized under. The sample never wires this to <see cref="Todos.TodoItem"/> -
/// it exists solely to give the generic <c>GetByIdQuery&lt;TEntity&gt;</c> handler a second entity type to
/// serve, alongside <see cref="Todos.TodoItem"/>, so it's genuinely used for more than one request type.
/// </summary>
public class TagItem
{
    /// <summary>Gets or sets the entity's primary key, assigned by the database on insert.</summary>
    /// <value>The entity's primary key.</value>
    public int Id { get; set; }

    /// <summary>Gets or sets the tag's display name.</summary>
    /// <value>The tag's display name.</value>
    public required string Name { get; set; }
}
