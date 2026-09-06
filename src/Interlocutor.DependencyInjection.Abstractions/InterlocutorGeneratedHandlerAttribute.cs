namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Assembly-level marker describing one compile-time discovered query, command, or bare-command handler's
/// request/response shape (ticket 12). Applied once per matching handler by <c>Interlocutor.Generator</c> -
/// never by hand.
/// </summary>
/// <remarks>
/// <para>
/// This attribute exists so the missing-handler diagnostic (<c>Interlocutor.Generator.InterlocutorDiagnostics.MissingHandler</c>)
/// can still fire correctly when a request is declared in one assembly and its handler lives in a different,
/// referenced assembly. <see cref="InterlocutorGeneratedModuleAttribute.ModuleType"/>'s <c>Registrations</c>
/// property carries fully-formed <see cref="InterlocutorRegistration"/> instances built inside a static
/// property initializer - not something Roslyn's symbol APIs can read back out of another assembly's compiled
/// IL without evaluating it. This attribute instead restates, as plain constructor-argument metadata readable
/// via <see cref="System.Reflection.Metadata"/>-level symbol inspection alone, exactly the (request type,
/// response type, kind) triple the missing-handler check needs - so a referencing project's generator can
/// answer "does some referenced assembly already handle this request?" from attribute metadata alone, with no
/// IL evaluation and no runtime reflection.
/// </para>
/// <para>
/// Only query, command, and bare-command handlers get one of these - notification handlers never participate
/// in the missing-handler diagnostic (a notification may legally have zero handlers), so the generator does
/// not emit this attribute for <see cref="GeneratedHandlerKind.Notification"/>.
/// </para>
/// </remarks>
/// <param name="requestType">The handler's request type.</param>
/// <param name="responseType">
/// The handler's response type, or <see langword="null"/> for a bare-command handler's request-matching
/// purposes - a bare command's result type is chosen by the caller, not declared on the request, so matching
/// ignores it (mirrors <c>Interlocutor.Generator.RequestKind.BareCommand</c>).
/// </param>
/// <param name="kind">Which handler contract <paramref name="requestType"/> is handled by.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class InterlocutorGeneratedHandlerAttribute(Type requestType, Type? responseType, GeneratedHandlerKind kind)
    : Attribute
{
    /// <summary>Gets the handler's request type.</summary>
    /// <value>The handler's request type.</value>
    public Type RequestType { get; } = requestType ?? throw new ArgumentNullException(nameof(requestType));

    /// <summary>Gets the handler's response type, or <see langword="null"/> for a bare-command handler.</summary>
    /// <value>The handler's response type, or <see langword="null"/> for a bare-command handler.</value>
    public Type? ResponseType { get; } = responseType;

    /// <summary>Gets which handler contract <see cref="RequestType"/> is handled by.</summary>
    /// <value>Which handler contract <see cref="RequestType"/> is handled by.</value>
    public GeneratedHandlerKind Kind { get; } = kind;
}
