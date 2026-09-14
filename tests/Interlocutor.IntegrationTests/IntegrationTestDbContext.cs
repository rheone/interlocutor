namespace Interlocutor.IntegrationTests;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// The single-entity <see cref="DbContext"/> this project's real EF Core unit of work commits through - built
/// against a Sqlite in-memory connection the same way <c>Interlocutor.Persistence.EntityFrameworkCore.Tests</c>'s
/// own <c>EntityFrameworkTestFixture</c> does, so this project's real transactional commit exercises the same
/// provider the rest of the solution already relies on for realistic transaction semantics.
/// </summary>
/// <param name="options">The context options, built with <c>UseSqlite</c> against a dedicated in-memory connection.</param>
public sealed class IntegrationTestDbContext(DbContextOptions<IntegrationTestDbContext> options) : DbContext(options)
{
    /// <summary>Gets the mapped <see cref="Widget"/> set.</summary>
    /// <value>The <see cref="DbSet{TEntity}"/> for <see cref="Widget"/>.</value>
    public DbSet<Widget> Widgets => this.Set<Widget>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Widget>();
}
