namespace Interlocutor.Persistence.NHibernate.Tests;

using global::NHibernate;
using Interlocutor.Abstractions.Commands;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

/// <summary>
/// Proves that <see cref="NHibernateUnitOfWork"/> and <see cref="NHibernateRepository{TEntity, TKey}"/>,
/// resolved from the same <c>Lazy&lt;ISession&gt;</c>, participate in the same <see cref="ISession"/> instance
/// that <see cref="UnitOfWorkBehavior{TRequest,TResponse}"/> sees for the same dispatch - the "same
/// transaction and session" guarantee this package's <see cref="Abstractions.IUnitOfWork"/> façade relies on
/// rather than re-implementing.
/// </summary>
public sealed class SharedSessionIntegrationTests
{
    [Fact]
    public async Task RepositoryInsert_AndUnitOfWorkFlush_ResolveSameSessionInstance_AsUnitOfWorkBehavior()
    {
        // Arrange
        var session = Substitute.For<ISession>();
        var transaction = Substitute.For<ITransaction>();
        transaction.IsActive.Returns(true);

        // Transaction only reports the active transaction once BeginTransaction() has actually run - matching
        // real NHibernate, where BeginTransaction() sets this property as a side effect.
        var transactionBegun = false;
        session
            .BeginTransaction()
            .Returns(_ =>
            {
                transactionBegun = true;
                return transaction;
            });
        session.Transaction.Returns(_ => transactionBegun ? transaction : null);
        var lazySession = new Lazy<ISession>(() => session);

        var repository = new NHibernateRepository<Widget, int>(lazySession, w => w.Id);
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

        // Assert
        Assert.True(result.IsSuccess);
        await session.Received(1).SaveAsync(widget, CancellationToken.None);
        await session.Received(1).FlushAsync(CancellationToken.None);
        session.Received(1).BeginTransaction();
        await transaction.Received(1).CommitAsync(CancellationToken.None);
    }

    private sealed record TransactionalRequest : ICommand<OperationOutcome<Success, Failure>>;

    public sealed class Widget
    {
        public int Id { get; set; }
    }
}
