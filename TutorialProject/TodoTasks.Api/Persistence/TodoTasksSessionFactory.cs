namespace TodoTasks.Api.Persistence;

using System.Data.SQLite;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

/// <summary>
/// Builds the NHibernate <see cref="ISessionFactory"/> this project runs on, along with the one
/// ADO.NET connection that has to stay open for the app's entire lifetime for an in-memory SQLite
/// database to work at all.
/// </summary>
/// <remarks>
/// <para>
/// An in-memory SQLite database (<c>Data Source=:memory:</c>) exists only as long as its one
/// connection stays open - the instant that connection closes, SQLite discards the database, and
/// the next connection opened from the same connection string starts a brand new, empty one.
/// NHibernate's usual pattern - one <see cref="ISession"/> per request, each opening and closing
/// its own physical connection - would hand every single request a fresh, empty database, which is
/// never what this project wants.
/// </para>
/// <para>
/// The fix: this class opens exactly one <see cref="SQLiteConnection"/> and returns it alongside
/// the <see cref="ISessionFactory"/> built against it, so the caller (see <c>Program.cs</c>) can
/// register the connection as a singleton and keep it open for as long as the app runs. Every
/// <see cref="ISession"/> opened afterward - see <c>Program.cs</c>'s scoped
/// <c>Lazy&lt;ISession&gt;</c> registration - is opened explicitly against that same connection via
/// <c>ISessionFactory.WithOptions().Connection(...)</c>, so every session in the app shares the one
/// database that connection is keeping alive. This is the one deliberate deviation from
/// <c>documentation/beginners-guide-todo-api.md</c> Step 4, which targets a real, persistent SQL
/// Server database and so never needs to think about a connection's lifetime this way.
/// </para>
/// </remarks>
public static class TodoTasksSessionFactory
{
    /// <summary>The connection string every session in this project ultimately shares.</summary>
    private const string InMemoryConnectionString = "Data Source=:memory:;Version=3;New=True;";

    /// <summary>
    /// Opens the app's one long-lived SQLite connection, builds an <see cref="ISessionFactory"/>
    /// mapped against every Fluent NHibernate <c>ClassMap&lt;TEntity&gt;</c> in this assembly, and
    /// creates the schema those mappings describe. Call this exactly once, at startup.
    /// </summary>
    /// <returns>
    /// The kept-alive connection (register it as a singleton so it never closes while the app is
    /// running) and the session factory built against it.
    /// </returns>
    public static (SQLiteConnection Connection, ISessionFactory SessionFactory) Build()
    {
        var connection = new SQLiteConnection(InMemoryConnectionString);
        connection.Open();

        var sessionFactory = Fluently
            .Configure()
            // This connection string only satisfies NHibernate's own configuration validation - no
            // session ever actually opens a connection from it. Every real session opens explicitly
            // against the "connection" instance above instead (see this type's own remarks).
            .Database(SQLiteConfiguration.Standard.ConnectionString(InMemoryConnectionString))
            // Picks up every FluentNHibernate ClassMap<TEntity> in this assembly automatically - a
            // new feature that adds a mapped entity (a new Persistence/*Map.cs file) needs no change
            // here to be included.
            .Mappings(m => m.FluentMappings.AddFromAssembly(typeof(TodoTasksSessionFactory).Assembly))
            .ExposeConfiguration(configuration =>
            {
                // There are no migrations for this proof-of-concept - the schema is (re)created from
                // the current mappings every time the app starts, against the connection above so
                // it's actually visible to every session that later opens against that same connection.
                new SchemaExport(configuration).Execute(
                    useStdOut: false,
                    execute: true,
                    justDrop: false,
                    connection: connection,
                    exportOutput: null
                );
            })
            .BuildSessionFactory();

        return (connection, sessionFactory);
    }
}
