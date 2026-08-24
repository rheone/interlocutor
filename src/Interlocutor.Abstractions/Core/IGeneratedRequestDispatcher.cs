namespace Interlocutor.Abstractions.Core;

using Interlocutor.CommonKernel;

/// <summary>
/// Implemented by <c>Interlocutor.Generator</c>'s emitted type switch - the reflection-free replacement for
/// <see cref="ISender.Send(object, CancellationToken)"/>'s hot path when the generator had visibility into
/// the full set of request types in the compilation.
/// </summary>
/// <remarks>
/// <para>
/// A single generated implementation, <c>Interlocutor.Generated.Dispatch.GeneratedRequestDispatcher</c>, is
/// registered as a singleton alongside the rest of a compilation's generated registrations (see
/// <c>InterlocutorBuilder.AddGeneratedRegistrations</c>) - never constructed directly.
/// <see cref="Interlocutor.Abstractions.Mediator"/> resolves it once per <see cref="Dispatch"/> call via
/// <see cref="IServiceProvider.GetService(Type)"/> and, when present, dispatches through it exclusively - no
/// <see cref="System.Reflection.Assembly.GetTypes"/>/<see cref="Type.MakeGenericType"/>/
/// <see cref="Activator.CreateInstance(Type)"/> call runs on that path. When absent (the generator never ran
/// over the compilation, or its output was never passed to <c>AddGeneratedRegistrations</c>),
/// <see cref="Interlocutor.Abstractions.Mediator"/> falls back to the reflection-based implementation instead
/// - the explicitly opted-in runtime-scanning path.
/// </para>
/// <para>
/// The switch this interface's implementation performs is built entirely from <c>typeof(...)</c>-known,
/// closed request types discovered in the compilation - a request type from an assembly the generator never
/// saw simply falls to the switch's default branch, which throws the same
/// <see cref="UnsupportedRequestTypeException"/> the reflection path throws for a type it does not recognize
/// as a request at all, rather than silently falling through or throwing a different exception.
/// </para>
/// </remarks>
public interface IGeneratedRequestDispatcher
{
    /// <summary>
    /// Dispatches <paramref name="request"/> - whose runtime type is only known via a compile-time-generated
    /// type switch, not reflection - to its registered handler, returning the result as the library's base
    /// outcome interface.
    /// </summary>
    /// <param name="request">The request to dispatch.</param>
    /// <param name="services">The service provider to resolve the handler and behaviors from.</param>
    /// <param name="ct">A token that may be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> whose result is the handler's response as an <see cref="IOperationOutcome"/>.
    /// </returns>
    /// <exception cref="HandlerNotRegisteredException">
    /// <paramref name="request"/>'s runtime type is a request shape the generator saw in the compilation, but
    /// no handler for it was discovered there.
    /// </exception>
    /// <exception cref="UnsupportedRequestTypeException">
    /// <paramref name="request"/>'s runtime type does not match any case in the generated switch - either it
    /// is not a request at all, or it is a request type the generator never saw.
    /// </exception>
    Task<IOperationOutcome> Dispatch(object request, IServiceProvider services, CancellationToken ct);
}
