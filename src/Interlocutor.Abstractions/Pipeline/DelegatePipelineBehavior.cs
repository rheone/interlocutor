namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// An <see cref="IPipelineBehavior{TRequest, TResponse}"/> implementation that wraps caller-supplied
/// enter/exit delegates instead of requiring a dedicated <see cref="PipelineBehaviorBase{TRequest, TResponse}"/>
/// subclass - the type backing lambda-form behavior registration, mirroring
/// <see cref="DelegatePipelineStage{TRequest, TResponse}"/>'s relationship to <see cref="IPipelineStage{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Either delegate may be omitted (<see langword="null"/>), in which case the corresponding hook is a
/// no-op, matching <see cref="PipelineBehaviorBase{TRequest, TResponse}"/>'s default behavior.
/// </remarks>
/// <param name="onEnter">
/// The delegate invoked from <see cref="OnEnterAsync"/>, or <see langword="null"/> for a no-op
/// (always-continue) enter-hook.
/// </param>
/// <param name="onExit">
/// The delegate invoked from <see cref="OnExitAsync"/>, or <see langword="null"/> for a no-op (pass-through)
/// exit-hook.
/// </param>
public sealed class DelegatePipelineBehavior<TRequest, TResponse>(
    Func<TRequest, CancellationToken, Task<StageResult<TResponse>>>? onEnter,
    Func<TRequest, TResponse, CancellationToken, Task<TResponse>>? onExit
) : PipelineBehaviorBase<TRequest, TResponse>
{
    /// <inheritdoc />
    /// <remarks>
    /// <para>Forwards to the <c>onEnter</c> delegate supplied at construction when one was given; otherwise defers to the base no-op.</para>
    /// </remarks>
    protected override Task<StageResult<TResponse>> OnEnterAsync(TRequest request, CancellationToken ct) =>
        onEnter is null ? base.OnEnterAsync(request, ct) : onEnter(request, ct);

    /// <inheritdoc />
    /// <remarks>
    /// <para>Forwards to the <c>onExit</c> delegate supplied at construction when one was given; otherwise defers to the base pass-through.</para>
    /// </remarks>
    protected override Task<TResponse> OnExitAsync(TRequest request, TResponse result, CancellationToken ct) =>
        onExit is null ? base.OnExitAsync(request, result, ct) : onExit(request, result, ct);
}
