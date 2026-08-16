namespace Interlocutor.Abstractions.Pipeline;

/// <summary>
/// The outcome of a pipeline stage's before-hook: either "continue" (the handler and any remaining
/// before-hooks proceed normally) or "short-circuit" (the handler and any remaining before-hooks are
/// skipped, and <see cref="Response"/> becomes the pipeline's result).
/// </summary>
/// <typeparam name="TResponse">The response type of the request/response pair the stage participates in.</typeparam>
/// <remarks>
/// <para>
/// A plain nullable <typeparamref name="TResponse"/> cannot express this decision: Interlocutor's outcome
/// types are value-type unions, and a "success" case is often a field-less struct (see
/// <c>Interlocutor.CommonKernel.ResultTypes.Success</c>), which is indistinguishable from
/// <see langword="default"/> by value alone. <see cref="StageResult{TResponse}"/> carries the decision as
/// an explicit flag instead of overloading the response value's bit pattern.
/// </para>
/// <para>
/// <see langword="default"/>(<see cref="StageResult{TResponse}"/>) has <see cref="ShortCircuited"/> equal
/// to <see langword="false"/> - i.e. "continue" - consistent with the library's convention that default
/// values are safe/inert. <see cref="Response"/> is meaningful only when <see cref="ShortCircuited"/> is
/// <see langword="true"/>; a stage that continues should not read it.
/// </para>
/// </remarks>
public readonly struct StageResult<TResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageResult{TResponse}"/> struct.
    /// </summary>
    /// <param name="shortCircuited">Whether the pipeline should stop and return <paramref name="response"/>.</param>
    /// <param name="response">
    /// The response to return when <paramref name="shortCircuited"/> is <see langword="true"/>. Ignored otherwise.
    /// </param>
    private StageResult(bool shortCircuited, TResponse response)
    {
        this.ShortCircuited = shortCircuited;
        this.Response = response;
    }

    /// <summary>Gets a value indicating whether the before-hook has short-circuited the pipeline.</summary>
    /// <value>
    /// <see langword="true"/> if the handler and any remaining before-hooks should be skipped and
    /// <see cref="Response"/> returned as the pipeline's result; otherwise, <see langword="false"/>.
    /// </value>
    public bool ShortCircuited { get; }

    /// <summary>Gets the response to short-circuit with. Meaningful only when <see cref="ShortCircuited"/> is <see langword="true"/>.</summary>
    /// <value>The short-circuit response, or <see langword="default"/> when not short-circuiting.</value>
    public TResponse Response { get; }

    /// <summary>Gets a result that lets the request continue through the pipeline unchanged.</summary>
    /// <value>A <see cref="StageResult{TResponse}"/> with <see cref="ShortCircuited"/> set to <see langword="false"/>.</value>
    public static StageResult<TResponse> Continue { get; } = default;

    /// <summary>Creates a result that stops the pipeline and returns <paramref name="response"/> immediately.</summary>
    /// <param name="response">The response the pipeline should produce without running the handler.</param>
    /// <returns>A <see cref="StageResult{TResponse}"/> with <see cref="ShortCircuited"/> set to <see langword="true"/>.</returns>
    public static StageResult<TResponse> ShortCircuit(TResponse response) => new(shortCircuited: true, response);
}
