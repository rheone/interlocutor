namespace Interlocutor.Abstractions.Pipeline.Cases;

/// <summary>
/// Thrown by <see cref="IOutcomeCaseRegistry.Construct{TResponse, TCase}(TCase)"/> or
/// <see cref="IOutcomeCaseRegistry.TryGetActiveCase{TResponse}(TResponse, out Type, out object?)"/> when the
/// registry has no way to construct or decompose a given response type - it is neither a compiler-generated
/// union covering the case in question, nor covered by a factory/extractor registered via
/// <see cref="OutcomeCaseRegistryBuilder"/>.
/// </summary>
/// <remarks>
/// This is a configuration mistake (a response type paired with a case it can't express, or a hand-rolled
/// <see cref="System.Runtime.CompilerServices.IUnion"/> implementation used before its factory/extractor was
/// registered), not a normal runtime outcome - so it is thrown rather than silently swallowed or reported as
/// an ordinary "not found" result, mirroring how <see cref="AuthorizationBehavior{TRequest, TResponse}"/>
/// already reports the equivalent problem for its own <c>Forbidden</c> short-circuit.
/// </remarks>
// RCS1194 wants the standard parameterless/message/message+innerException/serialization constructor set. This
// exception's ResponseType/CaseType are load-bearing - the whole point of catching it is to read which
// response shape could not express which case - so a parameterless or message-only constructor would produce
// a useless exception missing the one thing callers need. Suppressed here rather than adding constructors
// that would only confuse callers into using them. See HandlerNotRegisteredException's identical suppression.
#pragma warning disable RCS1194
public sealed class OutcomeCaseNotSupportedException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="OutcomeCaseNotSupportedException"/> class.</summary>
    /// <param name="responseType">The response type that could not be constructed or decomposed.</param>
    /// <param name="caseType">
    /// The case type that could not be constructed or extracted, or <see langword="null"/> when the failure
    /// is extraction of an unknown active case rather than construction of a specific one.
    /// </param>
    /// <param name="message">A message describing the failure, in the same style as <see cref="AuthorizationBehavior{TRequest, TResponse}"/>'s existing exception text.</param>
    public OutcomeCaseNotSupportedException(Type responseType, Type? caseType, string message)
        : base(message)
    {
        this.ResponseType = responseType;
        this.CaseType = caseType;
    }

    /// <summary>Gets the response type that could not be constructed or decomposed.</summary>
    /// <value>The response type that could not be constructed or decomposed.</value>
    public Type ResponseType { get; }

    /// <summary>
    /// Gets the case type that could not be constructed or extracted, or <see langword="null"/> when the
    /// failure is extraction of an unknown active case rather than construction of a specific one.
    /// </summary>
    /// <value>
    /// The case type that could not be constructed or extracted, or <see langword="null"/> when the failure
    /// is extraction of an unknown active case rather than construction of a specific one.
    /// </value>
    public Type? CaseType { get; }
}
#pragma warning restore RCS1194
