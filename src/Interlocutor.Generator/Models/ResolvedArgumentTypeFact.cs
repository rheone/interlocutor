namespace Interlocutor.Generator.Models;

using System.Diagnostics;

/// <summary>
/// One concrete generic type argument's value-type/reference-type/parameterless-constructor facts, resolved
/// once from its CLR metadata name - see <see cref="GenericConstraintSnapshot.ArgumentTypeFacts"/>.
/// </summary>
/// <param name="MetadataName">The type argument's CLR metadata name (no <c>global::</c> prefix).</param>
/// <param name="IsValueType">Whether the resolved type is a value type.</param>
/// <param name="IsReferenceType">Whether the resolved type is a reference type.</param>
/// <param name="HasAccessibleParameterlessConstructor">
/// Whether the resolved type has a public, accessible, parameterless constructor, or is a value type (which
/// always does).
/// </param>
/// <remarks>
/// <para>
/// A plain <c>string</c>/<c>bool</c> record struct - no <see cref="System.Collections.Immutable.ImmutableArray{T}"/>
/// field, so the compiler-generated equality this type gets is already correct element-wise, unlike
/// <see cref="GenericConstraintSnapshot"/> itself.
/// </para>
/// </remarks>
[DebuggerDisplay("{MetadataName} (IsValueType={IsValueType})")]
internal readonly record struct ResolvedArgumentTypeFact(
    string MetadataName,
    bool IsValueType,
    bool IsReferenceType,
    bool HasAccessibleParameterlessConstructor
);
