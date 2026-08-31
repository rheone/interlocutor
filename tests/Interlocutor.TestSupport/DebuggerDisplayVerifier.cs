namespace Interlocutor.TestSupport;

using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// Verifies that a type's <see cref="DebuggerDisplayAttribute"/> format string only references members that
/// actually exist - the one class of bug that attribute is prone to, since its expression is a plain string
/// literal the compiler never checks. A member renamed after the attribute was written fails a test calling
/// this helper immediately, instead of silently breaking the debugger experience for whoever next steps
/// through that type.
/// </summary>
public static class DebuggerDisplayVerifier
{
    // S6444 wants a timeout to bound unbounded input, but the input here is never user/network-controlled -
    // it's this repo's own DebuggerDisplayAttribute format strings, short and fixed at compile time, matched
    // against a pattern with no nested quantifiers to trigger catastrophic backtracking. A timeout could never
    // realistically fire.
#pragma warning disable S6444
    private static readonly Regex TokenPattern = new(@"\{([^}]+)\}", RegexOptions.Compiled);
#pragma warning restore S6444

    // S3011 (reflecting into non-public members) is the entire point of this helper - a DebuggerDisplay
    // expression is evaluated in the debugger's own scope, which can see a type's private members too.
#pragma warning disable S3011
    private const BindingFlags MemberLookupFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
#pragma warning restore S3011

    /// <summary>
    /// Asserts that every member path referenced by <paramref name="type"/>'s own <see cref="DebuggerDisplayAttribute"/>
    /// format string (the <c>{Member}</c>, <c>{Member,nq}</c>, and <c>{Member.NestedMember}</c> forms) resolves
    /// to a real, readable member - a property, field, or parameterless method - on <paramref name="type"/> or
    /// the type each preceding segment resolves to.
    /// </summary>
    /// <param name="type">The type to verify. Call this even when the caller doesn't know in advance whether
    /// it carries <see cref="DebuggerDisplayAttribute"/> - a type with no attribute trivially passes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// A referenced member path does not resolve to a readable member.
    /// </exception>
    public static void AssertValid(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var attribute = type.GetCustomAttribute<DebuggerDisplayAttribute>(inherit: false);
        if (attribute is null)
        {
            return;
        }

        foreach (var path in ExtractMemberPaths(attribute.Value))
        {
            AssertResolves(type, path);
        }
    }

    /// <summary>Extracts each <c>{...}</c> token's member path, ignoring literal text outside braces.</summary>
    /// <param name="format">The <see cref="DebuggerDisplayAttribute.Value"/> format string.</param>
    private static IEnumerable<string> ExtractMemberPaths(string format)
    {
        foreach (Match match in TokenPattern.Matches(format))
        {
            var expression = match.Groups[1].Value;

            // The ",nq" ("no quotes") suffix is a formatting directive, not part of the member path -
            // {Member,nq} and {Member} name the same member.
            var commaIndex = expression.IndexOf(',');
            yield return (commaIndex >= 0 ? expression[..commaIndex] : expression).Trim();
        }
    }

    /// <summary>Walks one dot-separated member path from <paramref name="rootType"/>, throwing if any segment doesn't resolve.</summary>
    private static void AssertResolves(Type rootType, string path)
    {
        var currentType = rootType;
        foreach (var rawSegment in path.Split('.'))
        {
            // DebuggerDisplay expressions allow a trailing "()" to mark a parameterless method call explicitly
            // (e.g. "{ToString()}"); strip it before member lookup.
            var segment = rawSegment.EndsWith("()", StringComparison.Ordinal) ? rawSegment[..^2] : rawSegment;

            currentType =
                ResolveMemberType(currentType, segment)
                ?? throw new InvalidOperationException(
                    $"{rootType}'s DebuggerDisplay expression references '{path}', but '{segment}' does not "
                        + $"resolve to a readable property, field, or parameterless method on {currentType}."
                );
        }
    }

    /// <summary>Resolves one path segment to the type of the property, field, or parameterless method it names.</summary>
    /// <returns>The resolved member's value type, or <see langword="null"/> if <paramref name="name"/> doesn't resolve.</returns>
    private static Type? ResolveMemberType(Type type, string name)
    {
        // GetProperty/GetField/GetMethod on an interface type only look at members declared directly on that
        // interface, not ones it inherits (unlike a class, where FlattenHierarchy walks base classes) - a
        // member like IReadOnlyList<T>.Count, declared on IReadOnlyCollection<T>, would otherwise never
        // resolve. Search the interface itself, then every interface it extends.
        foreach (var candidate in CandidateTypes(type))
        {
            var property = candidate.GetProperty(name, MemberLookupFlags);
            if (property is not null && property.CanRead)
            {
                return property.PropertyType;
            }

            var field = candidate.GetField(name, MemberLookupFlags);
            if (field is not null)
            {
                return field.FieldType;
            }

            var method = candidate.GetMethod(name, MemberLookupFlags, Type.EmptyTypes);
            if (method is not null)
            {
                return method.ReturnType;
            }
        }

        return null;
    }

    /// <summary>The type itself, followed by every interface it extends when it is itself an interface.</summary>
    private static IEnumerable<Type> CandidateTypes(Type type)
    {
        yield return type;

        if (type.IsInterface)
        {
            foreach (var extended in type.GetInterfaces())
            {
                yield return extended;
            }
        }
    }
}
