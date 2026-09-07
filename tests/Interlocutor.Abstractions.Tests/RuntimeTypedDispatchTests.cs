namespace Interlocutor.Abstractions.Tests;

using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;

/// <summary>A query with no handler registered in any test's service collection - used to prove the
/// "unregistered request type" failure path.</summary>
public record UnregisteredQuery(int Value) : IQuery<Outcome<int, Failure>>;

/// <summary>A bare command with no handler registered in any test's service collection - used to prove the
/// "unregistered request type" failure path for the unit-command shape.</summary>
public record UnregisteredBareCommand : ICommand;

/// <summary>A query whose handler always returns the failure case, used to prove the runtime-dispatched
/// outcome is pattern-matchable for failure without the caller knowing the concrete outcome shape.</summary>
public record FailingQuery : IQuery<Outcome<int, Failure>>;

/// <summary>Handler for <see cref="FailingQuery"/> - always fails.</summary>
public class FailingQueryHandler : IQueryHandler<FailingQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> HandleAsync(FailingQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(new Failure("boom")));
}

/// <summary>Tests for <see cref="ISender.SendAsync(object, CancellationToken)"/> - the runtime-typed dispatch
/// overload used by infrastructure callers (outbox/inbox/queue consumers) that only have a deserialized
/// <see cref="object"/>, not a statically-typed request.</summary>
public class RuntimeTypedDispatchTests
{
    [Fact]
    public async Task Send_ObjectQuery_DispatchesToRegisteredHandler()
    {
        // Arrange
        var sender = CreateSender(services =>
            services.AddSingleton<IQueryHandler<TestQuery, Outcome<int, Failure>>, TestQueryHandler>()
        );
        object request = new TestQuery(21);

        // Act
        var outcome = await sender.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<IOutcome>(outcome, exactMatch: false);
        var union = Assert.IsType<IUnion>(outcome, exactMatch: false);
        Assert.Equal(42, Assert.IsType<int>(union.Value));
    }

    [Fact]
    public async Task Send_ObjectQuery_MatchedByOverrideRule_RunsOnlyDeclaredBehaviors()
    {
        // Arrange - proves ticket 04's guarantee: the runtime-typed dispatch path (used for scenarios like
        // outbox replay, where the request's static type isn't known at the call site) resolves an
        // overridden pipeline identically to the ordinary typed Send<TResponse> path, since both call
        // PipelineChainResolver.ResolveChain.
        var executed = new List<string>();
        var sender = CreateSender(services =>
        {
            services.AddSingleton<IQueryHandler<TestQuery, Outcome<int, Failure>>, TestQueryHandler>();
            services.AddSingleton<IPipelineBehavior<TestQuery, Outcome<int, Failure>>>(
                new DroppedTrackingBehavior<TestQuery, Outcome<int, Failure>>(executed.Add)
            );
            services.AddSingleton<IPipelineBehavior<TestQuery, Outcome<int, Failure>>>(
                new KeptTrackingBehavior<TestQuery, Outcome<int, Failure>>(executed.Add)
            );
            services.AddSingleton<IPipelineOverrideRegistry>(
                new FakeOverrideRegistry(t =>
                    t == typeof(TestQuery) ? [typeof(KeptTrackingBehavior<TestQuery, Outcome<int, Failure>>)] : null
                )
            );
        });
        object request = new TestQuery(21);

        // Act
        await sender.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(["kept"], executed);
    }

    [Fact]
    public async Task Send_ObjectQuery_UnmatchedByOverrideRule_RunsGeneralPurposeBaselineUnaffected()
    {
        // Arrange - proves ticket 04's other guarantee: a registry is present, but its only rule targets a
        // different request type, so this request's runtime-typed dispatch must draw from the unaffected
        // general-purpose baseline, identical to today's behavior.
        var executed = new List<string>();
        var sender = CreateSender(services =>
        {
            services.AddSingleton<IQueryHandler<TestQuery, Outcome<int, Failure>>, TestQueryHandler>();
            services.AddSingleton<IPipelineBehavior<TestQuery, Outcome<int, Failure>>>(
                new KeptTrackingBehavior<TestQuery, Outcome<int, Failure>>(executed.Add)
            );
            services.AddSingleton<IPipelineOverrideRegistry>(
                new FakeOverrideRegistry(t =>
                    t == typeof(GreetCommand) ? [typeof(KeptTrackingBehavior<GreetCommand, Outcome<string, Failure>>)] : null
                )
            );
        });
        object request = new TestQuery(21);

        // Act
        await sender.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(["kept"], executed);
    }

    [Fact]
    public async Task Send_ObjectValueCommand_DispatchesToRegisteredHandler()
    {
        // Arrange
        var sender = CreateSender(services =>
            services.AddSingleton<ICommandHandler<GreetCommand, Outcome<string, Failure>>, GreetCommandHandler>()
        );
        object request = new GreetCommand("Ada");

        // Act
        var outcome = await sender.SendAsync(request, CancellationToken.None);

        // Assert
        var union = Assert.IsType<IUnion>(outcome, exactMatch: false);
        Assert.Equal("Hello Ada", Assert.IsType<string>(union.Value));
    }

    [Fact]
    public async Task Send_ObjectUnitCommand_DispatchesToRegisteredHandler()
    {
        // Arrange
        var sender = CreateSender(services =>
            services.AddSingleton<IBareCommandHandler<TestBareCommand, Outcome>, TestBareHandler>()
        );
        object request = new TestBareCommand("test");

        // Act
        var outcome = await sender.SendAsync(request, CancellationToken.None);

        // Assert - TestBareHandler returns the case-less Outcome, whose Value is always null (see its own
        // remarks); reaching this point without throwing already proves the dispatch reached the handler.
        var union = Assert.IsType<IUnion>(outcome, exactMatch: false);
        Assert.Null(union.Value);
    }

    [Fact]
    public async Task Send_ObjectResult_IsPatternMatchableForFailure_WithoutKnowingConcreteType()
    {
        // Arrange
        var sender = CreateSender(services =>
            services.AddSingleton<IQueryHandler<FailingQuery, Outcome<int, Failure>>, FailingQueryHandler>()
        );
        object request = new FailingQuery();

        // Act
        var outcome = await sender.SendAsync(request, CancellationToken.None);

        // Assert - The caller here only has IOutcome - no generic arguments, no knowledge of the
        // concrete Outcome<T1, T2> shape. IUnion.Value is the union's case value, and
        // case types (Failure, Success, plain data) can still be pattern-matched from it.
        var isFailure = outcome switch
        {
            IUnion { Value: Failure failure } => failure.Reason == "boom",
            IUnion { Value: int } => false,
            _ => throw new InvalidOperationException("Unexpected outcome shape."),
        };

        Assert.True(isFailure);
    }

    [Fact]
    public async Task Send_UnregisteredQueryType_ThrowsHandlerNotRegisteredExceptionNamingTheType()
    {
        // Arrange - Assigned through an `object` variable first: UnregisteredQuery statically implements
        // IQuery<T>, so passing it directly would let overload resolution pick the strongly-typed generic
        // Send<TResponse> overload instead of the runtime-typed Send(object, ...) overload this test means
        // to exercise.
        var sender = CreateSender(_ => { });
        object request = new UnregisteredQuery(1);

        // Act
        var ex = await Assert.ThrowsAsync<HandlerNotRegisteredException>(() =>
            sender.SendAsync(request, CancellationToken.None)
        );

        // Assert
        Assert.Equal(typeof(UnregisteredQuery), ex.RequestType);
        Assert.Contains(nameof(UnregisteredQuery), ex.Message);
    }

    [Fact]
    public async Task Send_UnregisteredBareCommandType_ThrowsHandlerNotRegisteredExceptionNamingTheType()
    {
        // Arrange
        var sender = CreateSender(_ => { });
        object request = new UnregisteredBareCommand();

        // Act
        var ex = await Assert.ThrowsAsync<HandlerNotRegisteredException>(() =>
            sender.SendAsync(request, CancellationToken.None)
        );

        // Assert
        Assert.Equal(typeof(UnregisteredBareCommand), ex.RequestType);
        Assert.Contains(nameof(UnregisteredBareCommand), ex.Message);
    }

    [Fact]
    public async Task Send_ObjectThatIsNotARequest_ThrowsDistinctUnsupportedRequestTypeException()
    {
        // Arrange
        var sender = CreateSender(_ => { });

        // Act
        var ex = await Assert.ThrowsAsync<UnsupportedRequestTypeException>(() =>
            sender.SendAsync("just a string, not a request", CancellationToken.None)
        );

        // Assert
        Assert.Equal(typeof(string), ex.RequestType);
    }

    [Fact]
    public Task Send_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sender = CreateSender(_ => { });

        // Act & Assert
        return Assert.ThrowsAsync<ArgumentNullException>(() => sender.SendAsync(null!, CancellationToken.None));
    }

    private static ISender CreateSender(Action<ServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        services.AddSingleton<IMediator, Mediator>();
        services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ISender>();
    }
}
