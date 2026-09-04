namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

/// <summary>
/// A minimal entity mapped only so this project's EF Core-backed tests have something real to insert,
/// update, delete, and query - proving actual persistence, query-pushdown, and transaction mechanics against
/// a real Sqlite in-memory database (see <see cref="TestDbContext"/> and <see cref="EntityFrameworkTestFixture"/>)
/// rather than a hand-simulated approximation of them.
/// </summary>
public sealed class Widget
{
    /// <summary>Gets or sets the identifier Sqlite assigns on insert.</summary>
    /// <value>An auto-incrementing integer, assigned once the entity is saved through <see cref="TestDbContext"/>.</value>
    public int Id { get; set; }

    /// <summary>Gets or sets the entity's display name - the property most tests assert on.</summary>
    /// <value>An arbitrary string; defaults to empty.</value>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an arbitrary numeric value used by tests that filter, sort, or page.</summary>
    /// <value>An arbitrary integer; defaults to zero.</value>
    public int Value { get; set; }
}
