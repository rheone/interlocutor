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
/// consumer, that both applies this attribute and defines the type it points to.
/// </para>
/// <para>
/// <b>Cross-assembly aggregation (ticket 12) is entirely compile-time, via Roslyn - never
/// <c>System.Reflection.Assembly.GetCustomAttribute&lt;T&gt;()</c> or any other runtime reflection.</b> The
/// application-level generator walks <c>Microsoft.CodeAnalysis.Compilation.References</c>, resolves each
/// reference's <c>IAssemblySymbol</c>, and reads this attribute straight out of assembly-level metadata via
/// <c>IAssemblySymbol.GetAttributes()</c> - the same mechanism compilers use for <c>InternalsVisibleTo</c>. This
/// keeps the generated code AOT- and trim-friendly (ADR-0008's stated goal): nothing is ever loaded or
/// reflected over at run time, only read as compile-time metadata. Because a project reference's public API
/// surface (a "reference assembly") only changes when something observable changes, editing one library's
/// method bodies does not change what this attribute-based walk sees for that library, so it does not force a
/// referencing project's generator output to change either.
/// </para>
/// <para>
/// <b>The module's name is qualified by its own compiling assembly, never the fixed
/// <c>Interlocutor.Generated.InterlocutorGeneratedRegistrations</c> name alone.</b> Every project the generator
/// runs on emits its module under <c>Interlocutor.Generated.&lt;SanitizedAssemblyName&gt;.InterlocutorGeneratedRegistrations</c>,
/// where <c>SanitizedAssemblyName</c> is the compiling assembly's simple name with every character illegal in a
/// C# identifier replaced by <c>_</c>. This is not cosmetic: assembly names are unique within one project's
/// reference graph, but an unqualified module name is not - an application that both runs this generator over
/// its own code and references one or more libraries that also ran it would otherwise declare a source-level
/// type with the exact same fully-qualified name as a type already present in a referenced assembly, which is a
/// compile error (<c>CS0433</c>), not merely an aggregation nicety to work around. A consuming project can still
/// reference its own compilation's module directly, by its own assembly-qualified name, and pass its
/// <c>Registrations</c> property straight to
/// <see cref="InterlocutorBuilder.AddGeneratedRegistrations(System.Collections.Generic.IEnumerable{InterlocutorRegistration})"/> -
/// a fully compile-time-checked, reflection-free call - exactly as before; only the exact spelling of that name
/// changed. The aggregating generator itself concatenates every discovered upstream module's <c>Registrations</c>
/// into its own compilation's module, so a single <c>AddGeneratedRegistrations</c> call in the application
/// already carries every transitively-referenced library's handlers.
/// </para>
/// <para>
/// A referenced assembly's individual handlers remain visible to the missing-handler diagnostic
/// (<c>Interlocutor.Generator.InterlocutorDiagnostics.MissingHandler</c>) even though <c>Registrations</c>
/// itself cannot be read back at compile time - see <see cref="InterlocutorGeneratedHandlerAttribute"/>, which
/// restates each handler's request/response shape as separately-inspectable metadata for exactly that purpose.
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
