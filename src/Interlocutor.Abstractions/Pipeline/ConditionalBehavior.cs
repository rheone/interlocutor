namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Wraps an inner <see cref="IPipelineBehavior{TRequest,TResponse}"/>, running it only when
/// <paramref name="shouldRun"/> returns <see langword="true"/> for the dispatched request - otherwise
/// passing straight through to <c>next()</c>, as if <paramref name="inner"/> had never been registered.
/// </summary>
/// <remarks>
/// <para>
/// The building block behind <c>InterlocutorPipelineBuilder.AddConditionalBehavior</c> (in
/// <c>Interlocutor.DependencyInjection.Abstractions</c>), which resolves <paramref name="inner"/> from the
/// container rather than requiring a caller to construct one directly - most consumers reach this type only
/// through that method, not by constructing it themselves. <paramref name="inner"/> is resolved once, when
/// this wrapper itself is constructed (the same as any other registered behavior), not lazily deferred until
/// <paramref name="shouldRun"/> is evaluated against a specific request - see that method's own remarks. It
/// lets a new or risky behavior
/// (a stricter validation rule, a new authorization policy) be turned on for a fraction of traffic and
/// ramped up without a redeploy, instead of the all-or-nothing choice every other <c>Add*Behavior</c> call
/// makes.
/// </para>
/// <para>
/// <paramref name="shouldRun"/> is evaluated once per dispatch, against the request instance only - it has
/// no knowledge of what <paramref name="inner"/> would have done. This makes it an on/off gate, not a
/// shadow/observe-only execution mode (running <paramref name="inner"/> but discarding its effect on the
/// outcome): a behavior this type gates either fully participates in the chain for that dispatch, or is
/// entirely absent from it. Shadow execution has no generic semantics for a behavior that short-circuits or
/// transforms the response, so it isn't offered here.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The request type this behavior participates in.</typeparam>
/// <typeparam name="TResponse">The response type this behavior participates in.</typeparam>
/// <param name="inner">The behavior to run only when <paramref name="shouldRun"/> returns <see langword="true"/>.</param>
/// <param name="shouldRun">
/// Evaluated once per dispatch against the request instance - <see langword="true"/> runs
/// <paramref name="inner"/>, <see langword="false"/> skips straight to <c>next()</c>.
/// </param>
public sealed class ConditionalBehavior<TRequest, TResponse>(
    IPipelineBehavior<TRequest, TResponse> inner,
    Func<TRequest, bool> shouldRun
) : IPipelineBehavior<TRequest, TResponse>
{
    /// <inheritdoc />
    public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) =>
        shouldRun(request) ? inner.HandleAsync(request, next, ct) : next();
}
