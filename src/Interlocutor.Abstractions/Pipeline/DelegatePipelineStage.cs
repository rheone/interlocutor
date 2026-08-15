namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// An <see cref="IPipelineStage{TRequest, TResponse}"/> implementation that wraps caller-supplied
/// delegates instead of requiring a dedicated class — the type backing lambda-form stage registration
/// (for example, <c>InterlocutorBuilder.AddStage</c>'s delegate overloads).
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Either delegate may be omitted (<see langword="null"/>), in which case the corresponding hook is a
/// no-op, matching <see cref="PipelineStageBase{TRequest, TResponse}"/>'s default behavior.
/// </remarks>
/// <param name="onBefore">
/// The delegate invoked from <see cref="OnBeforeAsync"/>, or <see langword="null"/> for a no-op
/// (always-continue) before-hook.
/// </param>
/// <param name="onAfter">
/// The delegate invoked from <see cref="OnAfterAsync"/>, or <see langword="null"/> for a no-op after-hook.
/// </param>
public sealed class DelegatePipelineStage<TRequest, TResponse>(
    Func<TRequest, CancellationToken, Task<StageResult<TResponse>>>? onBefore,
    Func<TRequest, TResponse, CancellationToken, Task>? onAfter
) : PipelineStageBase<TRequest, TResponse>
{
    /// <inheritdoc />
    public override Task<StageResult<TResponse>> OnBeforeAsync(TRequest request, CancellationToken ct) =>
        onBefore is null ? base.OnBeforeAsync(request, ct) : onBefore(request, ct);

    /// <inheritdoc />
    public override Task OnAfterAsync(TRequest request, TResponse result, CancellationToken ct) =>
        onAfter is null ? base.OnAfterAsync(request, result, ct) : onAfter(request, result, ct);
}
