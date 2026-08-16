namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

/// <summary>
/// Unit tests for <see cref="RootScopeGuardedMediator"/> in isolation from either container: an
/// <see cref="IMediator"/> double and an <see cref="IRootScopeDetector"/> double are enough to prove every
/// dispatch overload consults the detector before doing anything else, and delegates unchanged when the
/// detector reports a real scope.
/// </summary>
public class RootScopeGuardedMediatorTests
{
    [Fact]
    public async Task Send_Query_RootScope_ThrowsNamingTheQueryType_AndNeverCallsInner()
    {
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var query = new SampleQuery();
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.Send(query, CancellationToken.None)
        );

        Assert.Equal(typeof(SampleQuery), ex.RequestType);
        await inner.DidNotReceive().Send(Arg.Any<IQuery<OperationOutcome<int, Failure>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_Query_RealScope_DelegatesToInner_AndReturnsItsResult()
    {
        var inner = Substitute.For<IMediator>();
        var expected = new OperationOutcome<int, Failure>(42);
        inner.Send(Arg.Any<IQuery<OperationOutcome<int, Failure>>>(), Arg.Any<CancellationToken>()).Returns(expected);
        var detector = StubDetector(isRootScope: false);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var result = await guarded.Send(new SampleQuery(), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Send_ValueCommand_RootScope_ThrowsNamingTheCommandType_AndNeverCallsInner()
    {
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var command = new SampleValueCommand();
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.Send(command, CancellationToken.None)
        );

        Assert.Equal(typeof(SampleValueCommand), ex.RequestType);
        await inner.DidNotReceive().Send(Arg.Any<ICommand<OperationOutcome<int, Failure>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_UnitCommand_RootScope_ThrowsNamingTheCommandType_AndNeverCallsInner()
    {
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var command = new SampleUnitCommand();
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.Send<OperationOutcome>(command, CancellationToken.None)
        );

        Assert.Equal(typeof(SampleUnitCommand), ex.RequestType);
        await inner.DidNotReceive().Send<OperationOutcome>(Arg.Any<ICommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_RuntimeTypedObject_RootScope_ThrowsNamingTheRuntimeType_AndNeverCallsInner()
    {
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        object request = new SampleQuery();
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.Send(request, CancellationToken.None)
        );

        Assert.Equal(typeof(SampleQuery), ex.RequestType);
        await inner.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_RuntimeTypedObject_NullRequest_ThrowsArgumentNullException_BeforeConsultingDetector()
    {
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        await Assert.ThrowsAsync<ArgumentNullException>(() => guarded.Send(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Publish_RootScope_ThrowsNamingTheNotificationType_AndNeverCallsInner()
    {
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var notification = new SampleNotification();
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.Publish(notification, CancellationToken.None)
        );

        Assert.Equal(typeof(SampleNotification), ex.RequestType);
        await inner.DidNotReceive().Publish(Arg.Any<SampleNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_RealScope_DelegatesToInner()
    {
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: false);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var notification = new SampleNotification();
        await guarded.Publish(notification, CancellationToken.None);

        await inner.Received(1).Publish(notification, Arg.Any<CancellationToken>());
    }

    private static IRootScopeDetector StubDetector(bool isRootScope)
    {
        var detector = Substitute.For<IRootScopeDetector>();
        detector.IsRootScope.Returns(isRootScope);
        return detector;
    }

    private sealed record SampleQuery : IQuery<OperationOutcome<int, Failure>>;

    private sealed record SampleValueCommand : ICommand<OperationOutcome<int, Failure>>;

    private sealed record SampleUnitCommand : ICommand;

    private sealed record SampleNotification : INotification;
}
