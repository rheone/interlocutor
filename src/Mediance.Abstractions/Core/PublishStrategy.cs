namespace Mediance.Abstractions.Core;

/// <summary>Controls how notifications are dispatched to multiple handlers.</summary>
public enum PublishStrategy
{
    /// <summary>Handlers execute in registration order. Stops on the first exception.</summary>
    Sequential,

    /// <summary>Handlers execute concurrently via Task.WhenAll. Exceptions are aggregated.</summary>
    Parallel,
}
