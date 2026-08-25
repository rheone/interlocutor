namespace Interlocutor.DependencyInjection.Abstractions;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

/// <summary>
/// The single <see cref="Assembly.GetTypes"/> call site every scan path in this package goes through -
/// <see cref="HandlerDiscovery.Discover(IEnumerable{Assembly})"/> and
/// <see cref="InterlocutorBuilder.CandidateTypesForMatching"/> alike - so a <see cref="ReflectionTypeLoadException"/>
/// from one unloadable type never aborts the whole scan.
/// </summary>
internal static class AssemblyTypeScanner
{
    /// <summary>
    /// Returns every type in <paramref name="assembly"/> that loaded successfully, continuing past any
    /// that didn't rather than letting <see cref="ReflectionTypeLoadException"/> abort the whole scan.
    /// </summary>
    /// <param name="assembly">The assembly to enumerate types from.</param>
    /// <param name="failures">
    /// When given, one <see cref="TypeLoadFailure"/> is added for each type that failed to load. Left
    /// <see langword="null"/> by a caller that has nowhere to surface failures - the types that did load
    /// are still returned either way.
    /// </param>
    /// <returns>Every type in <paramref name="assembly"/> that loaded successfully.</returns>
    [RequiresUnreferencedCode("Calls Assembly.GetTypes(), which reflects over every type the assembly declares.")]
    public static IEnumerable<Type> GetLoadableTypes(Assembly assembly, ICollection<TypeLoadFailure>? failures)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ExtractLoadedTypes(assembly, ex, failures);
        }
    }

    /// <summary>
    /// Extracts the types that did load from a <see cref="ReflectionTypeLoadException"/>, recording one
    /// <see cref="TypeLoadFailure"/> per type that didn't - the logic behind <see cref="GetLoadableTypes"/>'s
    /// catch branch, split out so it can be exercised directly against a manually-constructed exception
    /// rather than requiring a genuinely unloadable assembly on disk.
    /// </summary>
    /// <param name="assembly">The assembly <paramref name="ex"/> was thrown while reflecting over.</param>
    /// <param name="ex">The exception <see cref="Assembly.GetTypes"/> threw.</param>
    /// <param name="failures">
    /// When given, receives one <see cref="TypeLoadFailure"/> per type that failed to load. Left
    /// <see langword="null"/> by a caller that has nowhere to surface failures - the types that did load
    /// are still returned either way.
    /// </param>
    /// <returns>Every type in <see cref="ReflectionTypeLoadException.Types"/> that loaded successfully.</returns>
    internal static IEnumerable<Type> ExtractLoadedTypes(
        Assembly assembly,
        ReflectionTypeLoadException ex,
        ICollection<TypeLoadFailure>? failures
    )
    {
        if (failures is not null)
        {
            var assemblyName = assembly.GetName().Name ?? assembly.FullName ?? "(unknown assembly)";
            for (var i = 0; i < ex.Types.Length; i++)
            {
                // ex.Types carries a null entry (not the failed Type) for each type that didn't load; the
                // corresponding LoaderExceptions entry - whose message typically names the type - is what
                // actually explains the failure.
                if (ex.Types[i] is not null)
                {
                    continue;
                }

                var detail = i < ex.LoaderExceptions.Length ? ex.LoaderExceptions[i]?.Message : null;
                failures.Add(new TypeLoadFailure(assemblyName, detail ?? "Unknown type load failure."));
            }
        }

        return ex.Types.OfType<Type>();
    }
}
