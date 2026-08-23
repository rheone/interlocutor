namespace Interlocutor.Persistence.EntityFrameworkCore.Tests;

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
        await this.repository.InsertAsync(new Widget { Name = "inserted" }, TestContext.Current.CancellationToken);
        await new EntityFrameworkUnitOfWork<TestDbContext>(new Lazy<TestDbContext>(() => this.context)).SaveChangesAsync(
            TestContext.Current.CancellationToken
        );

        Assert.Equal(["inserted"], this.context.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task UpdateAsync_ThenSaveChanges_PersistsModification()
    {
        var widget = new Widget { Name = "original" };
        this.context.Widgets.Add(widget);
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);
        this.context.ChangeTracker.Clear();

        var tracked = this.context.Widgets.Single();
        tracked.Name = "modified";
        await this.repository.UpdateAsync(tracked, TestContext.Current.CancellationToken);
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verify = this.CreateVerificationContext();
        Assert.Equal("modified", verify.Widgets.Single().Name);
    }

    [Fact]
    public async Task DeleteAsync_ThenSaveChanges_RemovesEntity()
    {
        var widget = new Widget { Name = "doomed" };
        this.context.Widgets.Add(widget);
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await this.repository.DeleteAsync(widget, TestContext.Current.CancellationToken);
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verify = this.CreateVerificationContext();
        Assert.Empty(verify.Widgets);
    }

    [Fact]
    public async Task GetManyAsync_PushesFilterAndSortToSql_RatherThanMaterializingFirst()
    {
        this.context.Widgets.AddRange(
            new Widget { Name = "beta", Value = 2 },
            new Widget { Name = "alpha", Value = 1 },
            new Widget { Name = "gamma", Value = 3 }
        );
        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var query = this
            .repository.Query.ApplyFilter(w => w.Value > 1)
            .ApplySort([new SortCriterion<Widget>(w => w.Name, SortDirection.Ascending)]);

        // Asserts the generated SQL itself, not just the result, per the query-pushdown requirement:
        // filter and sort must be composed onto the query, not applied after an unfiltered ToListAsync.
        var sql = query.ToQueryString();
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);

        var result = await this.repository.GetManyAsync(
            w => w.Value > 1,
            [new SortCriterion<Widget>(w => w.Name, SortDirection.Ascending)],
            TestContext.Current.CancellationToken
        );

        Assert.Equal(["beta", "gamma"], result.Select(w => w.Name));
    }

    [Fact]
    public async Task GetPagedAsync_PushesSkipTakeToSql_AndReportsAccurateTotalCount()
    {
        for (var i = 1; i <= 5; i++)
        {
            this.context.Widgets.Add(new Widget { Name = $"widget-{i}", Value = i });
        }

        await this.context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pagedSql = this
            .repository.Query.ApplyFilter(null)
            .ApplySort([new SortCriterion<Widget>(w => w.Value, SortDirection.Ascending)])
            .ApplyPaging(pageNumber: 2, pageSize: 2)
            .ToQueryString();

        Assert.Contains("LIMIT", pagedSql, StringComparison.OrdinalIgnoreCase);

        var page = await this.repository.GetPagedAsync(
            pageNumber: 2,
            pageSize: 2,
            sort: [new SortCriterion<Widget>(w => w.Value, SortDirection.Ascending)],
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(["widget-3", "widget-4"], page.Items.Select(w => w.Name));
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPageCount);
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
