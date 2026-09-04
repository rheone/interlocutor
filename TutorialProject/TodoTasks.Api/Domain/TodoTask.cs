namespace TodoTasks.Api.Domain;

/// <summary>
/// A single to-do item, owned by the caller who created it.
/// </summary>
/// <remarks>
/// Mirrors the EF Core mapping conventions <see cref="User"/>'s own remarks describe - a private
/// parameterless constructor for EF Core's own materialization, plain (non-virtual) auto-properties,
/// since EF Core's default snapshot-based change tracking needs no lazy-loading proxy generation.
/// Mapped by <c>Persistence/TodoTasksDbContext.OnModelCreating</c>. Follows the exact field set
/// <c>documentation/beginners-guide-todo-api.md</c> Step 3 walks through for this entity.
/// </remarks>
public class TodoTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TodoTask"/> class. EF Core needs a parameterless
    /// constructor to materialize entities from query results - it never calls this directly for
    /// entities constructed by application code.
    /// </summary>
    private TodoTask()
    {
        this.Title = string.Empty;
        this.OwnerId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="TodoTask"/> class.</summary>
    /// <param name="title">The task's title.</param>
    /// <param name="ownerId">
    /// The authenticated caller's id who owns this task - never trust this from a request body, only
    /// ever set it from the authenticated caller (see
    /// <c>Features/Tasks/CreateTodoTask/CreateTodoTaskEndpoint</c>).
    /// </param>
    /// <param name="notes">Optional free-text notes.</param>
    public TodoTask(string title, string ownerId, string? notes)
        : this()
    {
        this.Title = title;
        this.OwnerId = ownerId;
        this.Notes = notes;
        this.CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets or sets the task's identifier.</summary>
    /// <value>The identifier assigned by the database on insert.</value>
    public int Id { get; set; }

    /// <summary>Gets or sets the task's title.</summary>
    /// <value>The task's title.</value>
    public string Title { get; set; }

    /// <summary>Gets or sets the task's optional free-text notes.</summary>
    /// <value>The task's notes, or <see langword="null"/> if none were provided.</value>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the id of the authenticated caller who owns this task. Deliberately an opaque
    /// string, not a foreign key to a <see cref="User"/> row - see <see cref="User"/>'s own remarks for
    /// why that stays true even though this project also models <see cref="User"/>.
    /// </summary>
    /// <value>The owning caller's id.</value>
    public string OwnerId { get; set; }

    /// <summary>Gets or sets a value indicating whether this task has been completed.</summary>
    /// <value><see langword="true"/> if the task is complete; otherwise <see langword="false"/>.</value>
    public bool IsCompleted { get; set; }

    /// <summary>Gets or sets when this task was created.</summary>
    /// <value>The UTC creation timestamp.</value>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
