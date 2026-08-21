namespace Interlocutor.Abstractions.Core;

/// <summary>
/// Implemented by <c>Interlocutor.Generator</c>'s emitted notification handler order table - the compile-time
/// computed relative order (ADR-0007) for notification handlers whose declaring type carries a
/// <c>NotificationHandlerRunAfterAttribute</c> or <c>NotificationHandlerRunBeforeAttribute</c>.
/// </summary>
/// <remarks>
/// <para>
/// A single generated implementation, <c>Interlocutor.Generated.InterlocutorGeneratedNotificationHandlerOrder</c>,
/// is registered as a singleton alongside the rest of a compilation's generated registrations (see
/// <c>InterlocutorBuilder.AddGeneratedRegistrations</c>) - never constructed directly.
/// <c>Interlocutor.Abstractions.Notifications.NotificationHandlerResolver</c> resolves it once per publish via
/// <see cref="IServiceProvider.GetService(Type)"/> and, when present, uses <see cref="Rank"/> to order the
/// resolved handler instances before dispatch. When absent (the generator never ran over the compilation, or
/// its output was never passed to <c>AddGeneratedRegistrations</c>), every handler is treated as unranked and
/// execution order stays whatever order <see cref="IServiceProvider"/> happens to resolve services in -
/// unspecified, per ADR-0007, either way.
/// </para>
/// <para>
/// Only handler types involved in at least one run-after/run-before relationship receive a rank distinct from
/// <see cref="int.MaxValue"/> - every other handler type is deliberately left unranked so it sorts after every
/// ranked handler without disturbing its resolution-order position relative to other unranked handlers (a
/// stable sort by <see cref="Rank"/> preserves that).
/// </para>
/// </remarks>
public interface IGeneratedNotificationHandlerOrder
{
    /// <summary>
    /// Returns <paramref name="handlerType"/>'s position in the compile-time computed topological order, or
    /// <see cref="int.MaxValue"/> if it was never involved in a run-after/run-before relationship.
    /// </summary>
    /// <param name="handlerType">A notification handler's concrete, runtime type.</param>
    /// <returns>A lower value must run earlier; <see cref="int.MaxValue"/> means unranked.</returns>
    int Rank(Type handlerType);
}
