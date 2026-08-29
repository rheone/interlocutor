namespace TodoTasks.Api.Persistence;

using System.Data.SQLite;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

/// <summary>
/// Builds the NHibernate <see cref="ISessionFactory"/> this project runs on, along with the one
/// "anchor" ADO.NET connection that has to stay open for the app's entire lifetime for an in-memory
/// SQLite database to survive between requests.
/// </summary>
/// <remarks>
/// <para>
/// <b>The problem.</b> An in-memory SQLite database (<c>Data Source=:memory:</c>) exists only as
/// long as at least one connection to it stays open - the instant the last connection closes, SQLite
/// discards the database. NHibernate's usual pattern - one <see cref="ISession"/> per request, each
/// opening and closing its own physical connection - would normally hand every single request a
/// fresh, empty database, since nothing else keeps the previous connection (and its data) alive.
/// </para>
/// <para>
/// <b>The fix that looks obvious but doesn't work.</b> The first approach this project tried was
/// forcing every session to share one single, already-open <see cref="SQLiteConnection"/> object via
/// <c>ISessionFactory.WithOptions().Connection(...)</c>. That compiles, runs, and even round-trips
/// data correctly in a simple two-session smoke test - but breaks silently under NHibernate's real
/// session lifecycle: NHibernate assumes it manages a session's connection lifecycle even when the
/// connection is user-supplied, and something in that lifecycle (verified empirically, not by reading
/// documentation alone - neither <c>ConnectionReleaseMode.OnClose</c> nor <c>AfterTransaction</c>
/// changed the outcome) ends up resetting the shared in-memory database between sessions, even though
/// the connection object's own <see cref="System.Data.IDbConnection.State"/> reports
/// <see cref="System.Data.ConnectionState.Open"/> throughout and a direct raw ADO.NET query against
/// it confirms the data is gone. A second request's read of a first request's already-committed
/// insert would silently find nothing - exactly the kind of bug that only shows up under real,
/// independent request-scoped sessions, not a same-process two-session unit test.
/// </para>
/// <para>
/// <b>The fix that actually works: SQLite's own shared-cache mode.</b> Every <see cref="ISession"/>
/// opens its own independent connection, exactly the way NHibernate expects - no connection object is
/// forced across sessions. What keeps them all looking at the *same* database is the connection
/// string itself: <c>FullUri=file:{name}?mode=memory&amp;cache=shared</c> tells SQLite's native
/// shared-cache feature to back every connection opened with that exact URI by the same in-memory
/// database, for as long as at least one connection to it remains open somewhere in the process. That
/// "somewhere" is the one anchor connection this method opens and returns - callers (see
/// <c>Program.cs</c>) register it as a singleton purely so it never closes while the app runs. No
/// code ever queries through the anchor connection directly; it exists only to keep the shared cache
/// alive.
/// </para>
/// <para>
/// <b>The name matters.</b> The <c>{name}</c> in that URI is not decorative - SQLite's shared-cache
/// databases are identified by name (verified empirically: the bare, unnamed <c>file::memory:</c>
/// form is process-wide, so two unrelated callers opening it "at the same time" - two different test
/// fixtures, or a test fixture running alongside the app under test - would silently share one
/// database and contaminate each other's data). This method generates a fresh <see cref="Guid"/> per
/// call specifically so every call - one for the running app, one per test fixture that also calls
/// this method - gets its own isolated database, while still sharing correctly across every session
/// *within* that one call.
/// </para>
/// </remarks>
public static class TodoTasksSessionFactory
{
    /// <summary>
    /// Opens this call's own anchor SQLite connection, on a freshly named shared-cache in-memory
    /// database (kept open purely to keep that database alive - see this type's own remarks), builds
    /// an <see cref="ISessionFactory"/> mapped against every Fluent NHibernate
    /// <c>ClassMap&lt;TEntity&gt;</c> in this assembly, and creates the schema those mappings
    /// describe. Call this once per independent database you want (once at app startup; once per test
    /// fixture that needs its own isolated in-memory database).
    /// </summary>
    /// <returns>
    /// The anchor connection (register it as a singleton, or otherwise keep it alive for as long as
    /// the database needs to exist) and the session factory built against the same shared in-memory
    /// database.
    /// </returns>
    public static (SQLiteConnection AnchorConnection, ISessionFactory SessionFactory) Build()
    {
        // A fresh name per call - see this type's remarks for why a shared, un-named connection string
        // would silently let every caller of this method collide on one database.
        var connectionString = $"FullUri=file:todotasks-{Guid.NewGuid():N}?mode=memory&cache=shared;Version=3;";

        var anchorConnection = new SQLiteConnection(connectionString);
        anchorConnection.Open();

        var sessionFactory = Fluently
            .Configure()
            .Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
            // Picks up every FluentNHibernate ClassMap<TEntity> in this assembly automatically - a
            // new feature that adds a mapped entity (a new Persistence/*Map.cs file) needs no change
            // here to be included.
            .Mappings(m => m.FluentMappings.AddFromAssembly(typeof(TodoTasksSessionFactory).Assembly))
            .ExposeConfiguration(configuration =>
            {
                // There are no migrations for this proof-of-concept - the schema is (re)created from
                // the current mappings every time the app starts, against the anchor connection so
                // it's visible to every session opened afterward against the same shared-cache database.
                new SchemaExport(configuration).Execute(
                    useStdOut: false,
                    execute: true,
                    justDrop: false,
                    connection: anchorConnection,
                    exportOutput: null
                );
            })
            .BuildSessionFactory();

        return (anchorConnection, sessionFactory);
    }
}
