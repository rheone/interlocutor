namespace Interlocutor.IntegrationTests;

/// <summary>
/// Records, in the order it actually happened, which real <see cref="WidgetCreatedNotification"/> subscriber ran
/// for the current dispatch scope. Shared between <see cref="WidgetCreatedAuditNotificationHandler"/> and
/// <see cref="WidgetCreatedWelcomeEmailNotificationHandler"/> so the test observes the real fan-out order the
/// real <see cref="Interlocutor.Abstractions.Core.IPublisher"/> produced, rather than a mock's recorded calls.
/// </summary>
/// <remarks>
/// Registered with the same scoped lifetime the mediator resolves notification handlers with, so each test's
/// own scope gets its own log - concurrently running tests in this project never share one.
/// </remarks>
public sealed class WidgetNotificationLog
{
    private readonly List<string> handlerNames = [];

    /// <summary>Gets the handler names that ran, in the order they actually ran.</summary>
    /// <value>The recorded handler names, oldest first.</value>
    public IReadOnlyList<string> HandlerNames => this.handlerNames;

    /// <summary>Records that the handler named <paramref name="handlerName"/> ran.</summary>
    /// <param name="handlerName">The name of the handler that ran.</param>
    public void Record(string handlerName) => this.handlerNames.Add(handlerName);
}
