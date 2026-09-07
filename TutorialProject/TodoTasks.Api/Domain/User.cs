namespace TodoTasks.Api.Domain;

/// <summary>
/// A registered caller of this API. Backs two things at once: the identity a future <c>TodoTask.OwnerId</c>
/// refers to, and the record a future development-only authentication handler checks an incoming caller's
/// claimed id against.
/// </summary>
/// <remarks>
/// Mirrors the EF Core mapping conventions <c>documentation/beginners-guide-todo-api.md</c> Step 3
/// establishes for its own <c>TodoTask</c> entity - a private parameterless constructor for EF Core's own
/// materialization, plain (non-virtual) auto-properties since EF Core's default snapshot-based change
/// tracking, unlike NHibernate, needs no lazy-loading proxy generation. Mapped by
/// <c>Persistence/TodoTasksDbContext.OnModelCreating</c>. This entire feature is this project's own
/// addition, not part of the guide - the guide only models <c>TodoTask</c>.
/// </remarks>
public class User
{
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class. EF Core needs a parameterless
    /// constructor to materialize entities from query results - it never calls this directly for
    /// entities constructed by application code.
    /// </summary>
    private User()
    {
        this.DisplayName = string.Empty;
        this.Email = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="email">The user's email address - unique among registered users (see <c>Features/Users/CreateUser/CreateUserHandler</c>).</param>
    public User(string displayName, string email)
        : this()
    {
        this.DisplayName = displayName;
        this.Email = email;
    }

    /// <summary>Gets or sets the user's identifier.</summary>
    /// <value>The identifier assigned by the database on insert.</value>
    public int Id { get; set; }

    /// <summary>Gets or sets the user's display name.</summary>
    /// <value>The user's display name.</value>
    public string DisplayName { get; set; }

    /// <summary>Gets or sets the user's email address.</summary>
    /// <value>The user's email address.</value>
    public string Email { get; set; }
}
