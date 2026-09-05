namespace Interlocutor.Persistence.NHibernate.Tests;

using global::NHibernate;

/// <summary>Covers <see cref="NHibernateUnitOfWork.SaveChangesAsync"/>'s forwarding to <see cref="ISession.FlushAsync(CancellationToken)"/>.</summary>
public sealed class NHibernateUnitOfWorkTests : IClassFixture<RealSessionFixture>
{
    private readonly RealSessionFixture fixture;

    public NHibernateUnitOfWorkTests(RealSessionFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task SaveChangesAsync_FlushesPendingChanges_MakingThemVisibleToARealQuery()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        // Disables NHibernate's own default auto-flush-before-query, so the query below can only see
        // the pending insert if SaveChangesAsync's own FlushAsync call actually ran.
        session.FlushMode = FlushMode.Commit;
        var unitOfWork = new NHibernateUnitOfWork(new Lazy<ISession>(() => session));
        var widget = new Widget();
        await session.SaveAsync(widget, TestContext.Current.CancellationToken);

        // Act
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(session.Query<Widget>());
    }

    [Fact]
    public async Task SaveChangesAsync_ForwardsTheGivenCancellationToken_ToSessionFlushAsync()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var unitOfWork = new NHibernateUnitOfWork(new Lazy<ISession>(() => session));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert - FlushAsync checks its own token before doing anything else, so this only
        // throws if SaveChangesAsync actually forwarded the cancelled token rather than substituting
        // CancellationToken.None.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => unitOfWork.SaveChangesAsync(cts.Token));
    }

    [Fact]
    public void Constructor_DoesNotResolveSession_UntilSaveChangesAsyncIsCalled()
    {
        // Arrange & Act
        // The Lazy<ISession> factory throwing on access proves the session is never constructed just by
        // building the unit of work - matching UnitOfWorkBehavior's own deferred-construction idiom.
        var exception = Record.Exception(() =>
            new NHibernateUnitOfWork(new Lazy<ISession>(() => throw new InvalidOperationException("Should not be resolved")))
        );

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task CancelAsync_RollsBackActiveTransaction_AndClearsSession()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var unitOfWork = new NHibernateUnitOfWork(new Lazy<ISession>(() => session));
        var widget = new Widget();
        await session.SaveAsync(widget, TestContext.Current.CancellationToken);
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await unitOfWork.CancelAsync(TestContext.Current.CancellationToken);

        // Assert - the transaction was rolled back and the session cleared, so nothing is pending or persisted.
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });
        Assert.False(session.Contains(widget));
    }
}
