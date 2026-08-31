namespace Interlocutor.DependencyInjection.Abstractions;

using System.Diagnostics;

/// <summary>
/// A container-neutral description of a single factory-backed service registration - the delegate-form
/// counterpart to <see cref="InterlocutorRegistration"/>, used where a registration is a caller-supplied
/// closure rather than a type the container can construct on its own (for example, a lambda-form pipeline
/// stage registered via <see cref="InterlocutorPipelineBuilder.AddStage(Type, RegistrationLifetime)"/>'s
/// delegate overloads).
/// </summary>
/// <remarks>
/// Like <see cref="InterlocutorRegistration"/>, this is the unit of exchange between the container-neutral
/// builder and the container-specific translators in <c>Interlocutor.DependencyInjection.Microsoft</c> and
/// <c>Interlocutor.DependencyInjection.Autofac</c> - neither retains its own factory-registration logic.
/// </remarks>
/// <param name="ServiceType">The closed service (contract) type the registration satisfies.</param>
/// <param name="Factory">
/// Produces the service instance given the container's <see cref="IServiceProvider"/> - used both by
/// factories that need no services (which simply ignore the parameter) and by lambda-form stages that
/// resolve services from it.
/// </param>
/// <param name="Lifetime">The service lifetime the registration should use.</param>
/// <param name="Kind">The role this registration plays, used by translators and future override policy.</param>
[DebuggerDisplay("{Kind} {ServiceType.Name} (factory, {Lifetime})")]
public readonly record struct InterlocutorFactoryRegistration(
    Type ServiceType,
    Func<IServiceProvider, object> Factory,
    RegistrationLifetime Lifetime,
    RegistrationKind Kind
);
