namespace Interlocutor.DependencyInjection.Microsoft.EntityFrameworkCore.Tests;

using global::Microsoft.Data.Sqlite;
using global::Microsoft.EntityFrameworkCore;
using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.EntityFrameworkCore;

/// <summary>
/// Covers the audit-followups ticket 14 resolution: <c>InterlocutorPipelineBuilder.AddTransactionBehavior()</c>
/// registers EF Core's arity-3 <see cref="TransactionBehavior{TRequest,TResponse,TContext}"/> against the
/// arity-2 <see cref="IPipelineBehavior{TRequest,TResponse}"/> unchanged, which
/// Microsoft.Extensions.DependencyInjection cannot register at all. Since Microsoft DI has no way to leave a
/// third type parameter open behind a two-parameter service interface, the fix is a clear, immediate failure
/// plus a documented workaround (a small subclass closing <c>TContext</c>, registered via
/// <c>InterlocutorPipelineBuilder.AddBehavior</c>) rather than a structural change to the registration
/// translator - see <c>AddTransactionBehavior</c>'s remarks and <c>ServiceCollectionExtensions.ResolveServiceType</c>'s
/// exception documentation.
/// </summary>
public sealed class TransactionBehaviorArityTests
{
    [Fact]
    public void AddTransactionBehavior_WithEfCoreReferenced_ThrowsImmediately_WithActionableGuidance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        // Fails synchronously during AddInterlocutor(), not lazily the first time BuildServiceProvider()
        // runs - the whole point of resolving the arity mismatch eagerly instead of leaving Microsoft DI's
        // own late, cryptic ArgumentException to surface it.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddInterlocutor(builder => builder.Pipeline.AddTransactionBehavior())
        );

        Assert.Contains("TransactionBehavior", ex.Message, StringComparison.Ordinal);
        Assert.Contains("AddBehavior", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DocumentedSubclassWorkaround_ResolvesAndDispatches_ThroughRealServiceProvider()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection), contextLifetime: ServiceLifetime.Scoped);
        services.AddScoped(sp => new Lazy<TestDbContext>(sp.GetRequiredService<TestDbContext>));

        services.AddInterlocutor(builder =>
            builder
                .AddHandler(typeof(WidgetCommandHandler))
                // The documented workaround from AddTransactionBehavior()'s remarks: a subclass closing
                // TContext over the application's own DbContext, registered via the general AddBehavior
                // escape hatch instead of AddTransactionBehavior() itself.
                .Pipeline.AddBehavior(typeof(TestDbContextTransactionBehavior<,>))
        );

        await using var provider = services.BuildServiceProvider();
        await using (var schemaScope = provider.CreateAsyncScope())
        {
            await schemaScope
                .ServiceProvider.GetRequiredService<TestDbContext>()
                .Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new CreateWidget("subclassed"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        await using var verifyContext = new TestDbContext(
            new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options
        );
        Assert.Equal(["subclassed"], verifyContext.Widgets.Select(w => w.Name));
    }

    private sealed record CreateWidget(string Name) : ICommand<OperationOutcome<Success, Failure>>, ITransactionalRequest;

    private sealed class WidgetCommandHandler(TestDbContext context)
        : ICommandHandler<CreateWidget, OperationOutcome<Success, Failure>>
    {
        public Task<OperationOutcome<Success, Failure>> Handle(CreateWidget command, CancellationToken ct)
        {
            context.Widgets.Add(new Widget { Name = command.Name });
            return Task.FromResult<OperationOutcome<Success, Failure>>(default(Success));
        }
    }

    /// <summary>
    /// The exact pattern documented on <c>AddTransactionBehavior</c>'s remarks and demonstrated by
    /// <c>samples/Interlocutor.Sample.WebApi/SampleTransactionBehavior.cs</c>: closes
    /// <see cref="TransactionBehavior{TRequest,TResponse,TContext}"/>'s <c>TContext</c> over
    /// <see cref="TestDbContext"/>, leaving <c>TRequest</c>/<c>TResponse</c> open so it still registers as a
    /// two-parameter open generic against <see cref="IPipelineBehavior{TRequest,TResponse}"/>.
    /// </summary>
    private sealed class TestDbContextTransactionBehavior<TRequest, TResponse>(Lazy<TestDbContext> db)
        : TransactionBehavior<TRequest, TResponse, TestDbContext>(db)
        where TRequest : ITransactionalRequest;

    private sealed class Widget
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => this.Set<Widget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Widget>();
    }
}
