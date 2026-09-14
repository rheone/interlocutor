namespace Interlocutor.IntegrationTests;

using global::Autofac;
using global::FluentValidation;
using global::Microsoft.Data.Sqlite;
using global::Microsoft.EntityFrameworkCore;
using global::Microsoft.Extensions.Logging;
using global::Microsoft.Extensions.Logging.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Autofac;

/// <summary>
/// Runs <see cref="CrossPackagePipelineTracerBulletTests"/>'s tracer-bullet scenario a second time, resolved
/// from a real Autofac <see cref="IContainer"/> instead of Microsoft DI's <see cref="System.IServiceProvider"/>
/// - proving the canonical pipeline order (Logging → Auditing → Authorization → Validation → Transaction)
/// wired across real package boundaries is container-agnostic rather than incidentally dependent on
/// Microsoft.Extensions.DependencyInjection's own registration or resolution behavior. Every command, handler,
/// validator, entity, and <c>DbContext</c> type this class dispatches through is the exact same type
/// <see cref="CrossPackagePipelineTracerBulletTests"/> already uses - only the container wiring below differs,
/// isolated to this class's own fixture code.
/// </summary>
public sealed class AutofacCrossPackagePipelineTracerBulletTests : IDisposable
{
    private readonly SqliteConnection connection;

    /// <summary>Initializes a new instance of the <see cref="AutofacCrossPackagePipelineTracerBulletTests"/> class.</summary>
    public AutofacCrossPackagePipelineTracerBulletTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();
    }

    [Fact]
    public async Task CreateWidget_FlowsThroughFullCanonicalChain_AndCommitsARealEntityFrameworkCoreTransaction()
    {
        // Arrange
        await using var container = await this.BuildContainerAsync();
        await using var scope = container.BeginLifetimeScope();
        var mediator = scope.Resolve<IMediator>();

        // Act
        var result = await mediator.SendAsync(new CreateWidgetCommand("Widget A"), TestContext.Current.CancellationToken);

        // Assert: the real Outcome returned to the caller is Success - not a mock verifying a behavior ran.
        Assert.True(result.TryGetValue(out Success _));

        // Assert: a real row landed in a real EF Core-backed Sqlite database, committed by the real
        // TestDbContextUnitOfWorkBehavior further out in the pipeline - read through a second, independent
        // context sharing the same in-memory connection, so this reflects what actually reached the database
        // rather than the first-level cache of the context the handler wrote through.
        await using var verifyContext = this.CreateContext();
        Assert.Equal(["Widget A"], verifyContext.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task CreateWidget_WithInvalidName_ShortCircuitsWithValidationErrors_BeforeAnyTransactionCommits()
    {
        // Arrange
        await using var container = await this.BuildContainerAsync();
        await using var scope = container.BeginLifetimeScope();
        var mediator = scope.Resolve<IMediator>();

        // Act: an empty Name fails CreateWidgetCommandValidator's NotEmpty rule.
        var result = await mediator.SendAsync(new CreateWidgetCommand(string.Empty), TestContext.Current.CancellationToken);

        // Assert: ValidationBehavior short-circuits with the real ValidationErrors case - observed through the
        // real Outcome returned to the caller, not through a mock verifying a call was made. Validation runs
        // before the transaction stage in the canonical order, so the handler (and the commit gate around it)
        // never run at all.
        Assert.True(result.TryGetValue(out ValidationErrors validationErrors));
        Assert.Contains(validationErrors.Messages, message => message.Contains("Name", StringComparison.Ordinal));

        // Assert: no row was ever committed - the transaction never opened, let alone committed.
        await using var verifyContext = this.CreateContext();
        Assert.Empty(verifyContext.Widgets);
    }

    /// <inheritdoc/>
    public void Dispose() => this.connection.Dispose();

    /// <summary>
    /// Composes the real Autofac container both tests in this class dispatch through: the real Autofac
    /// <c>AddInterlocutor()</c>, the same compile-time generated registrations
    /// <see cref="CrossPackagePipelineTracerBulletTests"/> uses, the full canonical behavior order, and a real
    /// EF Core unit of work against this instance's Sqlite in-memory connection. Every container-specific
    /// difference from the Microsoft DI variant is contained here - registration calls and lifetime names -
    /// never in the handlers, behaviors, or validator under test.
    /// </summary>
    /// <returns>The built <see cref="IContainer"/>, with the database schema already created.</returns>
    private async Task<IContainer> BuildContainerAsync()
    {
        var containerBuilder = new ContainerBuilder();

        // LoggingBehavior and AuditingBehavior both resolve ILogger<TRequest>; Autofac has no built-in
        // Microsoft.Extensions.Logging bridge the way services.AddLogging() gives Microsoft DI, so a plain
        // NullLogger<> closes that dependency for every request type without needing a real sink.
        containerBuilder.RegisterGeneric(typeof(NullLogger<>)).As(typeof(ILogger<>));

        // Registered directly against ContainerBuilder rather than via Microsoft.Extensions.DependencyInjection's
        // AddDbContext - see PersistenceRegistrationTests for the identical pattern this project's own Autofac
        // package tests already establish for wiring a scoped DbContext under Autofac.
        containerBuilder
            .Register(_ => new DbContextOptionsBuilder<IntegrationTestDbContext>().UseSqlite(this.connection).Options)
            .SingleInstance();
        containerBuilder.RegisterType<IntegrationTestDbContext>().AsSelf().InstancePerLifetimeScope();
        containerBuilder
            .RegisterType<CreateWidgetCommandValidator>()
            .As<IValidator<CreateWidgetCommand>>()
            .InstancePerLifetimeScope();

        containerBuilder.AddInterlocutor(builder =>
            builder
                // No ScanAssembly call: the same compile-time generated module
                // CrossPackagePipelineTracerBulletTests dispatches through - this project's own handlers are
                // discovered once, at compile time, and reused by both container flavors.
                .AddGeneratedRegistrations(
                    Interlocutor.Generated.Interlocutor_IntegrationTests.InterlocutorGeneratedRegistrations.Registrations
                )
                .Pipeline.AddLoggingBehavior()
                .AddAuditingBehavior()
                .AddAuthorizationBehavior()
                .AddValidationBehavior()
                // The same TestDbContextUnitOfWorkBehavior<,> subclass CrossPackagePipelineTracerBulletTests
                // registers under Microsoft DI - kept identical here for parity even though Autofac's generic
                // registration has no arity restriction and could register UnitOfWorkBehavior<,,> directly via
                // AddUnitOfWorkBehavior() (see documentation/transactions-and-store-integration.md's
                // "Autofac/Microsoft DI" note). Using the same subclass proves the same pipeline scenario, not
                // a container-specific shortcut.
                .AddBehavior(typeof(TestDbContextUnitOfWorkBehavior<,>))
        );

        var container = containerBuilder.Build();
        await using (var schemaScope = container.BeginLifetimeScope())
        {
            await schemaScope
                .Resolve<IntegrationTestDbContext>()
                .Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        return container;
    }

    /// <summary>Builds another <see cref="IntegrationTestDbContext"/> sharing this instance's open connection.</summary>
    private IntegrationTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<IntegrationTestDbContext>().UseSqlite(this.connection).Options);
}
