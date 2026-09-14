namespace Interlocutor.IntegrationTests;

using global::FluentValidation;
using global::Microsoft.Data.Sqlite;
using global::Microsoft.EntityFrameworkCore;
using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.DependencyInjection.Microsoft;

/// <summary>
/// Closes the gap every other <c>*IntegrationTests.cs</c> file in this solution leaves open: each of those
/// proves one package against itself, never a single dispatch flowing through the real canonical pipeline
/// order (Logging → Auditing → Authorization → Validation → Transaction) across real package boundaries with
/// no mocks standing in for another Interlocutor package. This project wires a real <see cref="IServiceCollection"/>,
/// registers every handler and notification subscriber purely through <c>Interlocutor.Generator</c>'s
/// compile-time discovered registrations (no <c>ScanAssembly</c> call anywhere in this project), and commits
/// through a real EF Core unit of work against a Sqlite in-memory database. Beyond the canonical chain, this
/// class also proves the opt-in resilience behaviors (<c>RateLimitedBehavior</c>) and the real notification
/// fan-out (<c>IPublisher</c>) compose correctly with that same chain, in the same host built below - not a
/// separately constructed container.
/// </summary>
public sealed class CrossPackagePipelineTracerBulletTests : IDisposable
{
    /// <summary>
    /// The <see cref="IRateLimitedRequest.LimitKey"/> this project's real <see cref="DelegateRateLimiter"/>
    /// denies - every other key is allowed. Used by
    /// <see cref="CreateWidgetWithNotification_WhenRateLimited_NeverPublishesOrCommits"/> to throttle a
    /// dispatch without a mock standing in for <c>IRateLimiter</c>.
    /// </summary>
    private const string ThrottledLimitKey = "throttled";

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

    [Fact]
    public async Task CreateWidgetWithNotification_WhenAllowedByRateLimiter_CommitsAndFansOutNotificationInPinnedOrder()
    {
        // Arrange
        var provider = await this.BuildProviderAsync();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(
            new CreateWidgetWithNotificationCommand("Widget B", "allowed"),
            TestContext.Current.CancellationToken
        );

        // Assert: the real Outcome returned to the caller is Success.
        Assert.True(result.TryGetValue(out Success _));

        // Assert: a real row landed in the real database, committed by the real UnitOfWorkBehavior - read
        // through a second, independent context, same as the tracer-bullet test above.
        await using var verifyContext = this.CreateContext();
        Assert.Equal(["Widget B"], verifyContext.Widgets.Select(w => w.Name));

        // Assert: the notification the handler published mid-dispatch reached both real subscribers, in the
        // order WidgetCreatedAuditNotificationHandler's NotificationHandlerRunBeforeAttribute pins - the real
        // IPublisher's fan-out, resolved from the same scope the subscribers themselves were resolved into,
        // not a mock verifying a call was made.
        var log = scope.ServiceProvider.GetRequiredService<WidgetNotificationLog>();
        Assert.Equal(
            [nameof(WidgetCreatedAuditNotificationHandler), nameof(WidgetCreatedWelcomeEmailNotificationHandler)],
            log.HandlerNames
        );
    }

    [Fact]
    public async Task CreateWidgetWithNotification_WhenRateLimited_NeverPublishesOrCommits()
    {
        // Arrange
        var provider = await this.BuildProviderAsync();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act: ThrottledLimitKey is the one key this project's real DelegateRateLimiter disallows.
        var result = await mediator.SendAsync(
            new CreateWidgetWithNotificationCommand("Widget C", ThrottledLimitKey),
            TestContext.Current.CancellationToken
        );

        // Assert: RateLimitedBehavior short-circuits with the real RateLimited case - observed through the
        // real Outcome returned to the caller. RateLimitedBehavior is registered ahead of the transaction
        // behavior in this project's chain, so the handler (and the eager transaction
        // ITransactionalRequest asks for) never run at all.
        Assert.True(result.TryGetValue(out RateLimited rateLimited));
        Assert.Equal(ThrottledLimitKey, rateLimited.LimitKey);

        // Assert: no row was ever committed - the handler that would have inserted one never ran.
        await using var verifyContext = this.CreateContext();
        Assert.DoesNotContain("Widget C", verifyContext.Widgets.Select(w => w.Name));

        // Assert: the notification the handler would have published never went out either, since the
        // handler that publishes it never ran.
        var log = scope.ServiceProvider.GetRequiredService<WidgetNotificationLog>();
        Assert.Empty(log.HandlerNames);
    }

    /// <inheritdoc/>
    public void Dispose() => this.connection.Dispose();

    /// <summary>
    /// Composes the real container every test in this class dispatches through: the real
    /// <c>AddInterlocutor()</c>, the compile-time generated registrations for every handler and notification
    /// subscriber in this project, the full canonical behavior order plus <c>RateLimitedBehavior</c>, and a
    /// real EF Core unit of work against this instance's Sqlite in-memory connection. Every test builds its
    /// own provider from this one method - the same composition, not a bespoke one per scenario.
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
        // Real IRateLimiter, not a mock: denies exactly ThrottledLimitKey and allows every other key, so
        // CreateWidgetWithNotification_WhenRateLimited_NeverPublishesOrCommits can throttle a dispatch and
        // CreateWidgetWithNotification_WhenAllowedByRateLimiter_CommitsAndFansOutNotificationInPinnedOrder can
        // let one through, both against the same real RateLimitedBehavior. Registered before AddInterlocutor()
        // runs so it wins over AddRateLimitedBehavior()'s own try-added DefaultRateLimiter.
        services.AddSingleton<IRateLimiter>(
            new DelegateRateLimiter(
                (limitKey, _, _) => Task.FromResult(new RateLimitResult(IsAllowed: limitKey != ThrottledLimitKey))
            )
        );
        // Scoped, like the notification handlers themselves are (see RegistrationModuleEmitter), so each
        // test's own scope gets its own log instead of sharing state across dispatches.
        services.AddScoped<WidgetNotificationLog>();

        services.AddInterlocutor(builder =>
            builder
                // No ScanAssembly call: every handler and notification subscriber in this project is
                // discovered and registered purely through Interlocutor.Generator's compile-time generated
                // module, referenced as an analyzer in this project's own .csproj - proving the compile-time
                // discovered path, not runtime reflection.
                .AddGeneratedRegistrations(
                    Interlocutor.Generated.Interlocutor_IntegrationTests.InterlocutorGeneratedRegistrations.Registrations
                )
                .Pipeline.AddLoggingBehavior()
                .AddAuditingBehavior()
                .AddAuthorizationBehavior()
                .AddValidationBehavior()
                // Ahead of the transaction stage: RateLimitedBehavior has no prescribed position in the
                // canonical chain (documentation/pipeline-and-behaviors.md), but placed here it proves the
                // resilience and transaction behaviors interact correctly - a throttled
                // CreateWidgetWithNotificationCommand short-circuits before the transaction behavior ever
                // runs, so it never opens the eager transaction ITransactionalRequest asks for.
                .AddRateLimitedBehavior()
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
