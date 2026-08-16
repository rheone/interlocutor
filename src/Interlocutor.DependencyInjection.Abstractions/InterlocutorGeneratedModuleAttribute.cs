namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Assembly-level marker identifying the compile-time generated registration module a handler-defining
/// assembly emits (ADR-0008). Applied once per assembly by <c>Interlocutor.Generator</c> - never by hand.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ModuleType"/> names a public static type exposing a
/// <c>public static IReadOnlyList&lt;InterlocutorRegistration&gt; Registrations { get; }</c> member - the
/// generator's own emitted module always has this exact shape, since it is the generator, not a hand-written
/// consumer, that both applies this attribute and defines the type it points to. A future cross-assembly
/// aggregator (ticket 12) discovers every referenced assembly's module by reading this attribute via
/// <see cref="System.Reflection.Assembly.GetCustomAttribute{T}()"/> and reading <see cref="ModuleType"/>'s
/// <c>Registrations</c> member reflectively, once per referenced assembly at startup - a fixed, one-time cost
/// unrelated to (and far cheaper than) the per-request-type reflection this generator replaces.
/// </para>
/// <para>
/// A consuming project that wants its own compilation's generated registrations without waiting for ticket 12's
/// aggregator can already reference the module directly by its known, generator-emitted name
/// (<c>Interlocutor.Generated.InterlocutorGeneratedRegistrations</c>) and pass its <c>Registrations</c> property
/// straight to <see cref="InterlocutorBuilder.AddGeneratedRegistrations(System.Collections.Generic.IEnumerable{InterlocutorRegistration})"/> -
/// a fully compile-time-checked, reflection-free call. This attribute exists for the cross-assembly case where
/// the module's exact name and containing assembly are not known ahead of time by the code reading it.
/// </para>
/// </remarks>
/// <param name="moduleType">
/// The generated registration module type - a public static class exposing a static
/// <c>Registrations</c> property of type <c>IReadOnlyList&lt;InterlocutorRegistration&gt;</c>.
/// </param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class InterlocutorGeneratedModuleAttribute(Type moduleType) : Attribute
{
    /// <summary>Gets the generated registration module type this attribute identifies.</summary>
    /// <value>The generated registration module type this attribute identifies.</value>
    public Type ModuleType { get; } = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
}
