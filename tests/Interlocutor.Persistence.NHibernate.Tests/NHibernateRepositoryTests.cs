namespace Interlocutor.Persistence.NHibernate.Tests;

using System.Linq.Expressions;
using global::NHibernate;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;

/// <summary>
/// Covers <see cref="NHibernateRepository{TEntity, TKey}"/>'s CRUD forwarding to <see cref="ISession"/>.
/// </summary>
/// <remarks>
/// <see cref="NHibernateRepository{TEntity, TKey}.Query"/> is backed by NHibernate's own
/// <c>ISession.Query&lt;TEntity&gt;()</c> LINQ provider extension method, which resolves session internals
/// no substitute can stand in for - the same reason <c>Interlocutor.NHibernate.Tests.TransactionBehaviorTests</c>
/// tests transaction commit/rollback against a substituted <see cref="ISession"/> without exercising real
/// query execution. Filter/sort/paging composition itself is covered generically, against a plain in-memory
/// <see cref="IQueryable{T}"/>, by <c>Interlocutor.Persistence.Abstractions.Tests.DomainRepositoryQueryableExtensionsTests</c>
/// and proven to push down to real SQL for one ORM by <c>Interlocutor.Persistence.EntityFrameworkCore.Tests</c>;
/// both implementations share the identical <see cref="DomainRepositoryQueryableExtensions"/> composition.
/// </remarks>
public sealed class NHibernateRepositoryTests
{
    [Fact]
    public async Task InsertAsync_ForwardsToSessionSaveAsync_WithSameCancellationToken()
    {
        // Arrange
        var (repository, session) = CreateRepository();
        var widget = new Widget();
        using var cts = new CancellationTokenSource();

        // Act
        await repository.InsertAsync(widget, cts.Token);

        // Assert
        await session.Received(1).SaveAsync(widget, cts.Token);
    }

    [Fact]
    public async Task UpdateAsync_ForwardsToSessionUpdateAsync_WithSameCancellationToken()
    {
        // Arrange
        var (repository, session) = CreateRepository();
        var widget = new Widget();
        using var cts = new CancellationTokenSource();

        // Act
        await repository.UpdateAsync(widget, cts.Token);

        // Assert
        await session.Received(1).UpdateAsync(widget, cts.Token);
    }

    [Fact]
    public async Task DeleteAsync_ForwardsToSessionDeleteAsync_WithSameCancellationToken()
    {
        // Arrange
        var (repository, session) = CreateRepository();
        var widget = new Widget();
        using var cts = new CancellationTokenSource();

        // Act
        await repository.DeleteAsync(widget, cts.Token);

        // Assert
        await session.Received(1).DeleteAsync(widget, cts.Token);
    }

    [Fact]
    public void IdSelector_ReturnsExpressionSuppliedAtConstruction()
    {
        // Arrange
        Expression<Func<Widget, int>> idSelector = w => w.Id;

        // Act
        var repository = new NHibernateRepository<Widget, int>(
            new Lazy<ISession>(() => Substitute.For<ISession>()),
            idSelector
        );

        // Assert
        Assert.Same(idSelector, repository.IdSelector);
    }

    private static (NHibernateRepository<Widget, int> Repository, ISession Session) CreateRepository()
    {
        var session = Substitute.For<ISession>();
        var repository = new NHibernateRepository<Widget, int>(new Lazy<ISession>(() => session), w => w.Id);
        return (repository, session);
    }

    public sealed class Widget
    {
        public int Id { get; set; }
    }
}
