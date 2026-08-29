namespace Interlocutor.Persistence.NHibernate.Tests;

/// <summary>
/// A minimal entity mapped only so this project's transaction-lifecycle tests have something real to
/// insert, flush, and query - proving actual persistence and transaction mechanics against a real
/// in-memory NHibernate session rather than a hand-simulated approximation of them. See
/// <see cref="RealSessionFixture"/> for its mapping and the in-memory database it runs against.
/// </summary>
/// <remarks>
/// <see cref="Id"/> is client-generated (see <see cref="RealSessionFixture"/>'s <c>GeneratedBy.Guid()</c>
/// mapping) rather than database-identity-generated deliberately: an identity/auto-increment column forces
/// NHibernate to insert immediately on <c>Save()</c>, outside normal flush timing, which would make a
/// committed write indistinguishable from a rolled-back one by the time a test can observe it. A
/// client-generated id keeps the actual INSERT deferred to a real flush, same as every other mapped
/// property, so whether a row becomes visible to another session genuinely reflects commit vs. rollback.
/// </remarks>
public class Widget
{
    // Id is virtual because NHibernate proxies override it for lazy loading and change tracking - a
    // non-virtual property here would silently never get tracked, matching this repo's other mapped
    // entities (see TutorialProject/TodoTasks.Api/Domain/TodoTask.cs).

    /// <summary>Gets or sets the identifier NHibernate assigns on <c>Save()</c>.</summary>
    /// <value>A client-generated <see cref="Guid"/>, assigned as soon as the entity is saved into a session.</value>
    public virtual Guid Id { get; protected set; }
}
