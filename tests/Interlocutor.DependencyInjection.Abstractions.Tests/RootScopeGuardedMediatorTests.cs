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
        // Arrange
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var query = new SampleQuery();

        // Act
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.SendAsync(query, CancellationToken.None)
        );

        // Assert
        Assert.Equal(typeof(SampleQuery), ex.RequestType);
        await inner.DidNotReceive().SendAsync(Arg.Any<IQuery<Outcome<int, Failure>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_Query_RealScope_DelegatesToInner_AndReturnsItsResult()
    {
        // Arrange
        var inner = Substitute.For<IMediator>();
        var expected = new Outcome<int, Failure>(42);
        inner.SendAsync(Arg.Any<IQuery<Outcome<int, Failure>>>(), Arg.Any<CancellationToken>()).Returns(expected);
        var detector = StubDetector(isRootScope: false);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        // Act
        var result = await guarded.SendAsync(new SampleQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Send_ValueCommand_RootScope_ThrowsNamingTheCommandType_AndNeverCallsInner()
    {
        // Arrange
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var command = new SampleValueCommand();

        // Act
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.SendAsync(command, CancellationToken.None)
        );

        // Assert
        Assert.Equal(typeof(SampleValueCommand), ex.RequestType);
        await inner.DidNotReceive().SendAsync(Arg.Any<ICommand<Outcome<int, Failure>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_UnitCommand_RootScope_ThrowsNamingTheCommandType_AndNeverCallsInner()
    {
        // Arrange
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var command = new SampleUnitCommand();

        // Act
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.SendAsync<Outcome>(command, CancellationToken.None)
        );

        // Assert
        Assert.Equal(typeof(SampleUnitCommand), ex.RequestType);
        await inner.DidNotReceive().SendAsync<Outcome>(Arg.Any<ICommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_RuntimeTypedObject_RootScope_ThrowsNamingTheRuntimeType_AndNeverCallsInner()
    {
        // Arrange
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        object request = new SampleQuery();

        // Act
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.SendAsync(request, CancellationToken.None)
        );

        // Assert
        Assert.Equal(typeof(SampleQuery), ex.RequestType);
        await inner.DidNotReceive().SendAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_RuntimeTypedObject_NullRequest_ThrowsArgumentNullException_BeforeConsultingDetector()
    {
        // Arrange
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => guarded.SendAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Publish_RootScope_ThrowsNamingTheNotificationType_AndNeverCallsInner()
    {
        // Arrange
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: true);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var notification = new SampleNotification();

        // Act
        var ex = await Assert.ThrowsAsync<MediatorResolvedFromRootScopeException>(() =>
            guarded.PublishAsync(notification, CancellationToken.None)
        );

        // Assert
        Assert.Equal(typeof(SampleNotification), ex.RequestType);
        await inner.DidNotReceive().PublishAsync(Arg.Any<SampleNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_RealScope_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IMediator>();
        var detector = StubDetector(isRootScope: false);
        var guarded = new RootScopeGuardedMediator(inner, detector);

        var notification = new SampleNotification();

        // Act
        await guarded.PublishAsync(notification, CancellationToken.None);

        // Assert
        await inner.Received(1).PublishAsync(notification, Arg.Any<CancellationToken>());
    }

    private static IRootScopeDetector StubDetector(bool isRootScope)
    {
        var detector = Substitute.For<IRootScopeDetector>();
        detector.IsRootScope.Returns(isRootScope);
        return detector;
    }

    private sealed record SampleQuery : IQuery<Outcome<int, Failure>>;

    private sealed record SampleValueCommand : ICommand<Outcome<int, Failure>>;

    private sealed record SampleUnitCommand : ICommand;

    private sealed record SampleNotification : INotification;
}
