namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

using System.Linq.Expressions;
using Interlocutor.Persistence.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Covers CRUD, get-many, and get-paged on <see cref="EntityFrameworkRepository{TEntity,TKey,TContext}"/>
/// against a real Sqlite in-memory database, so filter/sort/paging pushdown to SQL can be asserted, not just
/// the materialized result.
/// </summary>
public sealed class EntityFrameworkRepositoryTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly TestDbContext context;
    private readonly EntityFrameworkRepository<Widget, int, TestDbContext> repository;
    private static readonly string[] Expected = ["alpha", "beta"];

    public EntityFrameworkRepositoryTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options;
        this.context = new TestDbContext(options);
        this.context.Database.EnsureCreated();

        this.repository = new EntityFrameworkRepository<Widget, int, TestDbContext>(
            new Lazy<TestDbContext>(() => this.context),
            w => w.Id
        );
    }

    [Fact]
    public async Task InsertAsync_ThenSaveChanges_PersistsEntity()
    {
        // Arrange & Act
        await this.repository.InsertAsync(new Widget { Name = "inserted" }, TestContext.Current.CancellationToken);
        await new EntityFrameworkUnitOfWork<TestDbContext>(new Lazy<TestDbContext>(() => this.context)).SaveChangesAsync(
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(["inserted"], this.context.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task UpdateAsync_ThenSaveChanges_PersistsModification()
    {
        // Arrange
        var widget = new Widget { Name = "original" };
        await this.context.Widgets.AddAsync(widget, TestContext.Current.CancellationToken);
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);
        this.context.ChangeTracker.Clear();

        var tracked = await this.context.Widgets.SingleAsync(TestContext.Current.CancellationToken);
        tracked.Name = "modified";

        // Act
        await this.repository.UpdateAsync(tracked, TestContext.Current.CancellationToken);
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verify = this.CreateVerificationContext();
        Assert.Equal("modified", (await verify.Widgets.SingleAsync(TestContext.Current.CancellationToken)).Name);
    }

    [Fact]
    public async Task DeleteAsync_ThenSaveChanges_RemovesEntity()
    {
        // Arrange
        var widget = new Widget { Name = "doomed" };
        await this.context.Widgets.AddAsync(widget, TestContext.Current.CancellationToken);
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await this.repository.DeleteAsync(widget, TestContext.Current.CancellationToken);
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
    }

    [Fact]
    public async Task GetManyAsync_PushesFilterAndSortToSql_RatherThanMaterializingFirst()
    {
        // Arrange
        await this.context.Widgets.AddRangeAsync(
            new Widget { Name = "beta", Value = 2 },
            new Widget { Name = "alpha", Value = 1 },
            new Widget { Name = "gamma", Value = 3 }
        );
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var query = this
            .repository.Query.ApplyFilter(w => w.Value > 1)
            .ApplySort([new SortCriterion<Widget>(w => w.Name, SortDirection.Ascending)]);

        // Act
        // Asserts the generated SQL itself, not just the result, per the query-pushdown requirement:
        // filter and sort must be composed onto the query, not applied after an unfiltered ToListAsync.
        var sql = query.ToQueryString();
        var result = await this.repository.GetManyAsync(
            w => w.Value > 1,
            [new SortCriterion<Widget>(w => w.Name, SortDirection.Ascending)],
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(["beta", "gamma"], result.Select(w => w.Name));
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task GetManyAsync_WithNoFilterOrSort_ReturnsEveryEntity()
    {
        // Arrange
        await this.context.Widgets.AddRangeAsync(new Widget { Name = "beta" }, new Widget { Name = "alpha" });
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await this.repository.GetManyAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        // No sort was supplied, so ordering is unspecified - assert set membership, not sequence order.
        Assert.Equal(2, result.Count);
        Assert.Equal(Expected, result.Select(w => w.Name).Order());
    }

    [Fact]
    public async Task GetPagedAsync_PushesSkipTakeToSql_AndReportsAccurateTotalCount()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            await this.context.Widgets.AddAsync(
                new Widget { Name = $"widget-{i}", Value = i },
                TestContext.Current.CancellationToken
            );
        }

        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pagedSql = this
            .repository.Query.ApplyFilter(null)
            .ApplySort([new SortCriterion<Widget>(w => w.Value, SortDirection.Ascending)])
            .ApplyPaging(pageNumber: 2, pageSize: 2)
            .ToQueryString();

        // Act
        var page = await this.repository.GetPagedAsync(
            pageNumber: 2,
            pageSize: 2,
            sort: [new SortCriterion<Widget>(w => w.Value, SortDirection.Ascending)],
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Contains("LIMIT", pagedSql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(["widget-3", "widget-4"], page.Items.Select(w => w.Name));
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPageCount);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task GetPagedAsync_WithFilter_AppliesFilterToBothCountAndItemsQueries()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            await this.context.Widgets.AddAsync(
                new Widget { Name = $"widget-{i}", Value = i },
                TestContext.Current.CancellationToken
            );
        }

        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var page = await this.repository.GetPagedAsync(
            pageNumber: 1,
            pageSize: 10,
            filter: w => w.Value > 2,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(["widget-3", "widget-4", "widget-5"], page.Items.Select(w => w.Name).Order());
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void IdSelector_ReturnsExpressionSuppliedAtConstruction()
    {
        // Arrange
        Expression<Func<Widget, int>> selector = w => w.Id;
        var repositoryWithExplicitSelector = new EntityFrameworkRepository<Widget, int, TestDbContext>(
            new Lazy<TestDbContext>(() => this.context),
            selector
        );

        // Act
        var idSelector = repositoryWithExplicitSelector.IdSelector;

        // Assert
        Assert.Same(selector, idSelector);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task GetById_ReturnsMatchingEntity_WhenQueryTranslatesThroughEfCore()
    {
        // Arrange
        await this.context.Widgets.AddRangeAsync(new Widget { Name = "first" }, new Widget { Name = "second" });
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var expectedId = (
            await this.context.Widgets.SingleAsync(w => w.Name == "second", TestContext.Current.CancellationToken)
        ).Id;

        // Act
        // GetById is a default interface method - reachable only through the interface, not the concrete type.
        IDomainRepository<Widget, int> domainRepository = this.repository;
        var result = domainRepository.GetById(expectedId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("second", result.Name);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void GetById_ReturnsNull_WhenNoEntityHasThatKey()
    {
        // Arrange & Act
        // GetById is a default interface method - reachable only through the interface, not the concrete type.
        IDomainRepository<Widget, int> domainRepository = this.repository;
        var result = domainRepository.GetById(id: 999);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        this.context.Dispose();
        this.connection.Dispose();
    }

    private TestDbContext CreateVerificationContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options;
        return new TestDbContext(options);
    }

    public sealed class Widget
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => this.Set<Widget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Widget>();
    }
}
