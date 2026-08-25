namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Records one type that failed to load while scanning an assembly - surfaced as a non-fatal entry in
/// <see cref="RegistrationReport.TypeLoadFailures"/> instead of aborting the scan that hit it.
/// </summary>
/// <remarks>
/// Produced only when <see cref="System.Reflection.Assembly.GetTypes"/> throws
/// <see cref="System.Reflection.ReflectionTypeLoadException"/> - most commonly a missing transitive
/// dependency assembly. Every type in the same assembly that did load successfully is still scanned and
/// registered normally; this is purely additional, discoverable detail about what couldn't be.
/// </remarks>
/// <param name="AssemblyName">The name of the assembly one or more of whose types failed to load.</param>
/// <param name="Detail">
/// The loader exception's own message for the failed type - typically names the type itself, since
/// <see cref="System.Reflection.ReflectionTypeLoadException.Types"/> carries a <see langword="null"/> entry
/// (not the failed <see cref="System.Type"/>) for each failure.
/// </param>
public readonly record struct TypeLoadFailure(string AssemblyName, string Detail);
