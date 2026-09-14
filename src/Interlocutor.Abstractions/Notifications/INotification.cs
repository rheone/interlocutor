namespace Interlocutor.Abstractions.Notifications;

/// <summary>Marker interface for domain events and other fire-and-forget notifications.</summary>
/// <remarks>
/// <para>
/// Implement this on any type that should be publishable via <see cref="Core.IPublisher"/> and consumed by zero
/// or more <see cref="INotificationHandler{TNotification}"/> subscribers. Unlike a query or command, a
/// notification carries no response type and has no single designated handler - publishing one is a fan-out to
/// however many subscribers are registered, not a request/response exchange.
/// </para>
/// </remarks>
public interface INotification;
