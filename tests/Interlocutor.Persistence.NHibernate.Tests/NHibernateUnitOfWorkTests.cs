namespace Interlocutor.Persistence.NHibernate.Tests;

using global::NHibernate;
using NSubstitute;

/// <summary>Covers <see cref="NHibernateUnitOfWork.SaveChangesAsync"/>'s forwarding to <see cref="ISession.FlushAsync"/>.</summary>
public sealed class NHibernateUnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_ForwardsToSessionFlushAsync_WithSameCancellationToken()
    {
        var session = Substitute.For<ISession>();
        var unitOfWork = new NHibernateUnitOfWork(new Lazy<ISession>(() => session));
        using var cts = new CancellationTokenSource();

        await unitOfWork.SaveChangesAsync(cts.Token);

        await session.Received(1).FlushAsync(cts.Token);
    }

    [Fact]
    public void Constructor_DoesNotResolveSession_UntilSaveChangesAsyncIsCalled()
    {
        // The Lazy<ISession> factory throwing on access proves the session is never constructed just by
        // building the unit of work - matching TransactionBehavior's own deferred-construction idiom.
        _ = new NHibernateUnitOfWork(new Lazy<ISession>(() => throw new InvalidOperationException("Should not be resolved")));
    }
}
