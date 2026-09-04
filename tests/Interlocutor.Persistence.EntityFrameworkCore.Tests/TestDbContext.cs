namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// A minimal <see cref="DbContext"/> mapping only <see cref="Widget"/> - shared across this project's EF
/// Core-backed tests via <see cref="EntityFrameworkTestFixture"/>, so every test exercises the same mapping
/// and query-pushdown behavior rather than a slightly different copy per test file.
/// </summary>
/// <param name="options">
/// The context options - built by <see cref="EntityFrameworkTestFixture"/> with <c>UseSqlite</c> against a
/// dedicated in-memory connection.
/// </param>
public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    /// <summary>Gets the mapped <see cref="Widget"/> set.</summary>
    /// <value>The <see cref="DbSet{TEntity}"/> for <see cref="Widget"/>.</value>
    public DbSet<Widget> Widgets => this.Set<Widget>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Widget>();
}
