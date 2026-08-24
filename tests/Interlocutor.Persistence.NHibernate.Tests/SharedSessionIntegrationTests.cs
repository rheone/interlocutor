namespace Interlocutor.Persistence.NHibernate.Tests;

using global::NHibernate;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Markers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

/// <summary>
/// Proves that <see cref="NHibernateUnitOfWork"/> and <see cref="NHibernateRepository{TEntity, TKey}"/>,
/// resolved from the same <c>Lazy&lt;ISession&gt;</c>, participate in the same <see cref="ISession"/> instance
/// that <c>Interlocutor.NHibernate.TransactionBehavior{TRequest,TResponse}</c> sees for the same dispatch -
/// the "same transaction and session" guarantee this package's <see cref="Abstractions.IUnitOfWork"/> façade
/// relies on rather than re-implementing.
/// </summary>
public sealed class SharedSessionIntegrationTests
{
    [Fact]
    public async Task RepositoryInsert_AndUnitOfWorkFlush_ResolveSameSessionInstance_AsTransactionBehavior()
    {
        // Arrange
        var session = Substitute.For<ISession>();
        var transaction = Substitute.For<ITransaction>();
        session.BeginTransaction().Returns(transaction);
        var lazySession = new Lazy<ISession>(() => session);

        var repository = new NHibernateRepository<Widget, int>(lazySession, w => w.Id);
        var unitOfWork = new NHibernateUnitOfWork(lazySession);
        var behavior = new global::Interlocutor.NHibernate.TransactionBehavior<
            TransactionalRequest,
            OperationOutcome<Success, Failure>
        >(lazySession);

        var widget = new Widget();

        // Act
        var result = await behavior.Handle(
            new TransactionalRequest(),
            async () =>
            {
                // Both calls resolve ISession.Value from the exact same Lazy<ISession> instance
                // TransactionBehavior itself resolves - proving all three share one scoped session.
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

    private sealed record TransactionalRequest : ICommand<OperationOutcome<Success, Failure>>, ITransactionalRequest;

    public sealed class Widget
    {
        public int Id { get; set; }
    }
}
