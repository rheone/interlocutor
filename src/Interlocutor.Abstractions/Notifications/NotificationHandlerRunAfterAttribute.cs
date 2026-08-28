namespace Interlocutor.Abstractions.Notifications;

/// <summary>
/// Declares that the annotated <see cref="INotificationHandler{TNotification}"/> implementation must run after
/// <paramref name="handlerType"/> whenever both are resolved for the same publish. See ADR-0007.
/// </summary>
/// <remarks>
/// <para>
/// This is the relative-ordering escape hatch for the rare genuine ordering need: execution order across
/// matching notification handlers is otherwise explicitly unspecified. <c>Interlocutor.Generator</c> reads
/// this attribute at build time, topologically sorts every handler type carrying a
/// <see cref="NotificationHandlerRunAfterAttribute"/> or <see cref="NotificationHandlerRunBeforeAttribute"/>,
/// and reports an ordering cycle as a build error naming the cycle rather than letting it fail silently at
/// runtime. <paramref name="handlerType"/> must itself be an <see cref="INotificationHandler{TNotification}"/>
/// implementation, or the generator reports it as invalid.
/// </para>
/// <para>Apply more than once to declare more than one predecessor.</para>
/// </remarks>
/// <param name="handlerType">The handler type this handler must run after.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class NotificationHandlerRunAfterAttribute(Type handlerType) : Attribute
{
    /// <summary>Gets the handler type the annotated handler must run after.</summary>
    /// <value>The handler type the annotated handler must run after.</value>
    public Type HandlerType { get; } = handlerType;
}
