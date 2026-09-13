namespace Interlocutor.IntegrationTests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Handles <see cref="CreateWidgetWithNotificationCommand"/> by inserting one <see cref="Widget"/> through the
/// scoped <see cref="IntegrationTestDbContext"/>, then publishing <see cref="WidgetCreatedNotification"/>
/// through the real <see cref="IPublisher"/> before returning - fanning out to every registered
/// <c>INotificationHandler&lt;WidgetCreatedNotification&gt;</c> subscriber. Never reached at all for a dispatch
/// <c>RateLimitedBehavior</c> throttles further out in the pipeline, so neither the insert nor the publish nor
/// the transaction commit happens for a throttled dispatch.
/// </summary>
/// <param name="context">The scoped <see cref="IntegrationTestDbContext"/> this dispatch writes through.</param>
/// <param name="publisher">The real publisher this handler fans <see cref="WidgetCreatedNotification"/> out through.</param>
public sealed class CreateWidgetWithNotificationCommandHandler(IntegrationTestDbContext context, IPublisher publisher)
    : ICommandHandler<CreateWidgetWithNotificationCommand, Outcome<Success, RateLimited>>
{
    /// <inheritdoc/>
    public async Task<Outcome<Success, RateLimited>> HandleAsync(
        CreateWidgetWithNotificationCommand command,
        CancellationToken ct
    )
    {
        await context.Widgets.AddAsync(new Widget { Name = command.Name }, ct).ConfigureAwait(false);
        await publisher.PublishAsync(new WidgetCreatedNotification(command.Name), ct).ConfigureAwait(false);
        return default(Success);
    }
}
