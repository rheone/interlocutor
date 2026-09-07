namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// An xunit class fixture giving this project's EF Core-backed tests the shared <see cref="Widget"/> and
/// <see cref="TestDbContext"/> declarations, plus a factory for opening a fresh, isolated Sqlite in-memory
/// database - the same consolidation the sibling Interlocutor.Persistence.NHibernate.Tests project's
/// <c>RealSessionFixture</c> already does for its own tests, adapted here to EF Core's per-connection-lifetime
/// in-memory Sqlite semantics.
/// </summary>
/// <remarks>
/// Unlike <c>RealSessionFixture</c>, this fixture holds no connection or context of its own. xunit constructs
/// an <see cref="Xunit.IClassFixture{TFixture}"/> instance once per test class and shares that one instance
/// across every test method in the class, but this project's tests rely on each test method starting from a
/// genuinely empty database - sharing one open connection across a class's test methods would leak rows
/// between them. So each consuming test class instead calls <see cref="CreateDatabase"/> from its own
/// constructor - which xunit does construct fresh for every test method - to obtain its own dedicated
/// connection and context, keeping exactly the per-test isolation these tests had before this fixture existed.
/// </remarks>
// S2325 wants CreateDatabase/CreateContext static since neither reads instance state, but they are
// deliberately shaped as ordinary instance members so this type can be constructor-injected as an
// IClassFixture<EntityFrameworkTestFixture> like the sibling project's RealSessionFixture, rather than
// forcing every consuming test class to call a static factory type instead.
#pragma warning disable S2325
public sealed class EntityFrameworkTestFixture
{
    /// <summary>
    /// Opens a fresh Sqlite in-memory connection, builds a <see cref="TestDbContext"/> against it, and
    /// ensures its schema exists - the full per-test setup every consuming test class previously duplicated.
    /// </summary>
    /// <returns>
    /// The open <see cref="SqliteConnection"/> and the <see cref="TestDbContext"/> built against it, both
    /// owned by the caller and disposed by it once its test completes.
    /// </returns>
    public (SqliteConnection Connection, TestDbContext Context) CreateDatabase()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var context = this.CreateContext(connection);
        context.Database.EnsureCreated();

        return (connection, context);
    }

    /// <summary>
    /// Builds another <see cref="TestDbContext"/> against an already-open connection - for a verification
    /// read through a second context instance, so the read reflects what actually reached the database rather
    /// than the first-level cache of the context that wrote it.
    /// </summary>
    /// <param name="connection">An open connection, typically one previously returned by <see cref="CreateDatabase"/>.</param>
    /// <returns>A new <see cref="TestDbContext"/> sharing the given connection's database.</returns>
    public TestDbContext CreateContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);

    /// <summary>
    /// Builds a <see cref="TestDbContext"/> against an already-open connection with the given save-changes
    /// interceptors registered - for the one test that must observe EF Core's own interceptor pipeline
    /// directly, rather than through <see cref="EntityFrameworkUnitOfWork{TContext}"/> or
    /// <c>UnitOfWorkBehavior{TRequest,TResponse,TContext}</c>.
    /// </summary>
    /// <param name="connection">An open connection, typically one previously returned by <see cref="CreateDatabase"/>.</param>
    /// <param name="interceptors">The save-changes interceptors to register on the resulting context.</param>
    /// <returns>A new <see cref="TestDbContext"/> sharing the given connection's database, with <paramref name="interceptors"/> registered.</returns>
    public TestDbContext CreateContext(SqliteConnection connection, params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).AddInterceptors(interceptors).Options);
}
#pragma warning restore S2325
