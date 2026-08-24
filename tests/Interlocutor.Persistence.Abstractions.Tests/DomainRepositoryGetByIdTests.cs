namespace Interlocutor.Persistence.Abstractions.Tests;

using System.Linq.Expressions;

/// <summary>
/// Covers <see cref="IDomainRepository{TEntity, TKey}.GetById"/>, the default interface method built on
/// the <see cref="IDomainRepository{TEntity, TKey}.Query"/> escape hatch, against a fake in-memory
/// <see cref="IQueryable{T}"/> implementation - no ORM is involved.
/// </summary>
public sealed class DomainRepositoryGetByIdTests
{
    [Fact]
    public void GetById_ReturnsMatchingEntity_WhenKeyIsPresent()
    {
        // Arrange
        // GetById is a default interface method - it is only reachable through the interface, not the
        // concrete implementing type, so the fake is deliberately held through its interface here.
        IDomainRepository<Widget, int> repository = new FakeRepository([new Widget(1, "one"), new Widget(2, "two")]);

        // Act
        var result = repository.GetById(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("two", result.Name);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenKeyIsAbsent()
    {
        // Arrange
        IDomainRepository<Widget, int> repository = new FakeRepository([new Widget(1, "one")]);

        // Act
        var result = repository.GetById(99);

        // Assert
        Assert.Null(result);
    }

    // Auto Generated, verify expected behavior:
    // Boundary case distinct from "key absent among several entities": the underlying Query is empty,
    // exercising FirstOrDefault against a source with no elements at all.
    [Fact]
    public void GetById_ReturnsNull_WhenRepositoryIsEmpty()
    {
        // Arrange
        IDomainRepository<Widget, int> repository = new FakeRepository([]);

        // Act
        var result = repository.GetById(1);

        // Assert
        Assert.Null(result);
    }

    private sealed record Widget(int Id, string Name);

    /// <summary>
    /// The minimum implementation needed to exercise the default <c>GetById</c> method: <see cref="Query"/>
    /// backed by an in-memory list, and <see cref="IdSelector"/> pointing at <see cref="Widget.Id"/>. Every
    /// other member is unused by these tests and throws if called.
    /// </summary>
    private sealed class FakeRepository(IReadOnlyList<Widget> widgets) : IDomainRepository<Widget, int>
    {
        public IQueryable<Widget> Query { get; } = widgets.AsQueryable();

        public Expression<Func<Widget, int>> IdSelector => w => w.Id;

        public Task InsertAsync(Widget entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Widget entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(Widget entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Widget>> GetManyAsync(
            Expression<Func<Widget, bool>>? filter = null,
            IReadOnlyList<SortCriterion<Widget>>? sort = null,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<Page<Widget>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<Widget, bool>>? filter = null,
            IReadOnlyList<SortCriterion<Widget>>? sort = null,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }
}
