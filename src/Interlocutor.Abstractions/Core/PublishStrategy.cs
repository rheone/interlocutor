namespace Interlocutor.Abstractions.Core;

/// <summary>Controls how notifications are dispatched to multiple handlers.</summary>
/// <remarks>
/// <para>
/// Pass a <see cref="PublishStrategy"/> to the
/// <see cref="IPublisher.PublishAsync{TNotification}(TNotification, PublishStrategy, CancellationToken)"/>
/// overload to control notification handler execution:
/// <list type="bullet">
/// <item><description><see cref="Sequential"/> - handlers run in order, stopping on the first exception. Use for ordered/dependent logic.</description></item>
/// <item><description><see cref="Parallel"/> - handlers run concurrently; exceptions aggregate into <see cref="AggregateException"/>. Use when handlers are independent and you want throughput.</description></item>
/// </list>
/// </para>
/// <para>
/// Default strategy is sequential - the strategy used by the parameterless
/// <see cref="IPublisher.PublishAsync{TNotification}(TNotification, CancellationToken)"/> overload.
/// </para>
/// </remarks>
public enum PublishStrategy
{
    /// <summary>Runs handlers sequentially, in registration order, stopping at the first exception.</summary>
    Sequential,

    /// <summary>
    /// Runs handlers concurrently via <see cref="Task.WhenAll(System.Threading.Tasks.Task[])"/>; exceptions from
    /// every handler are aggregated rather than surfaced individually.
    /// </summary>
    Parallel,
}
