namespace Interlocutor.IntegrationTests;

using Interlocutor.Abstractions.Notifications;

/// <summary>
/// Published by <see cref="CreateWidgetWithNotificationCommandHandler"/> after inserting a <see cref="Widget"/>
/// but before its own handler returns - fanned out to every real <see cref="INotificationHandler{TNotification}"/>
/// subscriber through the real <see cref="Interlocutor.Abstractions.Core.IPublisher"/>, proving cross-cutting
/// notification dispatch composes correctly with the resilience and transaction behaviors this project already
/// exercises, rather than being proven only in isolation by <c>Interlocutor.Abstractions.Tests</c>.
/// </summary>
/// <param name="Name">The name of the <see cref="Widget"/> that was created.</param>
public sealed record WidgetCreatedNotification(string Name) : INotification;
