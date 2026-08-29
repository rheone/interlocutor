namespace TodoTasks.Api.Tests.Persistence;

using NHibernate;

/// <summary>
/// Small test-only helpers shared by this project's persistence tests, mirroring the
/// <see cref="ISession"/>-shaped conventions the production code itself follows (see
/// <c>Interlocutor.Persistence.NHibernate.UnitOfWorkBehavior{TRequest,TResponse}</c>'s own use of
/// <see cref="SessionExtensions.GetCurrentTransaction(ISession)"/>).
/// </summary>
internal static class SessionTestExtensions
{
    /// <summary>
    /// Commits <paramref name="session"/>'s current transaction if one is active - a real dispatch's
    /// commit-gate behavior does this at pipeline completion; a test calling a handler directly (bypassing
    /// the pipeline) does it here in the behavior's place, so nothing is left for the session's own
    /// <c>Dispose()</c> to roll back afterward.
    /// </summary>
    /// <param name="session">The session whose active transaction, if any, should be committed.</param>
    public static void CommitIfActive(this ISession session)
    {
        if (session.GetCurrentTransaction() is { IsActive: true } transaction)
        {
            transaction.Commit();
        }
    }
}
