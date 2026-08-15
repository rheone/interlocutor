namespace Interlocutor.Abstractions.Tests;

using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;

/// <summary>A query with no handler registered in any test's service collection — used to prove the
/// "unregistered request type" failure path.</summary>
public record UnregisteredQuery(int Value) : IQuery<OperationOutcome<int, Failure>>;

/// <summary>A void command with no handler registered in any test's service collection — used to prove the
/// "unregistered request type" failure path for the unit-command shape.</summary>
public record UnregisteredVoidCommand : ICommand;

/// <summary>A query whose handler always returns the failure case, used to prove the runtime-dispatched
/// outcome is pattern-matchable for failure without the caller knowing the concrete outcome shape.</summary>
public record FailingQuery : IQuery<OperationOutcome<int, Failure>>;

/// <summary>Handler for <see cref="FailingQuery"/> — always fails.</summary>
public class FailingQueryHandler : IQueryHandler<FailingQuery, OperationOutcome<int, Failure>>
{
    public Task<OperationOutcome<int, Failure>> Handle(FailingQuery query, CancellationToken ct) =>
        Task.FromResult(new OperationOutcome<int, Failure>(new Failure("boom")));
}

/// <summary>Tests for <see cref="ISender.Send(object, CancellationToken)"/> — the runtime-typed dispatch
/// overload used by infrastructure callers (outbox/inbox/queue consumers) that only have a deserialized
/// <see cref="object"/>, not a statically-typed request.</summary>
public class RuntimeTypedDispatchTests
{
    [Fact]
    public async Task Send_ObjectQuery_DispatchesToRegisteredHandler()
    {
        var sender = CreateSender(services =>
            services.AddSingleton<IQueryHandler<TestQuery, OperationOutcome<int, Failure>>, TestQueryHandler>()
        );

        object request = new TestQuery(21);
        var outcome = await sender.Send(request, CancellationToken.None);

        Assert.IsAssignableFrom<IOperationOutcome>(outcome);
        var union = Assert.IsAssignableFrom<IUnion>(outcome);
        Assert.Equal(42, Assert.IsType<int>(union.Value));
    }

    [Fact]
    public async Task Send_ObjectValueCommand_DispatchesToRegisteredHandler()
    {
        var sender = CreateSender(services =>
            services.AddSingleton<ICommandHandler<GreetCommand, OperationOutcome<string, Failure>>, GreetCommandHandler>()
        );

        object request = new GreetCommand("Ada");
        var outcome = await sender.Send(request, CancellationToken.None);

        var union = Assert.IsAssignableFrom<IUnion>(outcome);
        Assert.Equal("Hello Ada", Assert.IsType<string>(union.Value));
    }

    [Fact]
    public async Task Send_ObjectUnitCommand_DispatchesToRegisteredHandler()
    {
        var sender = CreateSender(services =>
            services.AddSingleton<IVoidCommandHandler<TestVoidCommand, OperationOutcome>, TestVoidHandler>()
        );

        object request = new TestVoidCommand("test");
        var outcome = await sender.Send(request, CancellationToken.None);

        var union = Assert.IsAssignableFrom<IUnion>(outcome);
        Assert.IsType<Success>(union.Value);
    }

    [Fact]
    public async Task Send_ObjectResult_IsPatternMatchableForFailure_WithoutKnowingConcreteType()
    {
        var sender = CreateSender(services =>
            services.AddSingleton<IQueryHandler<FailingQuery, OperationOutcome<int, Failure>>, FailingQueryHandler>()
        );

        object request = new FailingQuery();
        var outcome = await sender.Send(request, CancellationToken.None);

        // The caller here only has IOperationOutcome — no generic arguments, no knowledge of the concrete
        // OperationOutcome<TSuccess, TFailure> shape. IUnion.Value is the union's case value, and case types
        // (Failure, Success, plain data) can still be pattern-matched from it.
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
        var sender = CreateSender(services => { });

        // Assigned through an `object` variable first: UnregisteredQuery statically implements IQuery<T>, so
        // passing it directly would let overload resolution pick the strongly-typed generic Send<TResponse>
        // overload instead of the runtime-typed Send(object, ...) overload this test means to exercise.
        object request = new UnregisteredQuery(1);
        var ex = await Assert.ThrowsAsync<HandlerNotRegisteredException>(() => sender.Send(request, CancellationToken.None));

        Assert.Equal(typeof(UnregisteredQuery), ex.RequestType);
        Assert.Contains(nameof(UnregisteredQuery), ex.Message);
    }

    [Fact]
    public async Task Send_UnregisteredVoidCommandType_ThrowsHandlerNotRegisteredExceptionNamingTheType()
    {
        var sender = CreateSender(services => { });

        object request = new UnregisteredVoidCommand();
        var ex = await Assert.ThrowsAsync<HandlerNotRegisteredException>(() => sender.Send(request, CancellationToken.None));

        Assert.Equal(typeof(UnregisteredVoidCommand), ex.RequestType);
        Assert.Contains(nameof(UnregisteredVoidCommand), ex.Message);
    }

    [Fact]
    public async Task Send_ObjectThatIsNotARequest_ThrowsDistinctUnsupportedRequestTypeException()
    {
        var sender = CreateSender(services => { });

        var ex = await Assert.ThrowsAsync<UnsupportedRequestTypeException>(() =>
            sender.Send("just a string, not a request", CancellationToken.None)
        );

        Assert.Equal(typeof(string), ex.RequestType);
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        var sender = CreateSender(services => { });

        await Assert.ThrowsAsync<ArgumentNullException>(() => sender.Send(null!, CancellationToken.None));
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
