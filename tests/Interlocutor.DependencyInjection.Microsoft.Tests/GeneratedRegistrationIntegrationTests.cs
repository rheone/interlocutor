namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves a handler discovered by <c>Interlocutor.Generator</c> (rather than runtime assembly scanning)
/// resolves and dispatches correctly through Microsoft DI, including a constructor dependency - the
/// acceptance criteria "Both containers work against generated registrations" and "A handler with
/// constructor dependencies resolves and receives them".
/// </summary>
public sealed class GeneratedRegistrationIntegrationTests
{
    public interface IGreeter
    {
        string Greet();
    }

    [Fact]
    public async Task GeneratedRegistration_ResolvesHandlerWithConstructorDependency_AndDispatchesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter, Greeter>();

        // No ScanAssembly call: the only source of handler registrations here is the generator's own
        // compile-time discovered module - proving the generated path works on its own, not merely
        // alongside runtime scanning.
        services.AddInterlocutor(builder =>
            builder.AddGeneratedRegistrations(
                Interlocutor
                    .Generated
                    .Interlocutor_DependencyInjection_Microsoft_Tests
                    .InterlocutorGeneratedRegistrations
                    .Registrations
            )
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new GeneratedQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("hi from Microsoft DI", result.Value);
    }

    [Fact]
    public async Task GeneratedRegistration_WithMultipleBehaviors_ExecutesBehaviorsInCorrectOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter, Greeter>();
        services.AddSingleton(executionOrder);

        services.AddInterlocutor(builder =>
            builder
                .AddGeneratedRegistrations(
                    Interlocutor
                        .Generated
                        .Interlocutor_DependencyInjection_Microsoft_Tests
                        .InterlocutorGeneratedRegistrations
                        .Registrations
                )
                .Pipeline.AddBehavior(typeof(OrderTrackingBehavior1))
                .AddBehavior(typeof(OrderTrackingBehavior2))
                .AddBehavior(typeof(OrderTrackingBehavior3))
        );

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new GeneratedQuery(), TestContext.Current.CancellationToken);

        // Assert
        // First registered wraps the outermost layer (documented in documentation/pipeline-and-behaviors.md):
        // behaviors registered as 1→2→3 enter in that same order and exit in reverse (3→2→1 on exit).
        // Each behavior records entry and exit to verify the wrap order is correct.
        var expected = new[] { "B1-enter", "B2-enter", "B3-enter", "handle", "B3-exit", "B2-exit", "B1-exit" };
        Assert.Equal(expected, executionOrder);
        Assert.Equal("hi from Microsoft DI", result.Value);
    }

    public sealed class OrderTrackingBehavior1 : IPipelineBehavior<GeneratedQuery, OperationOutcome<string, Failure>>
    {
        private readonly List<string> _executionOrder;

        public OrderTrackingBehavior1(List<string> executionOrder) => _executionOrder = executionOrder;

        public async Task<OperationOutcome<string, Failure>> Handle(
            GeneratedQuery request,
            Func<Task<OperationOutcome<string, Failure>>> next,
            CancellationToken ct
        )
        {
            _executionOrder.Add("B1-enter");
            var result = await next().ConfigureAwait(false);
            _executionOrder.Add("B1-exit");
            return result;
        }
    }

    public sealed class OrderTrackingBehavior2 : IPipelineBehavior<GeneratedQuery, OperationOutcome<string, Failure>>
    {
        private readonly List<string> _executionOrder;

        public OrderTrackingBehavior2(List<string> executionOrder) => _executionOrder = executionOrder;

        public async Task<OperationOutcome<string, Failure>> Handle(
            GeneratedQuery request,
            Func<Task<OperationOutcome<string, Failure>>> next,
            CancellationToken ct
        )
        {
            _executionOrder.Add("B2-enter");
            var result = await next().ConfigureAwait(false);
            _executionOrder.Add("B2-exit");
            return result;
        }
    }

    public sealed class OrderTrackingBehavior3 : IPipelineBehavior<GeneratedQuery, OperationOutcome<string, Failure>>
    {
        private readonly List<string> _executionOrder;

        public OrderTrackingBehavior3(List<string> executionOrder) => _executionOrder = executionOrder;

        public async Task<OperationOutcome<string, Failure>> Handle(
            GeneratedQuery request,
            Func<Task<OperationOutcome<string, Failure>>> next,
            CancellationToken ct
        )
        {
            _executionOrder.Add("B3-enter");
            var result = await next().ConfigureAwait(false);
            _executionOrder.Add("B3-exit");
            return result;
        }
    }

    public sealed record GeneratedQuery : IQuery<OperationOutcome<string, Failure>>;

    public sealed class GeneratedQueryHandler(IGreeter greeter, List<string>? executionOrder = null)
        : IQueryHandler<GeneratedQuery, OperationOutcome<string, Failure>>
    {
        public Task<OperationOutcome<string, Failure>> Handle(GeneratedQuery query, CancellationToken ct)
        {
            executionOrder?.Add("handle");
            return Task.FromResult<OperationOutcome<string, Failure>>(greeter.Greet());
        }
    }

    public sealed class Greeter : IGreeter
    {
        public string Greet() => "hi from Microsoft DI";
    }
}
