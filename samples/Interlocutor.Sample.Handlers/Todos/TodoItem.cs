namespace Interlocutor.Sample.Handlers.Todos;

/// <summary>A single to-do list entry. The sample's primary persisted entity.</summary>
public class TodoItem
{
    /// <summary>Gets or sets the entity's primary key, assigned by the database on insert.</summary>
    /// <value>The entity's primary key.</value>
    public int Id { get; set; }

    /// <summary>Gets or sets the short description of what needs doing.</summary>
    /// <value>The item's title.</value>
    public required string Title { get; set; }

    /// <summary>Gets or sets a value indicating whether the item has been completed.</summary>
    /// <value><see langword="true"/> if the item is done; otherwise, <see langword="false"/>.</value>
    public bool IsDone { get; set; }
}
