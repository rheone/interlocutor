namespace Interlocutor.Abstractions.Pipeline;

using Interlocutor.Abstractions.Pipeline.Cases;

/// <summary>
/// Internal control-flow signal (ADR-0025) thrown by <see cref="Eject.WithCase{TCase}(TCase)"/> to force a
/// pipeline dispatch to end immediately with a specific outcome case, from anywhere inside a handler's or
/// pipeline behavior's own call graph, without the throwing code ever knowing the dispatch's concrete response
/// type.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not generic over the response type, unlike <see cref="PipelineEjectException{TResponse}"/>.</b> This
/// type carries only the boxed case value and its runtime <see cref="Type"/> - mirroring
/// <see cref="System.Runtime.CompilerServices.IUnion.Value"/>'s own <c>object?</c> shape - rather than a fully
/// constructed response. That is the entire point: a behavior that ejects a case this way needs no
/// <see cref="IOutcomeCaseRegistry"/> dependency and no <c>TResponse</c> generic parameter of its own, because
/// it never constructs the response itself.
/// </para>
/// <para>
/// <b>Not actually generic over the case type either, despite the ticket's working name for this type.</b> A
/// <c>PipelineEjectCaseException&lt;TCase&gt;</c> shape was considered first, but a catch site composing the
/// pipeline chain (<c>PipelineChainExecutor</c>, shared by <c>QueryHandlerWrapper</c>,
/// <c>CommandHandlerWrapper</c>, and <c>BareCommandHandlerWrapper</c>) is generic only over
/// <c>TRequest</c>/<c>TResponse</c> - it cannot also declare a compile-time <c>TCase</c> to catch, since
/// different behaviors eject different case types through the very same catch clause. A single non-generic
/// exception type, carrying the case value boxed as <see cref="object"/> alongside its runtime
/// <see cref="Type"/>, is what a single shared catch clause can actually catch regardless of which case type
/// was ejected.
/// </para>
/// <para>
/// <b>Construction is deferred to the one place that already knows <c>TResponse</c> concretely.</b> Per this
/// library's own convention (handlers return <see cref="System.Runtime.CompilerServices.IUnion"/> outcomes for
/// expected failures; exceptions are reserved for bugs or infrastructure faults), this type must never be
/// observed by a caller of
/// <c>Interlocutor.Abstractions.Core.IMediator</c>. <c>PipelineChainExecutor</c> catches it at every layer it
/// already guards for <see cref="PipelineEjectException{TResponse}"/>, resolves
/// <see cref="IOutcomeCaseRegistry"/> from the ambient <see cref="IServiceProvider"/>, and performs the single
/// <see cref="IOutcomeCaseRegistry.Construct{TResponse, TCase}(TCase)"/> call - closed reflectively over this
/// exception's runtime <see cref="CaseType"/>, via
/// <see cref="OutcomeCaseRegistryExtensions.ConstructOrThrow{TResponse}(IOutcomeCaseRegistry, PipelineEjectCaseException)"/>
/// - needed to turn the boxed case back into an ordinary <c>TResponse</c> value.
/// </para>
/// <para>
/// Construct this only through <see cref="Eject.WithCase{TCase}(TCase)"/> - never directly.
/// </para>
/// </remarks>
// RCS1194 wants the standard parameterless/message/message+innerException/serialization constructor set. This
// exception's CaseValue/CaseType are load-bearing - the entire point of catching it is to recover the case it
// carries - so a parameterless or message-only constructor would produce a useless signal missing the one
// thing every catch site needs. Suppressed here rather than adding constructors that would only invite
// misuse, mirroring PipelineEjectException{TResponse}'s identical rationale.
#pragma warning disable RCS1194
public sealed class PipelineEjectCaseException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="PipelineEjectCaseException"/> class.</summary>
    /// <param name="caseValue">The case value the ejecting dispatch should have its response constructed from.</param>
    internal PipelineEjectCaseException(object caseValue)
        : base(
            $"Ejected with a case of type '{caseValue.GetType().Name}' via Eject.WithCase. This is an internal "
                + "pipeline control-flow signal and must never be observed outside a mediator dispatch - if "
                + "you are seeing this message, a catch site that should have converted it back into an "
                + "ordinary response is missing."
        )
    {
        this.CaseValue = caseValue;
        this.CaseType = caseValue.GetType();
    }

    /// <summary>Gets the boxed case value the ejecting dispatch should have its response constructed from.</summary>
    /// <value>The boxed case value the ejecting dispatch should have its response constructed from.</value>
    public object CaseValue { get; }

    /// <summary>Gets the runtime type of <see cref="CaseValue"/>.</summary>
    /// <value>The runtime type of <see cref="CaseValue"/>.</value>
    public Type CaseType { get; }
}
#pragma warning restore RCS1194
