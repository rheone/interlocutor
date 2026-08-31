namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

// CancellationBehavior<TRequest, TResponse> constrains TRequest : ICancellable, so a request that doesn't
// opt in cannot close this generic at all - there is no runtime "skip" branch left to unit test here, matching
// AuthorizationBehaviorTests' treatment of IRequiresPolicy. TResponse is the whole outcome type directly, not
// decomposed into a success-type parameter, for the same open-generic-resolution reason documented on
// AuthorizationBehavior.
public class CancellationBehaviorTests
{
    [Fact]
    public async Task Handle_ReturnsCancelledWhenNextThrowsOperationCanceledExceptionTiedToAmbientToken()
    {
        // Arrange
        var behavior = new CancellationBehavior<CancellableQuery, Outcome<string, Cancelled>>(
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new CancellableQuery("test");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var ct = cts.Token;

        Task<Outcome<string, Cancelled>> Next()
        {
            throw new OperationCanceledException(ct);
        }

        // Act
        var result = await behavior.Handle(request, Next, ct);

        // Assert
        Assert.IsType<Cancelled>(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsResultFromNextWhenNotCancelled()
    {
        // Arrange
        var behavior = new CancellationBehavior<CancellableQuery, Outcome<string, Cancelled>>(
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new CancellableQuery("test");
        static Task<Outcome<string, Cancelled>> Next()
        {
            return Task.FromResult<Outcome<string, Cancelled>>(new Outcome<string, Cancelled>("success"));
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task Handle_PropagatesNonCancellationExceptionUnchanged()
    {
        // Arrange
        var behavior = new CancellationBehavior<CancellableQuery, Outcome<string, Cancelled>>(
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new CancellableQuery("test");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var ct = cts.Token;

        static Task<Outcome<string, Cancelled>> Next()
        {
            throw new InvalidOperationException("boom");
        }

        // Act / Assert - a non-cancellation exception is not converted, even though the ambient token is
        // cancelled.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(request, Next, ct));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task Handle_PropagatesUnrelatedOperationCanceledExceptionUnchanged()
    {
        // Arrange - the OperationCanceledException is raised against a *different*, unrelated
        // CancellationTokenSource than the ambient ct passed to Handle. The ambient ct itself was never
        // signalled, so ct.IsCancellationRequested is false and the exception must propagate unchanged
        // instead of being misreported as the ambient request being cancelled.
        var behavior = new CancellationBehavior<CancellableQuery, Outcome<string, Cancelled>>(
            DefaultOutcomeCaseRegistry.Instance
        );
        var request = new CancellableQuery("test");
        using var unrelatedCts = new CancellationTokenSource();
        await unrelatedCts.CancelAsync();
        var unrelatedToken = unrelatedCts.Token;

        Task<Outcome<string, Cancelled>> Next()
        {
            throw new OperationCanceledException(unrelatedToken);
        }

        // Act / Assert
        var ambientCt = CancellationToken.None;
        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => behavior.Handle(request, Next, ambientCt));
        Assert.Equal(unrelatedToken, ex.CancellationToken);
    }

    [Fact]
    public async Task Handle_WithResponseTypeThatHasNoCancelledCase_ThrowsNamedExceptionOnCancellation()
    {
        // TResponse is an arbitrary IOutcome the caller chose, constructed reflectively from a
        // Cancelled case constructor when cancellation is actually observed. A response shape with no such
        // case (like Outcome<string, NotFound> here) can't express "cancelled" at all, so it must
        // fail loudly and specifically rather than silently succeeding or crashing obscurely.
        // Arrange
        var behavior = new CancellationBehavior<
            MisshapenCancellableQuery,
            Outcome<string, Interlocutor.CommonKernel.ResultTypes.NotFound>
        >(DefaultOutcomeCaseRegistry.Instance);
        var request = new MisshapenCancellableQuery();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var ct = cts.Token;

        Task<Outcome<string, NotFound>> Next()
        {
            throw new OperationCanceledException(ct);
        }

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(request, Next, ct));

        // Assert
        Assert.Contains("Cancelled", ex.Message);
    }

    [Fact]
    public async Task Handle_HandRolledNonUnionResponse_ShortCircuitsWithCancelledViaCustomRegistry()
    {
        // Proves IOutcomeCaseRegistry is a genuine drop-in for hand-rolled response shapes here too, not
        // just for AuthorizationBehavior's Forbidden case.
        // Arrange
        var caseRegistry = new OutcomeCaseRegistryBuilder()
            .RegisterFactory<HandRolledOutcome, Cancelled>(HandRolledOutcome.FromCancelled)
            .Build();

        var behavior = new CancellationBehavior<CancellableHandRolledQuery, HandRolledOutcome>(caseRegistry);
        var request = new CancellableHandRolledQuery();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var ct = cts.Token;

        Task<HandRolledOutcome> Next()
        {
            throw new OperationCanceledException(ct);
        }

        // Act
        var result = await behavior.Handle(request, Next, ct);

        // Assert
        Assert.Equal(typeof(Cancelled), result.CaseType);
        Assert.IsType<Cancelled>(result.CaseValue);
    }

    public record CancellableQuery(string Name) : IQuery<Outcome<string, Cancelled>>, ICancellable;

    /// <summary>A request implementing <see cref="ICancellable"/> whose response has no <see cref="Cancelled"/> case at all.</summary>
    public record MisshapenCancellableQuery
        : IQuery<Outcome<string, Interlocutor.CommonKernel.ResultTypes.NotFound>>,
            ICancellable;

    /// <summary>A request whose response type is a hand-rolled, non-union <see cref="IOutcome"/>.</summary>
    public record CancellableHandRolledQuery : ICancellable;

    /// <summary>A hand-rolled, non-union <see cref="IOutcome"/> implementation - no compiler-generated
    /// case constructor, so it can only participate in <see cref="CancellationBehavior{TRequest, TResponse}"/>
    /// via a custom <see cref="IOutcomeCaseRegistry"/> registration.</summary>
    public sealed class HandRolledOutcome : IOutcome
    {
        private HandRolledOutcome(Type caseType, object? caseValue)
        {
            this.CaseType = caseType;
            this.CaseValue = caseValue;
        }

        public Type CaseType { get; }

        public object? CaseValue { get; }

        public static HandRolledOutcome FromCancelled(Cancelled cancelled) => new(typeof(Cancelled), cancelled);
    }
}
