namespace Interlocutor.AspNetCore;

/// <summary>
/// Thrown by the reflective, runtime-typed <c>ToActionResult</c>/<c>ToMinimalApiResult</c> overloads when a
/// response's active case can be neither classified as success nor recognized as one of the built-in failure
/// case types/interfaces <see cref="DefaultHttpOutcomeStatusMapper"/> knows - so there is no principled way to
/// pick an HTTP status code for it.
/// </summary>
/// <remarks>
/// <para>
/// An HTTP response must carry <em>some</em> status code, so silently guessing one (the fail-closed policy the
/// retired outcome classifier applied) is worse than refusing outright: a guess that happens to be wrong looks
/// like a working endpoint until the day it matters. This is the one place in the mapping pipeline where
/// "can't decide" is itself the caller's problem to fix, rather than a case this library can shrug off with a
/// generic default - unlike <see cref="IOutcomeStatusMapper"/>, which always has a status code to fall back to
/// for a failure value it doesn't recognize (<see cref="DefaultHttpOutcomeStatusMapper"/>'s
/// <c>MapUnrecognized</c> maps it to 500), this exception fires before the mapper is even reached, when it is
/// not yet known whether the active case is a failure at all.
/// </para>
/// <para>
/// Both the reflective catch-all overloads and the dedicated, per-arity overloads (source-generated for the
/// built-in <c>Outcome</c>/<c>Outcome&lt;...&gt;</c> arities) can throw this, though for different reasons. The
/// catch-all has no compile-time case-position information at all, so it cannot even tell whether the active
/// case represents success or failure once it is neither the bare
/// <see cref="Interlocutor.CommonKernel.ResultTypes.Success"/> marker nor a recognized failure shape. The
/// dedicated overloads always know that much - their own <c>Match</c> tells them definitively which case is
/// active - but a case value that is failure-shaped despite occupying the success slot, or a declared-failure-slot
/// value that matches none of the recognized failure shapes with no explicit mapper supplied to take
/// responsibility for it, is still not a case either path can assign a principled HTTP status to without
/// guessing. Passing an explicit <see cref="Interlocutor.AspNetCore.IOutcomeStatusMapper"/> to a dedicated
/// overload avoids the latter: such a mapper is always trusted for whatever type it is handed.
/// </para>
/// </remarks>
// RCS1194 wants the standard parameterless/message/message+innerException/serialization constructor set. This
// exception's ResponseType/CaseType are load-bearing - the whole point of catching it is to read which
// response shape's active case could not be mapped - so a parameterless or message-only constructor would
// produce a useless exception missing the one thing callers need. Suppressed here rather than adding
// constructors that would only confuse callers into using them. See OutcomeCaseNotSupportedException's
// identical suppression.
#pragma warning disable RCS1194
public sealed class OutcomeCaseNotMappableException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="OutcomeCaseNotMappableException"/> class.</summary>
    /// <param name="responseType">The <see cref="Interlocutor.CommonKernel.ResultTypes.IOutcome"/> type whose active case could not be mapped.</param>
    /// <param name="caseType">The active case's runtime type, which is neither the bare success marker nor a recognized failure shape.</param>
    /// <param name="message">A message describing why the case could not be mapped.</param>
    public OutcomeCaseNotMappableException(Type responseType, Type caseType, string message)
        : base(message)
    {
        this.ResponseType = responseType;
        this.CaseType = caseType;
    }

    /// <summary>Gets the <see cref="Interlocutor.CommonKernel.ResultTypes.IOutcome"/> type whose active case could not be mapped.</summary>
    /// <value>The <see cref="Interlocutor.CommonKernel.ResultTypes.IOutcome"/> type whose active case could not be mapped.</value>
    public Type ResponseType { get; }

    /// <summary>Gets the active case's runtime type, which is neither the bare success marker nor a recognized failure shape.</summary>
    /// <value>The active case's runtime type, which is neither the bare success marker nor a recognized failure shape.</value>
    public Type CaseType { get; }
}
#pragma warning restore RCS1194
