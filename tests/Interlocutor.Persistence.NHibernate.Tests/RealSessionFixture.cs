namespace Interlocutor.Persistence.NHibernate.Tests;

using System.Data.SQLite;
using global::NHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using global::NHibernate.Tool.hbm2ddl;

/// <summary>
/// An xunit class fixture giving this project's transaction-lifecycle tests a real, fresh in-memory
/// SQLite-backed NHibernate <see cref="ISessionFactory"/>, mapped for <see cref="Widget"/> - the same
/// shared-cache-mode mechanism <c>TodoTasksSessionFactory.Build</c>
/// (<c>TutorialProject/TodoTasks.Api/Persistence/TodoTasksSessionFactory.cs</c>) established,
/// reproduced here since this package has no dependency on that sample project. Tests that use this
/// fixture open real sessions against real transactions, replacing the NSubstitute-mocked
/// <see cref="ISession"/>/<see cref="ITransaction"/> stand-ins those tests used to hand-simulate
/// transaction side effects with.
/// </summary>
public sealed class RealSessionFixture : IDisposable
{
    private readonly SQLiteConnection anchorConnection;

    /// <summary>Initializes a new instance of the <see cref="RealSessionFixture"/> class, building a fresh in-memory session factory.</summary>
    public RealSessionFixture()
    {
        // A fresh name per instance - a shared, un-named connection string would let every test class
        // using this fixture collide on one database (see TodoTasksSessionFactory's own remarks).
        var connectionString = $"FullUri=file:nhibernate-tests-{Guid.NewGuid():N}?mode=memory&cache=shared;Version=3;";

        this.anchorConnection = new SQLiteConnection(connectionString);
        this.anchorConnection.Open();

        this.SessionFactory = Fluently
            .Configure()
            .Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
            .Mappings(m => m.FluentMappings.Add<WidgetMap>())
            .ExposeConfiguration(configuration =>
                new SchemaExport(configuration).Execute(
                    useStdOut: false,
                    execute: true,
                    justDrop: false,
                    connection: this.anchorConnection,
                    exportOutput: null
                )
            )
            .BuildSessionFactory();
    }

    /// <summary>Gets the session factory this fixture's tests open sessions against.</summary>
    /// <value>The <see cref="ISessionFactory"/> built against this fixture's own isolated in-memory database.</value>
    public ISessionFactory SessionFactory { get; }

    /// <summary>Opens a new, independent <see cref="ISession"/> against this fixture's shared-cache database.</summary>
    /// <returns>A new <see cref="ISession"/>, ready to begin its own transaction.</returns>
    public ISession OpenSession() => this.SessionFactory.OpenSession();

    /// <summary>Closes the fixture's anchor connection, discarding its in-memory database.</summary>
    public void Dispose()
    {
        this.SessionFactory.Dispose();
        this.anchorConnection.Dispose();
    }

    private sealed class WidgetMap : ClassMap<Widget>
    {
        // Client-generated (see Widget's own remarks for why this - not an identity/auto-increment
        // column - matters to the tests using this fixture).
        public WidgetMap() => this.Id(w => w.Id).GeneratedBy.Guid();
    }
}
