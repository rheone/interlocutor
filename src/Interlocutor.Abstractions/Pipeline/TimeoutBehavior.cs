namespace Interlocutor.Abstractions.Pipeline;

using System.Diagnostics;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Pipeline behavior that enforces a time budget by racing the rest of the pipeline against a delay.
/// </summary>
/// <typeparam name="TRequest">
/// The request type. Constrained to <see cref="IHasTimeout"/> so only requests that declare a budget close
/// this generic - a container-neutral, compile-time-checked replacement for a runtime <c>is</c> check.
/// </typeparam>
/// <typeparam name="TResponse">
/// The response type, constrained only to <see cref="IOutcome"/>, mirroring
/// <see cref="AuthorizationBehavior{TRequest, TResponse}"/>'s <c>TResponse</c> shape for the same reason: both
/// type parameters must map 1:1 onto <see cref="IPipelineBehavior{TRequest, TResponse}"/>'s so Microsoft
/// DI's open-generic resolver can construct this behavior.
/// </typeparam>
/// <remarks>
/// <para>
/// <b>This behavior can only report a timeout, not enforce one.</b> <see cref="IPipelineBehavior{TRequest, TResponse}.HandleAsync"/>'s
/// <c>next</c> parameter is a parameterless <c>Func&lt;Task&lt;TResponse&gt;&gt;</c> - there is no way to hand
/// it a linked, shorter-lived token, so there is no way to ask the handler (or any inner behavior) to actually
/// stop. When the budget elapses first, this behavior returns a <see cref="Timeout"/> response to the caller,
/// but the abandoned <c>next()</c> task is not cancelled - it keeps running to completion (or failure) in the
/// background, unobserved. Genuinely aborting in-flight work requires the handler itself to observe <c>ct</c>
/// and enforcing a hard budget requires a mechanism this interface doesn't provide.
/// </para>
/// <para>
/// <b>Ambient cancellation is not a timeout.</b> The delay this behavior races against observes the ambient
/// <c>ct</c> as well as the resolved budget, so it can "win" the race for two different reasons:
/// the budget genuinely elapsed, or <c>ct</c> was cancelled first. Only the former is a
/// <see cref="Timeout"/> - the latter must propagate as <see cref="OperationCanceledException"/> so a
/// <c>CancellationBehavior</c> sitting outside this one in the pipeline (per the canonical Logging → Auditing →
/// Authorization → Validation → Transaction ordering, a cancellation-observing behavior belongs further out
/// than this one) converts it to <c>Cancelled</c> instead of this behavior misreporting it as <see cref="Timeout"/>.
/// This is distinguished by checking whether the delay task itself completed via cancellation
/// (<see cref="Task.IsCanceled"/>) rather than by racing to completion normally.
/// </para>
/// <para>
/// Because <typeparamref name="TRequest"/> is constrained to <see cref="IHasTimeout"/>, both Microsoft DI and
/// Autofac skip this open generic registration for a request type that does not implement the marker - the
/// container never even attempts to construct this behavior for it.
/// </para>
/// </remarks>
public class TimeoutBehavior<TRequest, TResponse>(ITimeoutPolicy timeoutPolicy, IOutcomeCaseRegistry caseRegistry)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IHasTimeout
    where TResponse : IOutcome
{
    /// <summary>
    /// Handles the request, racing it against the resolved time budget and short-circuiting with
    /// <see cref="Timeout"/> if the budget elapses first.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Timeout"/> outcome if the resolved budget elapses first, otherwise the result from the
    /// next behavior or handler.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="ct"/> was cancelled before both the budget elapsed and <c>next()</c> completed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The budget elapsed, but <typeparamref name="TResponse"/> has no <see cref="Timeout"/> case to
    /// construct - pair <see cref="IHasTimeout"/> only with response shapes that include one.
    /// </exception>
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var budget = timeoutPolicy.ResolveBudget(request);
        var stopwatch = Stopwatch.StartNew();

        var nextTask = next();
        var delayTask = Task.Delay(budget, ct);

        var winner = await Task.WhenAny(nextTask, delayTask).ConfigureAwait(false);
        if (winner == nextTask)
        {
            return await nextTask.ConfigureAwait(false);
        }

        // The delay task won the race. It observes `ct` as well as the budget, so it can complete either
        // because the budget genuinely elapsed (RanToCompletion) or because `ct` was cancelled first
        // (Canceled) - only the former is a Timeout. Awaiting a canceled delayTask rethrows the
        // OperationCanceledException it completed with, so cancellation propagates correctly rather than
        // being misreported as a Timeout.
        if (delayTask.IsCanceled)
        {
            await delayTask.ConfigureAwait(false);
        }

        stopwatch.Stop();
        return this.ConstructTimeoutResponse(new Timeout(budget, stopwatch.Elapsed));
    }

    private TResponse ConstructTimeoutResponse(Timeout timeout) =>
        caseRegistry.ConstructOrThrow<TResponse, Timeout>(
            timeout,
            $"TimeoutBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>",
            "timed out a request",
            $"A request implementing {nameof(IHasTimeout)} must use a response type with a {nameof(Timeout)} case"
        );
}
