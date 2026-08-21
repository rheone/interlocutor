namespace Interlocutor.Abstractions.Core;

/// <summary>
/// Declares that the annotated handler takes precedence over <paramref name="handlerType"/> when both would
/// otherwise match the same closed request type ambiguously.
/// </summary>
/// <remarks>
/// <para>
/// This is the escape hatch for a genuine ambiguity between two open generic handlers that both close over the
/// same concrete request type (see ticket 11's open-generic-handler support): by design, no such ambiguity is
/// ever resolved by inheritance distance, namespace, or assembly location - moving a file must never change
/// which handler runs - so an unresolved ambiguity is a build error naming both handlers unless one of them
/// carries this attribute naming the other. <c>Interlocutor.Generator</c> reads this attribute at build time;
/// <paramref name="handlerType"/> must itself be a discovered handler type in the same compilation (an open
/// generic handler is named via its own unbound generic type, e.g. <c>typeof(OtherHandler&lt;&gt;)</c>), or the
/// generator reports it as invalid.
/// </para>
/// <para>
/// Lives in this abstractions module - not a container or generator package - so a handler assembly declaring
/// an override takes no dependency beyond what it already has on the mediator's core contracts.
/// </para>
/// <para>Apply more than once to declare more than one overridden handler.</para>
/// </remarks>
/// <param name="handlerType">The handler type the annotated handler overrides.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class HandlerOverrideAttribute(Type handlerType) : Attribute
{
    /// <summary>Gets the handler type the annotated handler overrides.</summary>
    public Type HandlerType { get; } = handlerType;
}
