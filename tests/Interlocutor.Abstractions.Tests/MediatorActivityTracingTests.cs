namespace Interlocutor.Abstractions.Tests;

using System.Collections.Concurrent;
using System.Diagnostics;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>Covers ticket 15's acceptance criteria for <see cref="Mediator"/>'s built-in activity tracing.</summary>
/// <remarks>
/// <c>MediatorActivitySource.Source</c> is a single process-wide <see cref="ActivitySource"/>, so an
/// <see cref="ActivityListener"/> attached in one test observes every mediator dispatch happening anywhere in the
/// process, including other tests running concurrently. Each test below therefore dispatches request types unique
/// to itself and filters the captured activities down to the ones tagged with that exact request type, rather than
/// assuming the listener saw only its own dispatch.
/// </remarks>
public class MediatorActivityTracingTests
{
    [Fact]
    public async Task Send_Query_WithListener_StartsActivityTaggedWithRequestAndHandlerType()
    {
        // Arrange
        var captured = new ConcurrentBag<Activity>();
        using var listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        var mediator = CreateMediator();

        // Act
        await mediator.SendAsync(new TracingSuccessQuery(21), CancellationToken.None);

        // Assert
        var activity = SingleFor<TracingSuccessQuery>(captured);
        Assert.Equal(typeof(TracingSuccessQueryHandler).FullName, activity.GetTagItem(MediatorActivitySource.HandlerTypeTag));

        // The classifier (the same one LoggingBehavior uses) labels an int-payload success by its runtime type,
        // not literally "Success" - that label is reserved for the Success marker type itself (covered below).
        Assert.Equal("Int32", activity.GetTagItem(MediatorActivitySource.OutcomeTag));
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public async Task Send_VoidCommand_WithSuccessOutcome_TagsOutcomeAsSuccess()
    {
        // Arrange
        var captured = new ConcurrentBag<Activity>();
        using var listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddSingleton<IVoidCommandHandler<TracingSuccessCommand, Outcome>, TracingSuccessCommandHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync<Outcome>(new TracingSuccessCommand(), CancellationToken.None);

        // Assert
        var activity = SingleFor<TracingSuccessCommand>(captured);
        Assert.Equal(typeof(TracingSuccessCommandHandler).FullName, activity.GetTagItem(MediatorActivitySource.HandlerTypeTag));
        Assert.Equal("Success", activity.GetTagItem(MediatorActivitySource.OutcomeTag));
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public async Task Send_Query_WithFailureOutcome_TagsOutcomeWithoutErrorStatus()
    {
        // Arrange
        var captured = new ConcurrentBag<Activity>();
        using var listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        var mediator = CreateMediator();

        // Act
        await mediator.SendAsync(new TracingFailingQuery(), CancellationToken.None);

        // Assert
        var activity = SingleFor<TracingFailingQuery>(captured);
        Assert.Equal("Failure", activity.GetTagItem(MediatorActivitySource.OutcomeTag));
        Assert.NotEqual(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public async Task Send_Query_WhenHandlerThrows_RecordsErrorStatusAndStillStopsActivity()
    {
        // Arrange
        var captured = new ConcurrentBag<Activity>();
        using var listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        var mediator = CreateMediator();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new TracingThrowingQuery(), CancellationToken.None)
        );

        // Assert
        var activity = SingleFor<TracingThrowingQuery>(captured);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);

        // ActivityStopped only fires once the activity has been stopped/disposed, so its being observed here at
        // all proves it was stopped even though the dispatch threw.
        Assert.True(activity.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task Send_NestedDispatch_ChildActivityParentsToOuterActivity()
    {
        // Arrange
        var captured = new ConcurrentBag<Activity>();
        using var listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TracingOuterQuery, Outcome<int, Failure>>, TracingOuterQueryHandler>();
        services.AddSingleton<IQueryHandler<TracingInnerQuery, Outcome<int, Failure>>, TracingInnerQueryHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TracingOuterQuery(), CancellationToken.None);

        // Assert
        var outer = SingleFor<TracingOuterQuery>(captured);
        var inner = SingleFor<TracingInnerQuery>(captured);
        Assert.Equal(outer.Id, inner.ParentId);
    }

    [Fact]
    public async Task Publish_WithListener_StartsActivityTaggedWithRequestAndHandlerTypes()
    {
        // Arrange
        var captured = new ConcurrentBag<Activity>();
        using var listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TracingNotification>>(new TracingNotificationHandler(() => { }));
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.PublishAsync(new TracingNotification("hi"), CancellationToken.None);

        // Assert
        var activity = SingleFor<TracingNotification>(captured);
        Assert.Contains(
            typeof(TracingNotificationHandler).FullName!,
            (string?)activity.GetTagItem(MediatorActivitySource.HandlerTypeTag)
        );
    }

    [Fact]
    public async Task Send_Query_WithNoListener_ActivityCurrentStaysNull()
    {
        // Arrange - No ActivityListener attached to MediatorActivitySource.Name, the mechanism's built-in cheap path.
        var mediator = CreateMediator();

        // Act
        await mediator.SendAsync(new TracingSuccessQuery(21), CancellationToken.None);

        // Assert
        Assert.Null(Activity.Current);
    }

    [Fact]
    public async Task Send_Query_WithNoListener_HasListenersReturnsFalse()
    {
        // Arrange
        var mediator = CreateMediator();

        // Act
        await mediator.SendAsync(new TracingSuccessQuery(21), CancellationToken.None);

        // Assert - Nothing in this test attached an ActivityListener to MediatorActivitySource.Name, so the
        // source itself reports no listeners - the condition StartActivity checks internally to short-circuit
        // before any allocation or string formatting happens.
        using var probe = new ActivitySource(MediatorActivitySource.Name);
        Assert.False(probe.HasListeners());
    }

    [Fact]
    public async Task Send_Query_WithLoggingBehaviorAlsoRegistered_BothMechanismsObserveTheDispatchIndependently()
    {
        // MediatorActivitySource-based tracing lives in Mediator itself; LoggingBehavior is a separate, opt-in
        // IPipelineBehavior with its own ILogger-based mechanism. Neither shares state with the other, so
        // registering both must not suppress or alter either one's output for the same dispatch.
        // Arrange
        var captured = new ConcurrentBag<Activity>();
        using var listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        var logger = new RecordingLogger<LoggingBehavior<TracingSuccessQuery, Outcome<int, Failure>>>();

        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TracingSuccessQuery, Outcome<int, Failure>>, TracingSuccessQueryHandler>();
        services.AddSingleton<ILogger<LoggingBehavior<TracingSuccessQuery, Outcome<int, Failure>>>>(logger);
        services.AddSingleton<
            IPipelineBehavior<TracingSuccessQuery, Outcome<int, Failure>>,
            LoggingBehavior<TracingSuccessQuery, Outcome<int, Failure>>
        >();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TracingSuccessQuery(21), CancellationToken.None);

        // Assert - The Activity-based tracing still fired, tagged as normal...
        var activity = SingleFor<TracingSuccessQuery>(captured);
        Assert.Equal(typeof(TracingSuccessQueryHandler).FullName, activity.GetTagItem(MediatorActivitySource.HandlerTypeTag));

        // ...and LoggingBehavior independently logged the same dispatch, unaffected by the activity's presence.
        Assert.True(logger.LoggedAnything);
    }

    private static Activity SingleFor<TRequest>(ConcurrentBag<Activity> captured) =>
        Assert.Single(captured, a => (string?)a.GetTagItem(MediatorActivitySource.RequestTypeTag) == typeof(TRequest).FullName);

    private static ActivityListener CreateListener(ConcurrentBag<Activity> captured) =>
        new()
        {
            ShouldListenTo = source => source.Name == MediatorActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => captured.Add(activity),
        };

    private static IMediator CreateMediator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<TracingSuccessQuery, Outcome<int, Failure>>, TracingSuccessQueryHandler>();
        services.AddSingleton<IQueryHandler<TracingFailingQuery, Outcome<int, Failure>>, TracingFailingQueryHandler>();
        services.AddSingleton<IQueryHandler<TracingThrowingQuery, Outcome<int, Failure>>, TracingThrowingQueryHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }
}

public record TracingSuccessQuery(int Value) : IQuery<Outcome<int, Failure>>;

public class TracingSuccessQueryHandler : IQueryHandler<TracingSuccessQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> HandleAsync(TracingSuccessQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(query.Value * 2));
}

public record TracingFailingQuery : IQuery<Outcome<int, Failure>>;

public class TracingFailingQueryHandler : IQueryHandler<TracingFailingQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> HandleAsync(TracingFailingQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(new Failure("boom")));
}

public record TracingThrowingQuery : IQuery<Outcome<int, Failure>>;

public class TracingThrowingQueryHandler : IQueryHandler<TracingThrowingQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> HandleAsync(TracingThrowingQuery query, CancellationToken ct) =>
        throw new InvalidOperationException("handler bug");
}

public record TracingOuterQuery : IQuery<Outcome<int, Failure>>;

public class TracingOuterQueryHandler(IMediator mediator) : IQueryHandler<TracingOuterQuery, Outcome<int, Failure>>
{
    public async Task<Outcome<int, Failure>> HandleAsync(TracingOuterQuery query, CancellationToken ct) =>
        await mediator.SendAsync(new TracingInnerQuery(), ct).ConfigureAwait(false);
}

public record TracingInnerQuery : IQuery<Outcome<int, Failure>>;

public class TracingInnerQueryHandler : IQueryHandler<TracingInnerQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> HandleAsync(TracingInnerQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(1));
}

public record TracingSuccessCommand : ICommand;

// IVoidCommandHandler<,> has no HandleAsync member (known gap - ticket 01 never added one), so this fake
// stays on Handle as-is.
public class TracingSuccessCommandHandler : IVoidCommandHandler<TracingSuccessCommand, Outcome>
{
    public Task<Outcome> Handle(TracingSuccessCommand command, CancellationToken ct) =>
        Task.FromResult(new Outcome(default(Success)));
}

public record TracingNotification(string Message) : INotification;

public class TracingNotificationHandler(Action action) : INotificationHandler<TracingNotification>
{
    public Task HandleAsync(TracingNotification notification, CancellationToken ct)
    {
        action();
        return Task.CompletedTask;
    }
}

/// <summary>Minimal <see cref="ILogger{TCategoryName}"/> stub that only records whether it was ever called.</summary>
/// <typeparam name="T">The category type, matching <see cref="ILogger{TCategoryName}"/>'s own type parameter.</typeparam>
public sealed class RecordingLogger<T> : ILogger<T>
{
    public bool LoggedAnything { get; private set; }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) => this.LoggedAnything = true;
}
