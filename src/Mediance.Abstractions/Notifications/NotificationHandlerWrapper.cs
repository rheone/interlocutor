namespace Mediance.Abstractions.Notifications;

/// <summary>Defines the base contract for notification dispatch wrappers.</summary>
internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(object notification, IServiceProvider services, CancellationToken ct);
}
