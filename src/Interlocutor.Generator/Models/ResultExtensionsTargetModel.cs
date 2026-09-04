namespace Interlocutor.Generator.Models;

using System.Diagnostics;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of the <c>GenerateResultExtensionsAttribute</c>-attributed
/// partial class declaration <see cref="Emission.ResultExtensionsEmitter"/> emits the per-arity HTTP result
/// overloads into.
/// </summary>
/// <param name="Namespace">The declaring namespace, or empty for the global namespace.</param>
/// <param name="ModifiersText">
/// The class declaration's own modifier tokens verbatim (e.g. <c>"public static partial"</c>) - reused so the
/// generated partial declaration's modifiers agree with the hand-written one, as a partial type's declarations
/// must.
/// </param>
/// <param name="TypeName">The class's simple (unqualified) type name.</param>
/// <param name="HintName">The hint name passed to <c>AddSource</c>.</param>
[DebuggerDisplay("{TypeName}")]
internal sealed record ResultExtensionsTargetModel(string Namespace, string ModifiersText, string TypeName, string HintName);
