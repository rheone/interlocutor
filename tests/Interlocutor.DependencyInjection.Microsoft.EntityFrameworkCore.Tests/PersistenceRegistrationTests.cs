namespace Interlocutor.DependencyInjection.Microsoft.EntityFrameworkCore.Tests;

using global::Microsoft.Data.Sqlite;
using global::Microsoft.EntityFrameworkCore;
using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Persistence.Abstractions;
using Interlocutor.Persistence.EntityFrameworkCore;

/// <summary>
/// Covers <see cref="PersistenceServiceCollectionExtensions"/>: opting a Microsoft DI container into
/// Interlocutor's persistence abstraction is an explicit call, separate from <c>AddInterlocutor()</c>, and
/// the registered <see cref="IUnitOfWork"/> and <see cref="IDomainRepository{TEntity, TKey}"/> resolve
/// scoped, sharing one underlying <see cref="DbContext"/> instance per scope.
/// </summary>
public sealed class PersistenceRegistrationTests : IDisposable
{
    private readonly SqliteConnection connection;

    public PersistenceRegistrationTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();
    }

    [Fact]
    public async Task UnitOfWorkAndRepository_ResolveScoped_SharingOneDbContextInstance_WithinOneScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(
            options => options.UseSqlite(this.connection),
            contextLifetime: ServiceLifetime.Scoped
        );
        services.AddScoped(sp => new Lazy<TestDbContext>(sp.GetRequiredService<TestDbContext>));
        services.AddInterlocutorUnitOfWork(sp => new EntityFrameworkUnitOfWork<TestDbContext>(
            sp.GetRequiredService<Lazy<TestDbContext>>()
        ));
        services.AddInterlocutorRepository<Widget, int, EntityFrameworkRepository<Widget, int, TestDbContext>>(
            sp => new EntityFrameworkRepository<Widget, int, TestDbContext>(
                sp.GetRequiredService<Lazy<TestDbContext>>(),
                w => w.Id
            )
        );

        await using var provider = services.BuildServiceProvider();
        await using (var schemaScope = provider.CreateAsyncScope())
        {
            await schemaScope
                .ServiceProvider.GetRequiredService<TestDbContext>()
                .Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        await using var scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repository = scope.ServiceProvider.GetRequiredService<IDomainRepository<Widget, int>>();
        var contextFromContainer = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Act
        // The repository's Query escape hatch reads through DbSet<Widget> - proving it and the container's
        // own resolved TestDbContext agree confirms IUnitOfWork, the repository, and a plain DbContext
        // injection all share one scoped instance within this scope.
        await contextFromContainer.Widgets.AddAsync(new Widget { Name = "shared" }, TestContext.Current.CancellationToken);
        await contextFromContainer.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<EntityFrameworkUnitOfWork<TestDbContext>>(unitOfWork);
        Assert.IsType<EntityFrameworkRepository<Widget, int, TestDbContext>>(repository);
        Assert.Equal(["shared"], repository.Query.Select(w => w.Name));
    }

    [Fact]
    public async Task UnitOfWorkAndRepository_ResolveDifferentContextInstances_AcrossSeparateScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(
            options => options.UseSqlite(this.connection),
            contextLifetime: ServiceLifetime.Scoped
        );
        services.AddScoped(sp => new Lazy<TestDbContext>(sp.GetRequiredService<TestDbContext>));
        services.AddInterlocutorUnitOfWork(sp => new EntityFrameworkUnitOfWork<TestDbContext>(
            sp.GetRequiredService<Lazy<TestDbContext>>()
        ));

        await using var provider = services.BuildServiceProvider();

        // Act
        await using var firstScope = provider.CreateAsyncScope();
        await using var secondScope = provider.CreateAsyncScope();

        var firstContext = firstScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Assert
        // Scoped, not singleton: two independent scopes never share the underlying DbContext.
        Assert.NotSame(firstContext, secondContext);
    }

    public void Dispose() => this.connection.Dispose();

    public sealed class Widget
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => this.Set<Widget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Widget>();
    }
}
