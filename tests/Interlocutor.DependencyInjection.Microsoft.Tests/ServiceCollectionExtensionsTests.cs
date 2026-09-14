namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

public class ServiceCollectionExtensionsTests
{
    private static readonly string[] Expected = ["open-enter", "closed-enter", "handle", "closed-exit", "open-exit"];

    [Fact]
    public void AddInterlocutor_RegistersMediatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder => builder.ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly));
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void UseRecommendedChain_RegistersBehaviorsInCanonicalOrder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
            builder.ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly).Pipeline.UseRecommendedChain()
        );
        var behaviorImplementationTypes = services
            .Where(s => s.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(s => s.ImplementationType!.Name)
            .ToList();

        // Assert
        // Registration order must match UseRecommendedChain's documented canonical order (logging,
        // validation, transaction) - not merely contain each behavior. Auditing/Authorization are opt-in,
        // added via AddAuditingBehavior()/AddAuthorizationBehavior(), not part of this minimal default.
        Assert.Equal(["LoggingBehavior`2", "ValidationBehavior`2", "UnitOfWorkBehavior`2"], behaviorImplementationTypes);
    }

    [Fact]
    public void OverrideOutcomeDescriber_RegistersOutcomeDescriber()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly)
                .Pipeline.OverrideOutcomeDescriber<DefaultResponseOutcomeDescriber>()
        );
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IResponseOutcomeDescriber));

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void OverrideOutcomeDescriber_AllowsCustomOutcomeDescriber()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(OutcomeClassifierBuilder.DefaultClassifier);

        // Act
        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly)
                .Pipeline.OverrideOutcomeDescriber<DefaultResponseOutcomeDescriber>()
        );
        var provider = services.BuildServiceProvider();
        var describer = provider.GetRequiredService<IResponseOutcomeDescriber>();

        // Assert
        Assert.NotNull(describer);
        Assert.IsType<DefaultResponseOutcomeDescriber>(describer);
    }

    /// <summary>
    /// Regression test for the closed-generic behavior registration defect (ticket
    /// audit-followups/13): an open-generic implementation (<see cref="OpenGenericBehavior{TRequest,TResponse}"/>)
    /// and a closed-generic implementation (<see cref="ClosedGenericBehavior"/>, which implements
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/> directly rather than via an open type
    /// parameter) must both register successfully via <c>AddBehavior</c>, resolve, and execute - proving
    /// <see cref="InterlocutorServiceCollectionExtensions"/> no longer unconditionally pairs a closed
    /// implementation type with the open <see cref="IPipelineBehavior{TRequest,TResponse}"/> service type,
    /// which Microsoft DI rejects at <c>BuildServiceProvider()</c> time.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AddBehavior_WithOpenAndClosedGenericImplementations_BothResolveAndExecute()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);

        services.AddInterlocutor(builder =>
            builder
                .ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly)
                .Pipeline.AddBehavior(typeof(OpenGenericBehavior<,>))
                .AddBehavior(typeof(ClosedGenericBehavior))
        );

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new RegressionQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Expected, executionOrder);
        Assert.Equal("regression-handled", result.Value);
    }

    public sealed class OpenGenericBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly List<string> _executionOrder;

        public OpenGenericBehavior(List<string> executionOrder) => this._executionOrder = executionOrder;

        public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
        {
            this._executionOrder.Add("open-enter");
            var result = await next().ConfigureAwait(false);
            this._executionOrder.Add("open-exit");
            return result;
        }
    }

    public sealed class ClosedGenericBehavior : IPipelineBehavior<RegressionQuery, Outcome<string, Failure>>
    {
        private readonly List<string> _executionOrder;

        public ClosedGenericBehavior(List<string> executionOrder) => this._executionOrder = executionOrder;

        public async Task<Outcome<string, Failure>> HandleAsync(
            RegressionQuery request,
            Func<Task<Outcome<string, Failure>>> next,
            CancellationToken ct
        )
        {
            this._executionOrder.Add("closed-enter");
            var result = await next().ConfigureAwait(false);
            this._executionOrder.Add("closed-exit");
            return result;
        }
    }

    public sealed record RegressionQuery : IQuery<Outcome<string, Failure>>;

    public sealed class RegressionQueryHandler(List<string> executionOrder)
        : IQueryHandler<RegressionQuery, Outcome<string, Failure>>
    {
        public Task<Outcome<string, Failure>> HandleAsync(RegressionQuery query, CancellationToken ct)
        {
            executionOrder.Add("handle");
            return Task.FromResult<Outcome<string, Failure>>("regression-handled");
        }
    }
}
