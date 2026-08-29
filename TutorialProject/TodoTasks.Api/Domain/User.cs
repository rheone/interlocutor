namespace TodoTasks.Api.Domain;

/// <summary>
/// A registered caller of this API. Backs two things at once: the identity <c>TodoTask.OwnerId</c> refers
/// to (an upcoming ticket - see that type's own remarks for why that reference stays an opaque string
/// rather than a database foreign key), and the record the development-only authentication handler
/// (<c>Infrastructure/DevHeaderAuthenticationHandler</c>, added in a later ticket) checks an incoming
/// caller's claimed id against.
/// </summary>
/// <remarks>
/// Mirrors the NHibernate-mapping conventions <c>documentation/beginners-guide-todo-api.md</c> Step 3
/// establishes for its own <c>TodoTask</c> entity - a protected parameterless constructor for lazy-loading
/// proxies, every mapped member virtual for change tracking. Mapped by <c>Persistence/UserMap.cs</c>. This
/// entire feature is this project's own addition, not part of the guide - the guide only models
/// <c>TodoTask</c>.
/// </remarks>
public class User
{
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class. NHibernate needs a parameterless
    /// constructor to build lazy-loading proxies from query results - it never calls this directly for
    /// entities constructed by application code.
    /// </summary>
    protected User()
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

    // Virtual because NHibernate proxies override every mapped member for lazy loading and change
    // tracking - a non-virtual property here would silently never get tracked.

    /// <summary>Gets or sets the user's identifier.</summary>
    /// <value>The identifier assigned by the database on insert.</value>
    public virtual int Id { get; protected set; }

    /// <summary>Gets or sets the user's display name.</summary>
    /// <value>The user's display name.</value>
    public virtual string DisplayName { get; set; }

    /// <summary>Gets or sets the user's email address.</summary>
    /// <value>The user's email address.</value>
    public virtual string Email { get; set; }
}
