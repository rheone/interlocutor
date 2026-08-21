namespace Interlocutor.Abstractions.Notifications;

/// <summary>
/// Declares that the annotated <see cref="INotificationHandler{TNotification}"/> implementation must run before
/// <paramref name="handlerType"/> whenever both are resolved for the same publish. See ADR-0007.
/// </summary>
/// <remarks>
/// <para>
/// The mirror image of <see cref="NotificationHandlerRunAfterAttribute"/> - see its remarks for the full
/// ordering design. <paramref name="handlerType"/> must itself be an
/// <see cref="INotificationHandler{TNotification}"/> implementation, or the generator reports it as invalid.
/// </para>
/// <para>Apply more than once to declare more than one successor.</para>
/// </remarks>
/// <param name="handlerType">The handler type the annotated handler must run before.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class NotificationHandlerRunBeforeAttribute(Type handlerType) : Attribute
{
    /// <summary>Gets the handler type the annotated handler must run before.</summary>
    public Type HandlerType { get; } = handlerType;
}
