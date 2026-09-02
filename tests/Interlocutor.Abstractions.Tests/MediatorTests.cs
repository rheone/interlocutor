namespace Interlocutor.Abstractions.Tests;

using System.Diagnostics;
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
    public Task<Outcome<int, Failure>> Handle(TestQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(query.Value * 2));
}

public record GreetCommand(string PersonName) : ICommand<Outcome<string, Failure>>;

public class GreetCommandHandler : ICommandHandler<GreetCommand, Outcome<string, Failure>>
{
    public Task<Outcome<string, Failure>> Handle(GreetCommand command, CancellationToken ct) =>
        Task.FromResult(new Outcome<string, Failure>($"Hello {command.PersonName}"));
}

public record TestVoidCommand(string Value) : ICommand;

public class TestVoidHandler : IVoidCommandHandler<TestVoidCommand, Outcome>
{
    public Task<Outcome> Handle(TestVoidCommand command, CancellationToken ct) =>
        Task.FromResult(new Outcome(default(Success)));
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
    public Task<Outcome<int, Failure>> Handle(AuditableTestQuery query, CancellationToken ct) =>
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
    public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        track();
        return next();
    }
}

public class TrackingBehavior<TRequest, TResponse>(Action track) : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        track();
        return next();
    }
}

/// <summary>A query handler that ejects from several method calls deep in its own logic, proving
/// <see cref="Eject"/> works without manually threading the chosen outcome back up through every frame.</summary>
public class EjectingQueryHandler : IQueryHandler<TestQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> Handle(TestQuery query, CancellationToken ct) => Inner(query);

    private static Task<Outcome<int, Failure>> Inner(TestQuery query) => Deepest(query);

    private static Task<Outcome<int, Failure>> Deepest(TestQuery query) =>
        Task.FromResult(Eject.With<Outcome<int, Failure>>(new Failure($"ejected for {query.Value}")));
}

/// <summary>A behavior that ejects before calling <c>next()</c> - the handler (and any behavior further in)
/// must never run.</summary>
/// <typeparam name="TRequest">The request type, unconstrained since this behavior never inspects it.</typeparam>
/// <typeparam name="TResponse">The response type, constrained to <see cref="IOutcome"/> so
/// <paramref name="response"/> can be ejected via <see cref="Eject.With{TResult}(TResult)"/>.</typeparam>
public class EjectingBeforeNextBehavior<TRequest, TResponse>(TResponse response) : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IOutcome
{
    public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) =>
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
    public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) =>
        throw new InvalidOperationException("not an eject");
}

/// <summary>A behavior that calls <c>next()</c> first, then ejects from its own post-<c>next()</c> logic -
/// proving the eject is recovered at this behavior's own call site, not only when thrown straight from a
/// handler.</summary>
/// <typeparam name="TRequest">The request type, unconstrained since this behavior never inspects it.</typeparam>
/// <typeparam name="TResponse">The response type, constrained to <see cref="IOutcome"/> so
/// <paramref name="response"/> can be ejected via <see cref="Eject.With{TResult}(TResult)"/>.</typeparam>
public class EjectingAfterNextBehavior<TRequest, TResponse>(TResponse response) : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IOutcome
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
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
    public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
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
    public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
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
    public bool TryGetOverride(Type requestType, out IReadOnlyList<Type> behaviorTypes)
    {
        var found = lookup(requestType);
        behaviorTypes = found ?? [];
        return found is not null;
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
        var result = await mediator.Send(new TestQuery(21), CancellationToken.None);

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
        await mediator.Send(new TestQuery(1), CancellationToken.None);

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
        await mediator.Send(new TestQuery(1), CancellationToken.None);

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
        await mediator.Send(new AuditableTestQuery(1), CancellationToken.None);

        // Assert
        Assert.Equal(["kept"], executed);
    }

    [Fact]
    public async Task Send_Query_HandlerEjectsDeepInOwnCallGraph_ReturnsEjectedOutcomeWithoutThrowing()
    {
        // Arrange
        var mediator = CreateMediator<EjectingQueryHandler>(_ => { });

        // Act
        var result = await mediator.Send(new TestQuery(7), CancellationToken.None);

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
        var result = await mediator.Send(new TestQuery(1), CancellationToken.None);

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
        var result = await mediator.Send(new TestQuery(1), CancellationToken.None);

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
        await mediator.Send(new GreetCommand("test"), CancellationToken.None);

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
        var result = await mediator.Send(new GreetCommand("test"), CancellationToken.None);

        // Assert
        Assert.Equal(new Failure("dead end"), result.Value);
    }

    [Fact]
    public async Task Send_VoidCommand_RunsPipelineBehaviors()
    {
        // Arrange
        var invoked = false;
        var mediator = CreateMediator<TestVoidHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestVoidCommand, Outcome>>(
                new TrackingBehavior<TestVoidCommand, Outcome>(() => invoked = true)
            );
        });

        // Act
        await mediator.Send<Outcome>(new TestVoidCommand("test"), CancellationToken.None);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_VoidCommand_BehaviorEjectsBeforeNext_ReturnsEjectedOutcomeWithoutThrowing()
    {
        // Arrange - covers VoidCommandHandlerWrapper's composition site, distinct from the other two wrappers.
        var mediator = CreateMediator<TestVoidHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestVoidCommand, Outcome>>(
                new EjectingBeforeNextBehavior<TestVoidCommand, Outcome>(new Outcome(new Failure("dead end")))
            );
        });

        // Act
        var result = await mediator.Send<Outcome>(new TestVoidCommand("test"), CancellationToken.None);

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
        return Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new TestQuery(1), CancellationToken.None));
    }

    [Fact]
    public async Task Send_VoidCommand_ReturnsSuccess()
    {
        // Arrange
        var mediator = CreateMediator<TestVoidHandler>(_ => { });

        // Act
        var result = await mediator.Send<Outcome>(new TestVoidCommand("test"), CancellationToken.None);

        // Assert
        var isSuccess = result.Value switch
        {
            Success => true,
            Failure => false,
            _ => throw new UnreachableException(),
        };
        Assert.True(isSuccess);
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
        await mediator.Publish(new TestNotification("hello"), CancellationToken.None);

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
            mediator.Publish(new TestNotification("hello"), CancellationToken.None)
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
            mediator.Publish(new TestNotification("hello"), CancellationToken.None)
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
        var first = await mediator.Send(new TestQuery(1), CancellationToken.None);
        var second = await mediator.Send(new TestQuery(2), CancellationToken.None);
        var third = await mediator.Send(new TestQuery(3), CancellationToken.None);

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
        else if (typeof(THandler) == typeof(EjectingQueryHandler))
        {
            services.AddSingleton<IQueryHandler<TestQuery, Outcome<int, Failure>>, EjectingQueryHandler>();
        }
        else if (typeof(THandler) == typeof(GreetCommandHandler))
        {
            services.AddSingleton<ICommandHandler<GreetCommand, Outcome<string, Failure>>, GreetCommandHandler>();
        }
        else if (typeof(THandler) == typeof(TestVoidHandler))
        {
            services.AddSingleton<IVoidCommandHandler<TestVoidCommand, Outcome>, TestVoidHandler>();
        }

        configure(services);
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }
}

public class TestNotificationHandler(Action action) : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken ct)
    {
        action();
        return Task.CompletedTask;
    }
}
