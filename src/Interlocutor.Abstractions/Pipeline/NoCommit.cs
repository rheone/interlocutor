namespace Interlocutor.Abstractions.Pipeline;

using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Explicit, call-site "don't commit" signal a handler pairs with one specific returned or ejected response
/// value, completely independent of what case that value carries.
/// </summary>
/// <remarks>
/// <para>
/// No response case type - <c>Success</c>, <c>Conflict</c>, <c>ValidationError</c>, or any other - carries an
/// inherent transactional meaning. A pipeline behavior that owns a commit-versus-rollback (or analogous)
/// decision for the current dispatch - for example <c>Interlocutor.Persistence.Abstractions.CommitGateDriver</c> -
/// must never infer that decision by inspecting the response's shape; doing so reintroduces the exact
/// "framework asserts a universal judgment" problem this type exists to avoid. A handler that wants to
/// override the default outcome for that decision calls <see cref="With{TResponse}(TResponse)"/> or
/// <see cref="AndEject{TResponse}(TResponse)"/> instead, immediately alongside the value it has already
/// decided to return or eject.
/// </para>
/// <para>
/// <b>Why a mutable box, not a plain ambient flag.</b> A flag simply reassigned on an
/// <see cref="AsyncLocal{T}"/> does not work here: a mutation made deep inside a nested <see langword="await"/>
/// call is not visible to a caller resuming after that call returns - each async method's own continuation
/// restores the execution context it captured at its own await point, discarding whatever an inner call
/// reassigned in the meantime. Mutating a field on a shared <see cref="StrongBox{T}"/> instance sidesteps this
/// entirely: <see cref="BeginDispatch"/> creates that instance once per dispatch and hands the owning behavior
/// its own direct reference before flowing the box forward (which <em>does</em> propagate reliably) to
/// whatever the dispatch calls next; any nested call to <see cref="With{TResponse}(TResponse)"/> or
/// <see cref="AndEject{TResponse}(TResponse)"/>, however many <see langword="await"/> layers deep, mutates
/// that same instance directly - ordinary object mutation, not context propagation - so the owning behavior's
/// own already-held reference reflects it immediately, regardless of how the call graph in between unwound.
/// </para>
/// </remarks>
public static class NoCommit
{
    private static readonly AsyncLocal<StrongBox<bool>?> ActiveSignal = new();

    /// <summary>
    /// Begins a fresh no-commit signal for the current dispatch, for exactly one caller - the pipeline
    /// behavior that owns a commit-versus-rollback (or analogous) decision - to call once, immediately before
    /// invoking its own next pipeline delegate.
    /// </summary>
    /// <returns>
    /// The mutable flag this dispatch's calls to <see cref="With{TResponse}(TResponse)"/>/
    /// <see cref="AndEject{TResponse}(TResponse)"/>, however deep in the call graph, set - read this instance's
    /// own <see cref="StrongBox{T}.Value"/> after the next delegate returns to learn whether either was called.
    /// </returns>
    /// <remarks>
    /// Calling this again - for example for a nested, recursive mediator dispatch started from within the
    /// current one - begins an independent, correctly isolated signal for that inner dispatch: the outer
    /// caller's own previously-returned box is unaffected once the inner dispatch's owning behavior returns,
    /// the same way a locally-scoped variable would be.
    /// </remarks>
    public static StrongBox<bool> BeginDispatch()
    {
        var flag = new StrongBox<bool>(false);
        ActiveSignal.Value = flag;
        return flag;
    }

    /// <summary>
    /// Marks the current dispatch to skip whatever default completion action a pipeline behavior further out
    /// in the chain would otherwise take, then returns <paramref name="response"/> unchanged - for an ordinary
    /// <see langword="return"/> that still wants to signal no-commit.
    /// </summary>
    /// <typeparam name="TResponse">The <see cref="IUnion"/> type being returned.</typeparam>
    /// <param name="response">The response to return, untouched by this call.</param>
    /// <returns><paramref name="response"/>, unchanged.</returns>
    /// <remarks>
    /// A no-op signal (still returns <paramref name="response"/> normally) if no dispatch called
    /// <see cref="BeginDispatch"/> - for example, no persistence package's commit-gate behavior is registered
    /// for this request at all.
    /// </remarks>
    public static TResponse With<TResponse>(TResponse response)
        where TResponse : IUnion
    {
        if (ActiveSignal.Value is { } flag)
        {
            flag.Value = true;
        }

        return response;
    }

    /// <summary>
    /// Marks the current dispatch to skip whatever default completion action a pipeline behavior further out
    /// in the chain would otherwise take, then ejects with <paramref name="response"/> - for the short-circuit
    /// path, several calls deep in a handler's own logic. See <see cref="Eject.With{TResponse}(TResponse)"/>
    /// for the unwind contract this defers to.
    /// </summary>
    /// <typeparam name="TResponse">The <see cref="IUnion"/> type the ejecting dispatch returns.</typeparam>
    /// <param name="response">The outcome the dispatch should end with.</param>
    /// <returns>Never returns - always throws, exactly like <see cref="Eject.With{TResponse}(TResponse)"/>.</returns>
    /// <exception cref="PipelineEjectException{TResponse}">Always thrown - see <see cref="Eject.With{TResponse}(TResponse)"/>.</exception>
    public static TResponse AndEject<TResponse>(TResponse response)
        where TResponse : IUnion
    {
        if (ActiveSignal.Value is { } flag)
        {
            flag.Value = true;
        }

        return Eject.With(response);
    }
}
