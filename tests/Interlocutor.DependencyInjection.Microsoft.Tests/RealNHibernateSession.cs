namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::NHibernate;
using global::NHibernate.Cfg;
using global::NHibernate.Dialect;
using global::NHibernate.Driver;

/// <summary>
/// Opens a real, mapping-free in-memory NHibernate <see cref="ISession"/> - shared by every test in this
/// project that needs <c>Lazy&lt;ISession&gt;</c> resolvable for the real <c>UnitOfWorkBehavior&lt;,&gt;</c>
/// without hand-simulating NHibernate's own transaction lifecycle. No entity mappings are needed: none of
/// this project's tests persist anything through the session, and <see cref="ISession.BeginTransaction()"/>,
/// <c>FlushMode</c>, and <c>IsDirty()</c> all work against an unmapped session.
/// </summary>
internal static class RealNHibernateSession
{
    /// <summary>Builds a fresh in-memory session factory and opens one session against it.</summary>
    /// <returns>A real <see cref="ISession"/> backed by an in-memory SQLite database.</returns>
    public static ISession Open() =>
        new Configuration()
            .DataBaseIntegration(db =>
            {
                db.Dialect<SQLiteDialect>();
                db.Driver<SQLite20Driver>();
                db.ConnectionString = "Data Source=:memory:;Version=3;";
            })
            .BuildSessionFactory()
            .OpenSession();
}
