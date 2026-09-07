namespace Interlocutor.IntegrationTests;

using global::FluentValidation;
using global::Microsoft.Data.Sqlite;
using global::Microsoft.EntityFrameworkCore;
using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Microsoft;

/// <summary>
/// Closes the gap every other <c>*IntegrationTests.cs</c> file in this solution leaves open: each of those
/// proves one package against itself, never a single dispatch flowing through the real canonical pipeline
/// order (Logging → Auditing → Authorization → Validation → Transaction) across real package boundaries with
/// no mocks standing in for another Interlocutor package. This project wires a real <see cref="IServiceCollection"/>,
/// registers <see cref="CreateWidgetCommandHandler"/> purely through <c>Interlocutor.Generator</c>'s compile-time
/// discovered registrations (no <c>ScanAssembly</c> call anywhere in this project), and commits through a real
/// EF Core unit of work against a Sqlite in-memory database.
/// </summary>
public sealed class CrossPackagePipelineTracerBulletTests : IDisposable
{
    private readonly SqliteConnection connection;

    /// <summary>Initializes a new instance of the <see cref="CrossPackagePipelineTracerBulletTests"/> class.</summary>
    public CrossPackagePipelineTracerBulletTests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();
    }

    [Fact]
    public async Task CreateWidget_FlowsThroughFullCanonicalChain_AndCommitsARealEntityFrameworkCoreTransaction()
    {
        // Arrange
        var provider = await this.BuildProviderAsync();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new CreateWidgetCommand("Widget A"), TestContext.Current.CancellationToken);

        // Assert: the real Outcome returned to the caller is Success - not a mock verifying a behavior ran.
        Assert.True(result.TryGetValue(out Success _));

        // Assert: a real row landed in a real EF Core-backed Sqlite database, committed by the real
        // TestDbContextUnitOfWorkBehavior further out in the pipeline - read through a second, independent
        // context sharing the same in-memory connection, so this reflects what actually reached the
        // database rather than the first-level cache of the context the handler wrote through.
        await using var verifyContext = this.CreateContext();
        Assert.Equal(["Widget A"], verifyContext.Widgets.Select(w => w.Name));
    }

    [Fact]
    public async Task CreateWidget_WithInvalidName_ShortCircuitsWithValidationErrors_BeforeAnyTransactionCommits()
    {
        // Arrange
        var provider = await this.BuildProviderAsync();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

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
    /// Composes the real container this project's tests dispatch through: the real <c>AddInterlocutor()</c>,
    /// the compile-time generated registrations for <see cref="CreateWidgetCommandHandler"/>, the full
    /// canonical behavior order, and a real EF Core unit of work against this instance's Sqlite in-memory
    /// connection.
    /// </summary>
    /// <returns>The built <see cref="ServiceProvider"/>, with the database schema already created.</returns>
    private async Task<ServiceProvider> BuildProviderAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<IntegrationTestDbContext>(
            options => options.UseSqlite(this.connection),
            contextLifetime: ServiceLifetime.Scoped
        );
        services.AddScoped(sp => new Lazy<IntegrationTestDbContext>(sp.GetRequiredService<IntegrationTestDbContext>));
        services.AddScoped<IValidator<CreateWidgetCommand>, CreateWidgetCommandValidator>();

        services.AddInterlocutor(builder =>
            builder
                // No ScanAssembly call: CreateWidgetCommandHandler is discovered and registered purely through
                // Interlocutor.Generator's compile-time generated module, referenced as an analyzer in this
                // project's own .csproj - proving the compile-time discovered path, not runtime reflection.
                .AddGeneratedRegistrations(
                    Interlocutor.Generated.Interlocutor_IntegrationTests.InterlocutorGeneratedRegistrations.Registrations
                )
                .Pipeline.AddLoggingBehavior()
                .AddAuditingBehavior()
                .AddAuthorizationBehavior()
                .AddValidationBehavior()
                // Not AddUnitOfWorkBehavior(): that throws at container-build time under Microsoft DI once EF
                // Core is referenced (arity mismatch - see TestDbContextUnitOfWorkBehavior's remarks). This is
                // the documented subclass workaround, registered through the general AddBehavior escape hatch.
                .AddBehavior(typeof(TestDbContextUnitOfWorkBehavior<,>))
        );

        var provider = services.BuildServiceProvider();
        await using (var schemaScope = provider.CreateAsyncScope())
        {
            await schemaScope
                .ServiceProvider.GetRequiredService<IntegrationTestDbContext>()
                .Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        return provider;
    }

    /// <summary>Builds another <see cref="IntegrationTestDbContext"/> sharing this instance's open connection.</summary>
    private IntegrationTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<IntegrationTestDbContext>().UseSqlite(this.connection).Options);
}
