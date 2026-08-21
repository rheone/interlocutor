namespace Interlocutor.Generator;

using System.Text;

/// <summary>
/// Turns a fully-qualified, <c>global::</c>-prefixed type name into a syntactically valid, unique-enough
/// C# identifier for a generated dispatch wrapper class name.
/// </summary>
internal static class IdentifierSanitizer
{
    /// <summary>
    /// Replaces every character illegal in a C# identifier with an underscore. Not guaranteed globally
    /// unique on its own - callers combining a handler type name with its <see cref="HandlerKind"/> get
    /// uniqueness in practice because one handler type implements a given contract at most once.
    /// </summary>
    /// <param name="fullyQualifiedName">A fully-qualified type name, typically <c>global::</c>-prefixed.</param>
    /// <returns>A valid C# identifier derived from <paramref name="fullyQualifiedName"/>.</returns>
    public static string ToIdentifier(string fullyQualifiedName)
    {
        var builder = new StringBuilder(fullyQualifiedName.Length);
        foreach (var c in fullyQualifiedName)
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Turns a compiling assembly's simple name into a namespace segment that is guaranteed unique across
    /// every assembly in a reference graph - the segment <see cref="RegistrationModuleEmitter"/> nests each
    /// project's generated registration module under (ticket 12), so two projects that both ran this
    /// generator never emit a type with the exact same fully-qualified name.
    /// </summary>
    /// <param name="assemblyName">The compiling assembly's simple name (<c>Compilation.AssemblyName</c>).</param>
    /// <returns>A valid, non-empty C# identifier derived from <paramref name="assemblyName"/>.</returns>
    public static string ToNamespaceSegment(string assemblyName)
    {
        var identifier = ToIdentifier(assemblyName);
        if (identifier.Length == 0)
        {
            return "_";
        }

        // A leading digit is illegal as the first character of a C# identifier, even though ToIdentifier
        // leaves digits elsewhere in the name untouched.
        return char.IsDigit(identifier[0]) ? "_" + identifier : identifier;
    }
}
