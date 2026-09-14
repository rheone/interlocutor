namespace Interlocutor.Abstractions.Pipeline;

using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Internal control-flow signal thrown by <see cref="Eject.With{TResponse}(TResponse)"/> to force a pipeline
/// dispatch to end immediately with a developer-chosen <typeparamref name="TResponse"/>, from anywhere inside
/// a handler's or pipeline behavior's own call graph.
/// </summary>
/// <typeparam name="TResponse">The <see cref="IUnion"/> type the ejecting dispatch returns.</typeparam>
/// <remarks>
/// <para>
/// <b>Not related to <see cref="CancellationBehavior{TRequest, TResponse}"/>/<see cref="Cancelled"/>.</b> This
/// type has nothing to do with cooperative cancellation of an ambient <see cref="System.Threading.CancellationToken"/>.
/// It exists for the opposite situation: a developer, running normally with no cancellation involved, decides
/// mid-flight that the operation is at a dead end and chooses to stop - picking whichever legal outcome case
/// applies - rather than letting the pipeline or handler continue.
/// </para>
/// <para>
/// <b>This is an internal signal, never an expected-outcome exception.</b> Per this library's own convention
/// (handlers return <see cref="IUnion"/> outcomes for expected failures; exceptions are reserved for
/// bugs or infrastructure faults), this type must never be observed by a caller of
/// <c>Interlocutor.Abstractions.Core.IMediator</c>. Every place that composes a pipeline behavior chain around
/// a handler call (<c>CommandHandlerWrapper</c>, <c>BareCommandHandlerWrapper</c>, <c>QueryHandlerWrapper</c>,
/// and the source-generator's emitted dispatch wrappers) catches this exception immediately at the call site
/// that invoked the throwing <c>Handle</c> method, converting it back into an ordinary <see cref="Response"/>
/// value one frame at a time - so every enclosing behavior's own post-<c>next()</c> logic (auditing, the
/// transaction commit/rollback gate, logging) runs exactly as if the ejecting code had returned
/// <see cref="Response"/> directly, never observing a thrown exception at all.
/// </para>
/// <para>
/// Construct this only through <see cref="Eject.With{TResponse}(TResponse)"/> - never directly.
/// </para>
/// </remarks>
// RCS1194 wants the standard parameterless/message/message+innerException/serialization constructor set. This
// exception's Response is load-bearing - the entire point of catching it is to recover the outcome it carries
// - so a parameterless or message-only constructor would produce a useless signal missing the one thing every
// catch site needs. Suppressed here rather than adding constructors that would only invite misuse, mirroring
// HandlerNotRegisteredException's identical rationale.
#pragma warning disable RCS1194
public sealed class PipelineEjectException<TResponse> : Exception
    where TResponse : IUnion
{
    /// <summary>Initializes a new instance of the <see cref="PipelineEjectException{TResponse}"/> class.</summary>
    /// <param name="response">The outcome the ejecting dispatch should return.</param>
    internal PipelineEjectException(TResponse response)
        : base(
            $"Ejected with an outcome of type '{typeof(TResponse).Name}'. This is an internal pipeline "
                + "control-flow signal produced by Eject.With and must never be observed outside a mediator "
                + "dispatch - if you are seeing this message, a catch site that should have converted it back "
                + "into an ordinary response is missing."
        )
    {
        this.Response = response;
    }

    /// <summary>Gets the outcome the ejecting dispatch should return.</summary>
    /// <value>The outcome the ejecting dispatch should return.</value>
    public TResponse Response { get; }
}
#pragma warning restore RCS1194
