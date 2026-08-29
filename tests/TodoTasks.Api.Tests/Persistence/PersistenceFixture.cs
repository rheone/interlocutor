namespace TodoTasks.Api.Tests.Persistence;

using System.Data.SQLite;
using NHibernate;
using TodoTasks.Api.Persistence;

/// <summary>
/// An xunit class fixture that gives one test class its own real, fresh in-memory SQLite database -
/// mapped, schema-exported, and ready for <see cref="ISession"/>s to open against it - without that
/// test class having to know anything about how the app itself wires NHibernate up.
/// </summary>
/// <remarks>
/// This calls <see cref="TodoTasksSessionFactory.Build"/> - the exact same method <c>Program.cs</c>
/// calls at startup - rather than re-implementing its shared-cache-database setup as a separate,
/// test-only copy. That matters for more than avoiding duplication: if these tests used a different
/// connection/session strategy than the running app, a mapping bug that only shows up under the app's
/// real strategy could pass here while still breaking the app. Each call to
/// <see cref="TodoTasksSessionFactory.Build"/> opens its own uniquely-named shared-cache database (see
/// that method's own remarks), so every test class that uses this fixture gets a database no other
/// test class - or the running app under test in <c>Http/TodoTasksApiFactory</c> - can contaminate.
/// </remarks>
public sealed class PersistenceFixture : IDisposable
{
    private readonly SQLiteConnection anchorConnection;

    /// <summary>Initializes a new instance of the <see cref="PersistenceFixture"/> class, building a fresh in-memory session factory.</summary>
    public PersistenceFixture()
    {
        (this.anchorConnection, this.SessionFactory) = TodoTasksSessionFactory.Build();
    }

    /// <summary>
    /// Gets the session factory this fixture's tests open sessions against.
    /// </summary>
    /// <value>
    /// The <see cref="ISessionFactory"/> built by <see cref="TodoTasksSessionFactory.Build"/>, mapped
    /// and schema-exported against this fixture's own isolated in-memory database.
    /// </value>
    public ISessionFactory SessionFactory { get; }

    /// <summary>
    /// Opens a new <see cref="ISession"/> - the same <see cref="ISessionFactory.OpenSession()"/> call
    /// <c>Program.cs</c> uses for every request, so a test using this method sees the same
    /// session-sharing behavior the running app does.
    /// </summary>
    /// <returns>A new, independent <see cref="ISession"/> against this fixture's shared-cache database.</returns>
    public ISession OpenSession() => this.SessionFactory.OpenSession();

    /// <summary>Closes the fixture's anchor connection, discarding its in-memory database.</summary>
    public void Dispose()
    {
        this.SessionFactory.Dispose();
        this.anchorConnection.Dispose();
    }
}
