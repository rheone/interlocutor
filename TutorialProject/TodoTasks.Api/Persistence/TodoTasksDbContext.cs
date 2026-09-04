namespace TodoTasks.Api.Persistence;

using Microsoft.EntityFrameworkCore;
using TodoTasks.Api.Domain;

/// <summary>
/// The EF Core <see cref="DbContext"/> this project runs on, against an in-memory SQLite database - see
/// <c>Program.cs</c> for the connection strategy that keeps that database alive across the scoped
/// instances of this type each request gets.
/// </summary>
/// <remarks>
/// Each feature ticket that lands a mapped entity adds its own <c>DbSet&lt;TEntity&gt;</c> property and
/// <c>OnModelCreating</c> configuration here, the same way
/// <c>documentation/sample-app-walkthrough.md</c>'s <c>Domain/</c>/<c>Persistence/</c> folders describe
/// entities and mappings as the two things shared across every vertical slice.
/// </remarks>
/// <param name="options">The options this context is configured with - see <c>Program.cs</c>'s <c>AddDbContext</c> call.</param>
public sealed class TodoTasksDbContext(DbContextOptions<TodoTasksDbContext> options) : DbContext(options)
{
    /// <summary>Gets the set of registered <see cref="User"/> entities.</summary>
    /// <value>The <see cref="DbSet{TEntity}"/> backing <see cref="User"/> queries and writes.</value>
    public DbSet<User> Users => this.Set<User>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mirrors the shape documentation/beginners-guide-todo-api.md Step 4 gives TodoTaskMap under
        // NHibernate: a required, length-bounded DisplayName and Email. The unique index on Email is
        // defense-in-depth at the schema level - Features/Users/CreateUser/CreateUserHandler still does
        // its own pre-check query (inside CreateUserCommand's eager ITransactionalRequest transaction) to
        // translate a duplicate into the Conflict outcome case itself, rather than relying on this index
        // throwing DbUpdateException and mapping that exception back into an outcome.
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            user.Property(u => u.Email).IsRequired().HasMaxLength(320); // 320 = the longest an RFC 5321 email can be.
            user.HasIndex(u => u.Email).IsUnique();
        });
    }
}
