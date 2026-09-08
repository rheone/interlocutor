namespace Interlocutor.Abstractions.Tests;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;

public record TestQuery(int Value) : IQuery<Outcome<int, Failure>>;

public class TestQueryHandler : IQueryHandler<TestQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> HandleAsync(TestQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(query.Value * 2));
}

/// <summary>
/// A genuinely infallible query that still produces a value - the real end-to-end proof that
/// <see cref="Outcome{T}"/> (arity 1: <see cref="IUnion"/>-only, not <see cref="IOutcome"/>) is legal as a
/// handler's <c>TResponse</c> and dispatches through the full pipeline exactly like every other arity.
/// </summary>
public record TestSinglePayloadQuery(int Value) : IQuery<Outcome<int>>;

public class TestSinglePayloadQueryHandler : IQueryHandler<TestSinglePayloadQuery, Outcome<int>>
{
    public Task<Outcome<int>> HandleAsync(TestSinglePayloadQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int>(query.Value * 2));
}

public record GreetCommand(string PersonName) : ICommand<Outcome<string, Failure>>;

public class GreetCommandHandler : ICommandHandler<GreetCommand, Outcome<string, Failure>>
{
    public Task<Outcome<string, Failure>> HandleAsync(GreetCommand command, CancellationToken ct) =>
        Task.FromResult(new Outcome<string, Failure>($"Hello {command.PersonName}"));
}

public record TestBareCommand(string Value) : ICommand;

public class TestBareHandler : IBareCommandHandler<TestBareCommand, Outcome>
{
    public Task<Outcome> HandleAsync(TestBareCommand command, CancellationToken ct) => Task.FromResult(default(Outcome));
}

/// <summary>A second <see cref="TestBareCommand"/> handler, registered against <see cref="Outcome{T1, T2}"/>
/// (arity 2) rather than the case-less arity-0 <see cref="Outcome"/> - used only by the eject test, which needs
/// a response shape an ejected value can be distinguished from.</summary>
public class TestBareHandlerWithFailureCase : IBareCommandHandler<TestBareCommand, Outcome<Success, Failure>>
{
    public Task<Outcome<Success, Failure>> HandleAsync(TestBareCommand command, CancellationToken ct) =>
        Task.FromResult<Outcome<Success, Failure>>(default(Success));
}

public record TestNotification(string Message) : INotification;

/// <summary>A query that also satisfies <see cref="IAuditableRequest"/> - used to prove the marker-leak-prevention
/// guarantee: an override rule matching this request excludes a marker-constrained behavior it does not name,
/// even though the request structurally satisfies that behavior's generic constraint.</summary>
public record AuditableTestQuery(int Value) : IQuery<Outcome<int, Failure>>, IAuditableRequest
{
    public string ActionName => "Test.Action";
}

public class AuditableTestQueryHandler : IQueryHandler<AuditableTestQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> HandleAsync(AuditableTestQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(query.Value));
}

/// <summary>A marker-constrained behavior - registered as an open generic, like the library's own
/// <see cref="AuditingBehavior{TRequest,TResponse}"/> - that would resolve for any <see cref="IAuditableRequest"/>
/// via the container's ordinary open-generic resolution unless an override rule excludes it.</summary>
/// <typeparam name="TRequest">The request type, constrained to <see cref="IAuditableRequest"/> so the container's
/// open-generic resolution considers this behavior a candidate for any marker-implementing request.</typeparam>
/// <typeparam name="TResponse">The response type flowing through this behavior unchanged.</typeparam>
public class MarkerConstrainedTrackingBehavior<TRequest, TResponse>(Action track) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditableRequest
{
    public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        track();
        return next();
    }
}

public class TrackingBehavior<TRequest, TResponse>(Action track) : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        track();
        return next();
    }
}

/// <summary>A query handler that ejects from several method calls deep in its own logic, proving
/// <see cref="Eject"/> works without manually threading the chosen outcome back up through every frame.</summary>
public class EjectingQueryHandler : IQueryHandler<TestQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> HandleAsync(TestQuery query, CancellationToken ct) => InnerAsync(query);

    private static Task<Outcome<int, Failure>> InnerAsync(TestQuery query) => DeepestAsync(query);

    private static Task<Outcome<int, Failure>> DeepestAsync(TestQuery query) =>
        Task.FromResult(Eject.With<Outcome<int, Failure>>(new Failure($"ejected for {query.Value}")));
}

/// <summary>A behavior that ejects before calling <c>next()</c> - the handler (and any behavior further in)
/// must never run.</summary>
/// <typeparam name="TRequest">The request type, unconstrained since this behavior never inspects it.</typeparam>
/// <typeparam name="TResponse">The response type, constrained to <see cref="IUnion"/> so
/// <paramref name="response"/> can be ejected via <see cref="Eject.With{TResult}(TResult)"/>.</typeparam>
public class EjectingBeforeNextBehavior<TRequest, TResponse>(TResponse response) : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IUnion
{
    public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) =>
        Task.FromResult(Eject.With(response));
}

/// <summary>A behavior that always throws a plain <see cref="InvalidOperationException"/> - used to prove the
/// eject catch added to every composition site is scoped to <see cref="PipelineEjectException{TResponse}"/>
/// only, and does not swallow an unrelated fault.</summary>
/// <typeparam name="TRequest">The request type, unconstrained since this behavior never inspects it before
/// throwing.</typeparam>
/// <typeparam name="TResponse">The response type, unconstrained since this behavior never produces one.</typeparam>
public class ThrowingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) =>
        throw new InvalidOperationException("not an eject");
}

/// <summary>A behavior that calls <c>next()</c> first, then ejects from its own post-<c>next()</c> logic -
/// proving the eject is recovered at this behavior's own call site, not only when thrown straight from a
/// handler.</summary>
/// <typeparam name="TRequest">The request type, unconstrained since this behavior never inspects it.</typeparam>
/// <typeparam name="TResponse">The response type, constrained to <see cref="IUnion"/> so
/// <paramref name="response"/> can be ejected via <see cref="Eject.With{TResult}(TResult)"/>.</typeparam>
public class EjectingAfterNextBehavior<TRequest, TResponse>(TResponse response) : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IUnion
{
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        _ = await next().ConfigureAwait(false);
        return Eject.With(response);
    }
}

/// <summary>A tracking behavior an override rule can name to keep in a matched request's resolved chain -
/// a distinct type from <see cref="DroppedTrackingBehavior{TRequest,TResponse}"/> so
/// <c>PipelineChainResolver</c>'s type-based matching can tell the two apart.</summary>
/// <typeparam name="TRequest">The request type, unconstrained - only this behavior's own type identity matters
/// to the override-rule matching under test.</typeparam>
/// <typeparam name="TResponse">The response type flowing through this behavior unchanged.</typeparam>
public class KeptTrackingBehavior<TRequest, TResponse>(Action<string> track) : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        track("kept");
        return next();
    }
}

/// <summary>A tracking behavior registered as part of the general-purpose baseline but never named by an
/// override rule - used to prove it is excluded once a rule matches. See
/// <see cref="KeptTrackingBehavior{TRequest,TResponse}"/>.</summary>
/// <typeparam name="TRequest">The request type, unconstrained - only this behavior's own type identity matters
/// to the override-rule matching under test.</typeparam>
/// <typeparam name="TResponse">The response type flowing through this behavior unchanged.</typeparam>
public class DroppedTrackingBehavior<TRequest, TResponse>(Action<string> track) : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        track("dropped");
        return next();
    }
}

/// <summary>A minimal <see cref="IPipelineOverrideRegistry"/> test double - looks a request type up via
/// <paramref name="lookup"/> instead of being built from real <c>InterlocutorPipelineBuilder</c> rules, so
/// these tests exercise <see cref="Mediator"/>'s wiring without depending on
/// <c>Interlocutor.DependencyInjection.Abstractions</c>.</summary>
public class FakeOverrideRegistry(Func<Type, IReadOnlyList<Type>?> lookup) : IPipelineOverrideRegistry
{
    public PipelineOverrideMatch? TryGetOverride(Type requestType)
    {
        var found = lookup(requestType);
        return found is null ? null : new PipelineOverrideMatch(found, "fake-rule");
    }
}

public class MediatorTests
{
    [Fact]
    public async Task Send_Query_ReturnsHandlerResult()
    {
        // Arrange
        var mediator = CreateMediator<TestQueryHandler>(_ => { });

        // Act
        var result = await mediator.SendAsync(new TestQuery(21), CancellationToken.None);

        // Assert
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Send_Query_RunsPipelineBehaviors()
    {
        // Arrange
        var invoked = false;
        var mediator = CreateMediator<TestQueryHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestQuery, Outcome<int, Failure>>>(
                new TrackingBehavior<TestQuery, Outcome<int, Failure>>(() => invoked = true)
            );
        });

        // Act
        await mediator.SendAsync(new TestQuery(1), CancellationToken.None);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_Query_MatchedByOverrideRule_RunsOnlyDeclaredBehaviors()
    {
        // Arrange - two behaviors registered, but the override rule names only "kept"; the general-purpose
        // baseline behavior ("dropped") must not run for a request the override matches.
        var executed = new List<string>();
        var mediator = CreateMediator<TestQueryHandler>(services =>
        {
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

        // Act
        await mediator.SendAsync(new TestQuery(1), CancellationToken.None);

        // Assert
        Assert.Equal(["kept"], executed);
    }

    [Fact]
    public async Task Send_Query_MatchedByOverrideRule_DoesNotRunMarkerConstrainedBehaviorItDoesNotName()
    {
        // Arrange - AuditableTestQuery structurally satisfies MarkerConstrainedTrackingBehavior's generic
        // constraint, so the container's ordinary open-generic resolution would normally include it. The
        // override rule matches this request but declares only KeptTrackingBehavior, so the marker-constrained
        // behavior must not run.
        var executed = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<AuditableTestQuery, Outcome<int, Failure>>, AuditableTestQueryHandler>();
        services.AddSingleton<IPipelineBehavior<AuditableTestQuery, Outcome<int, Failure>>>(
            new MarkerConstrainedTrackingBehavior<AuditableTestQuery, Outcome<int, Failure>>(() => executed.Add("marker"))
        );
        services.AddSingleton<IPipelineBehavior<AuditableTestQuery, Outcome<int, Failure>>>(
            new KeptTrackingBehavior<AuditableTestQuery, Outcome<int, Failure>>(executed.Add)
        );
        services.AddSingleton<IPipelineOverrideRegistry>(
            new FakeOverrideRegistry(t =>
                t == typeof(AuditableTestQuery)
                    ? [typeof(KeptTrackingBehavior<AuditableTestQuery, Outcome<int, Failure>>)]
                    : null
            )
        );
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new AuditableTestQuery(1), CancellationToken.None);

        // Assert
        Assert.Equal(["kept"], executed);
    }

    [Fact]
    public async Task Send_Query_HandlerEjectsDeepInOwnCallGraph_ReturnsEjectedOutcomeWithoutThrowing()
    {
        // Arrange
        var mediator = CreateMediator<EjectingQueryHandler>(_ => { });

        // Act
        var result = await mediator.SendAsync(new TestQuery(7), CancellationToken.None);

        // Assert - the dispatch completes normally with the chosen outcome; Eject.With never surfaces as a
        // thrown exception to this caller.
        Assert.Equal(new Failure("ejected for 7"), result.Value);
    }

    [Fact]
    public async Task Send_Query_OutermostBehaviorEjectsBeforeNext_HandlerNeverRuns()
    {
        // Arrange
        var handlerRan = false;
        var mediator = CreateMediator<TestQueryHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestQuery, Outcome<int, Failure>>>(
                new EjectingBeforeNextBehavior<TestQuery, Outcome<int, Failure>>(
                    new Outcome<int, Failure>(new Failure("dead end"))
                )
            );
            services.AddSingleton<IPipelineBehavior<TestQuery, Outcome<int, Failure>>>(
                new TrackingBehavior<TestQuery, Outcome<int, Failure>>(() => handlerRan = true)
            );
        });

        // Act
        var result = await mediator.SendAsync(new TestQuery(1), CancellationToken.None);

        // Assert
        Assert.Equal(new Failure("dead end"), result.Value);
        Assert.False(handlerRan, "a behavior that ejects before calling next() must skip the handler entirely");
    }

    [Fact]
    public async Task Send_Query_InnerBehaviorEjectsAfterNext_OuterBehaviorsPostNextLogicStillRuns()
    {
        // Arrange - EjectingAfterNextBehavior calls next() (running the handler and the inner tracking
        // behavior) and then ejects from its own post-next() logic. An outer TrackingBehavior wraps it and
        // must still see this as an ordinary returned response, proving its own post-next() logic runs
        // exactly as if nothing had been thrown.
        var outerRan = false;
        var mediator = CreateMediator<TestQueryHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestQuery, Outcome<int, Failure>>>(
                new TrackingBehavior<TestQuery, Outcome<int, Failure>>(() => outerRan = true)
            );
            services.AddSingleton<IPipelineBehavior<TestQuery, Outcome<int, Failure>>>(
                new EjectingAfterNextBehavior<TestQuery, Outcome<int, Failure>>(
                    new Outcome<int, Failure>(new Failure("dead end after next"))
                )
            );
        });

        // Act
        var result = await mediator.SendAsync(new TestQuery(1), CancellationToken.None);

        // Assert
        Assert.Equal(new Failure("dead end after next"), result.Value);
        Assert.True(outerRan, "the outer behavior's Handle call must complete normally, not observe an exception");
    }

    [Fact]
    public async Task Send_Command_RunsPipelineBehaviors()
    {
        // Arrange
        var invoked = false;
        var mediator = CreateMediator<GreetCommandHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<GreetCommand, Outcome<string, Failure>>>(
                new TrackingBehavior<GreetCommand, Outcome<string, Failure>>(() => invoked = true)
            );
        });

        // Act
        await mediator.SendAsync(new GreetCommand("test"), CancellationToken.None);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_Command_BehaviorEjectsBeforeNext_ReturnsEjectedOutcomeWithoutThrowing()
    {
        // Arrange - covers CommandHandlerWrapper's composition site, distinct from QueryHandlerWrapper's.
        var mediator = CreateMediator<GreetCommandHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<GreetCommand, Outcome<string, Failure>>>(
                new EjectingBeforeNextBehavior<GreetCommand, Outcome<string, Failure>>(
                    new Outcome<string, Failure>(new Failure("dead end"))
                )
            );
        });

        // Act
        var result = await mediator.SendAsync(new GreetCommand("test"), CancellationToken.None);

        // Assert
        Assert.Equal(new Failure("dead end"), result.Value);
    }

    [Fact]
    public async Task Send_BareCommand_RunsPipelineBehaviors()
    {
        // Arrange
        var invoked = false;
        var mediator = CreateMediator<TestBareHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestBareCommand, Outcome>>(
                new TrackingBehavior<TestBareCommand, Outcome>(() => invoked = true)
            );
        });

        // Act
        await mediator.SendAsync<Outcome>(new TestBareCommand("test"), CancellationToken.None);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_Query_WithSinglePayloadOutcome_RunsPipelineBehaviorsAndReturnsTheValue()
    {
        // Arrange - Outcome<T> implements IUnion directly, never IOutcome (see its own remarks); this proves
        // that constraint alone is enough for a genuinely infallible, single-payload response to dispatch
        // through the full pipeline exactly like every other arity, not just compile against TResponse in
        // isolation.
        var invoked = false;
        var mediator = CreateMediator<TestSinglePayloadQueryHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestSinglePayloadQuery, Outcome<int>>>(
                new TrackingBehavior<TestSinglePayloadQuery, Outcome<int>>(() => invoked = true)
            );
        });

        // Act
        var result = await mediator.SendAsync(new TestSinglePayloadQuery(21), CancellationToken.None);

        // Assert
        Assert.True(invoked);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Send_BareCommand_BehaviorEjectsBeforeNext_ReturnsEjectedOutcomeWithoutThrowing()
    {
        // Arrange - covers BareCommandHandlerWrapper's composition site, distinct from the other two wrappers.
        // Uses Outcome<Success, Failure> rather than the case-less arity-0 Outcome the other tests in this
        // class use: the ejected value needs to be distinguishable from whatever TestBareHandlerWithFailureCase
        // itself would have returned, which a case-less Outcome (every instance equal to every other) can't be.
        var services = new ServiceCollection();
        services.AddSingleton<
            IBareCommandHandler<TestBareCommand, Outcome<Success, Failure>>,
            TestBareHandlerWithFailureCase
        >();
        services.AddSingleton<IPipelineBehavior<TestBareCommand, Outcome<Success, Failure>>>(
            new EjectingBeforeNextBehavior<TestBareCommand, Outcome<Success, Failure>>(new Failure("dead end"))
        );
        services.AddSingleton<IMediator, Mediator>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync<Outcome<Success, Failure>>(new TestBareCommand("test"), CancellationToken.None);

        // Assert
        Assert.Equal(new Failure("dead end"), result.Value);
    }

    [Fact]
    public Task Send_Query_NonEjectException_PropagatesUnchanged()
    {
        // Arrange - proves the catch added to every composition site is narrowly scoped to
        // PipelineEjectException<TResponse> only; any other exception must still propagate out of Send.
        var mediator = CreateMediator<TestQueryHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestQuery, Outcome<int, Failure>>>(
                new ThrowingBehavior<TestQuery, Outcome<int, Failure>>()
            );
        });

        // Act & Assert
        return Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new TestQuery(1), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Send_BareCommand_ReturnsAcknowledgment()
    {
        // Arrange
        var mediator = CreateMediator<TestBareHandler>(_ => { });

        // Act
        var result = await mediator.SendAsync<Outcome>(new TestBareCommand("test"), CancellationToken.None);

        // Assert - holding an Outcome instance is itself the acknowledgment that TestBareHandler ran; there is
        // nothing further to inspect on it (see Outcome's own remarks).
        Assert.Equal(default(Outcome), result);
    }

    [Fact]
    public async Task Publish_Sequential_InvokesHandlers()
    {
        // Arrange
        var invoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(new TestNotificationHandler(() => invoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.PublishAsync(new TestNotification("hello"), CancellationToken.None);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task Publish_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - no handlers registered for TestNotification; publishing must not throw.
        var exception = await Record.ExceptionAsync(() =>
            mediator.PublishAsync(new TestNotification("hello"), CancellationToken.None)
        );

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Publish_Sequential_StopsOnFailure()
    {
        // Arrange
        var secondInvoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(
            new TestNotificationHandler(() => throw new InvalidOperationException("First handler fails"))
        );
        services.AddSingleton<INotificationHandler<TestNotification>>(new TestNotificationHandler(() => secondInvoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.PublishAsync(new TestNotification("hello"), CancellationToken.None)
        );

        // Assert
        Assert.False(secondInvoked);
    }

    [Fact]
    public async Task Send_Query_ReusedAcrossMultipleDispatches_ReturnsCorrectResultEachTime()
    {
        // Arrange - the wrapper Mediator builds per closed request type is cached internally; sending the
        // same query type repeatedly must keep returning a result derived from that specific dispatch's
        // input, not a stale or shared one.
        var mediator = CreateMediator<TestQueryHandler>(_ => { });

        // Act
        var first = await mediator.SendAsync(new TestQuery(1), CancellationToken.None);
        var second = await mediator.SendAsync(new TestQuery(2), CancellationToken.None);
        var third = await mediator.SendAsync(new TestQuery(3), CancellationToken.None);

        // Assert
        Assert.Equal(2, first.Value);
        Assert.Equal(4, second.Value);
        Assert.Equal(6, third.Value);
    }

    [Fact]
    public async Task Mediator_ImplementsISender()
    {
        // Arrange
        var mediator = CreateMediator<TestQueryHandler>(_ => { });

        // Act & Assert
        Assert.IsType<ISender>(mediator, exactMatch: false);
    }

    [Fact]
    public async Task Mediator_ImplementsIPublisher()
    {
        // Arrange
        var mediator = CreateMediator<TestQueryHandler>(_ => { });

        // Act & Assert
        Assert.IsType<IPublisher>(mediator, exactMatch: false);
    }

    private static IMediator CreateMediator<THandler>(Action<ServiceCollection> configure)
    {
        var services = new ServiceCollection();

        if (typeof(THandler) == typeof(TestQueryHandler))
        {
            services.AddSingleton<IQueryHandler<TestQuery, Outcome<int, Failure>>, TestQueryHandler>();
        }
        else if (typeof(THandler) == typeof(TestSinglePayloadQueryHandler))
        {
            services.AddSingleton<IQueryHandler<TestSinglePayloadQuery, Outcome<int>>, TestSinglePayloadQueryHandler>();
        }
        else if (typeof(THandler) == typeof(EjectingQueryHandler))
        {
            services.AddSingleton<IQueryHandler<TestQuery, Outcome<int, Failure>>, EjectingQueryHandler>();
        }
        else if (typeof(THandler) == typeof(GreetCommandHandler))
        {
            services.AddSingleton<ICommandHandler<GreetCommand, Outcome<string, Failure>>, GreetCommandHandler>();
        }
        else if (typeof(THandler) == typeof(TestBareHandler))
        {
            services.AddSingleton<IBareCommandHandler<TestBareCommand, Outcome>, TestBareHandler>();
        }

        configure(services);
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }
}

public class TestNotificationHandler(Action action) : INotificationHandler<TestNotification>
{
    public Task HandleAsync(TestNotification notification, CancellationToken ct)
    {
        action();
        return Task.CompletedTask;
    }
}
