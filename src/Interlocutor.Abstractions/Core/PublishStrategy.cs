namespace Interlocutor.Abstractions.Core;

/// <summary>Controls how notifications are dispatched to multiple handlers.</summary>
/// <remarks>
/// <para>
/// Pass a <see cref="PublishStrategy"/> to the mediator's <c>Publish</c> overload to control notification handler execution:
/// <list type="bullet">
/// <item><description><see cref="Sequential"/> — handlers run in order, stopping on the first exception. Use for ordered/dependent logic.</description></item>
/// <item><description><see cref="Parallel"/> — handlers run concurrently; exceptions aggregate into <see cref="AggregateException"/>. Use when handlers are independent and you want throughput.</description></item>
/// </list>
/// </para>
/// <para>
/// Default strategy is sequential (used by the parameterless <c>Publish</c> overload).
/// </para>
/// </remarks>
public enum PublishStrategy
{
    /// <summary>Handlers execute in registration order. Stops on the first exception.</summary>
    Sequential,

    /// <summary>Handlers execute concurrently via Task.WhenAll. Exceptions are aggregated.</summary>
    Parallel,
}
