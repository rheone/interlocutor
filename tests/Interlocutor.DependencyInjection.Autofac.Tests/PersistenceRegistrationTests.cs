namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using global::Microsoft.Data.Sqlite;
using global::Microsoft.EntityFrameworkCore;
using Interlocutor.Persistence.Abstractions;
using Interlocutor.Persistence.EntityFrameworkCore;

/// <summary>
/// Covers <see cref="PersistenceContainerBuilderExtensions"/>: opting an Autofac container into
/// Interlocutor's persistence abstraction is an explicit call, separate from <c>AddInterlocutor()</c>, and
/// the registered <see cref="IUnitOfWork"/> and <see cref="IDomainRepository{TEntity, TKey}"/> resolve
/// scoped (<c>InstancePerLifetimeScope</c>), sharing one underlying <see cref="DbContext"/> instance per scope.
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
        var builder = new ContainerBuilder();
        builder.Register(_ => new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options).SingleInstance();
        builder.RegisterType<TestDbContext>().AsSelf().InstancePerLifetimeScope();

        // Autofac resolves Lazy<T> natively from a registered T, so no separate Lazy<TestDbContext>
        // registration is needed - unlike wrapping it by hand in a factory lambda, which would capture the
        // short-lived IComponentContext past the end of its own resolve operation.
        builder.AddInterlocutorUnitOfWork(c => new EntityFrameworkUnitOfWork<TestDbContext>(c.Resolve<Lazy<TestDbContext>>()));
        builder.AddInterlocutorRepository<Widget, int, EntityFrameworkRepository<Widget, int, TestDbContext>>(
            c => new EntityFrameworkRepository<Widget, int, TestDbContext>(c.Resolve<Lazy<TestDbContext>>(), w => w.Id)
        );

        await using var container = builder.Build();
        await using (var schemaScope = container.BeginLifetimeScope())
        {
            await schemaScope.Resolve<TestDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        await using var scope = container.BeginLifetimeScope();

        // Act
        var unitOfWork = scope.Resolve<IUnitOfWork>();
        var repository = scope.Resolve<IDomainRepository<Widget, int>>();
        var contextFromContainer = scope.Resolve<TestDbContext>();

        // The repository's Query escape hatch reads through DbSet<Widget> - proving it and the container's
        // own resolved TestDbContext agree confirms IUnitOfWork, the repository, and a plain DbContext
        // resolution all share one InstancePerLifetimeScope instance within this scope.
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
        var builder = new ContainerBuilder();
        builder.Register(_ => new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options).SingleInstance();
        builder.RegisterType<TestDbContext>().AsSelf().InstancePerLifetimeScope();

        // Autofac resolves Lazy<T> natively from a registered T, so no separate Lazy<TestDbContext>
        // registration is needed - unlike wrapping it by hand in a factory lambda, which would capture the
        // short-lived IComponentContext past the end of its own resolve operation.
        builder.AddInterlocutorUnitOfWork(c => new EntityFrameworkUnitOfWork<TestDbContext>(c.Resolve<Lazy<TestDbContext>>()));

        await using var container = builder.Build();

        await using var firstScope = container.BeginLifetimeScope();
        await using var secondScope = container.BeginLifetimeScope();

        // Act
        var firstContext = firstScope.Resolve<TestDbContext>();
        var secondContext = secondScope.Resolve<TestDbContext>();

        // Assert
        // InstancePerLifetimeScope, not SingleInstance: two independent scopes never share the underlying
        // DbContext.
        Assert.NotSame(firstContext, secondContext);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void AddInterlocutorUnitOfWork_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ContainerBuilder();

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.AddInterlocutorUnitOfWork<EntityFrameworkUnitOfWork<TestDbContext>>(null!)
        );

        // Assert
        Assert.Equal("implementationFactory", exception.ParamName);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public void AddInterlocutorRepository_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ContainerBuilder();

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.AddInterlocutorRepository<Widget, int, EntityFrameworkRepository<Widget, int, TestDbContext>>(null!)
        );

        // Assert
        Assert.Equal("implementationFactory", exception.ParamName);
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
