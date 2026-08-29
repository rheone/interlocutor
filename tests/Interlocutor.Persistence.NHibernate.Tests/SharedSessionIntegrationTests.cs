namespace Interlocutor.Persistence.NHibernate.Tests;

using global::NHibernate;
using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves that <see cref="NHibernateUnitOfWork"/> and <see cref="NHibernateRepository{TEntity, TKey}"/>,
/// resolved from the same <c>Lazy&lt;ISession&gt;</c>, participate in the same <see cref="ISession"/> instance
/// that <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/> sees for the same dispatch - the "same
/// transaction and session" guarantee this package's <see cref="Abstractions.IUnitOfWork"/> façade relies on
/// rather than re-implementing. Runs against a real in-memory NHibernate session (see
/// <see cref="RealSessionFixture"/>), so the claim is proven by an actually-persisted row, not a mocked
/// call-count assertion.
/// </summary>
public sealed class SharedSessionIntegrationTests : IClassFixture<RealSessionFixture>
{
    private readonly RealSessionFixture fixture;

    public SharedSessionIntegrationTests(RealSessionFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task RepositoryInsert_AndUnitOfWorkFlush_ResolveSameSessionInstance_AsUnitOfWorkBehavior()
    {
        // Arrange
        using var session = this.fixture.OpenSession();
        var lazySession = new Lazy<ISession>(() => session);

        var repository = new NHibernateRepository<Widget, Guid>(lazySession, w => w.Id);
        var unitOfWork = new NHibernateUnitOfWork(lazySession);
        var behavior = new UnitOfWorkBehavior<TransactionalRequest, OperationOutcome<Success, Failure>>(lazySession);

        var widget = new Widget();

        // Act
        var result = await behavior.Handle(
            new TransactionalRequest(),
            async () =>
            {
                // Both calls resolve ISession.Value from the exact same Lazy<ISession> instance
                // UnitOfWorkBehavior itself resolves - proving all three share one scoped session.
                await repository.InsertAsync(widget, CancellationToken.None);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
                return default(Success);
            },
            CancellationToken.None
        );

        // Assert - the outcome is success, the transaction committed for real, and a brand new session
        // sees the row that the shared session/repository/unit-of-work trio actually persisted.
        Assert.True(result.IsSuccess);
        Assert.False(session.GetCurrentTransaction() is { IsActive: true });

        using var verifySession = this.fixture.OpenSession();
        Assert.NotNull(await verifySession.GetAsync<Widget>(widget.Id, CancellationToken.None));
    }

    private sealed record TransactionalRequest : ICommand<OperationOutcome<Success, Failure>>;
}
