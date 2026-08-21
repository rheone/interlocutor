namespace Interlocutor.Generator;

using System.Collections.Immutable;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of everything <see cref="InterlocutorSourceGenerator"/>
/// needs from the compilation itself, rather than from an individual candidate type declaration: the
/// namespace segment this compilation's own generated module is emitted under, and every upstream module
/// discovered across the reference graph (ticket 12).
/// </summary>
/// <param name="AssemblyNamespaceSegment">
/// This compilation's own sanitized assembly-name namespace segment - see
/// <see cref="IdentifierSanitizer.ToNamespaceSegment(string)"/>.
/// </param>
/// <param name="ExternalModules">Every referenced assembly's generated registration module, if any.</param>
internal sealed record CompilationInfo(string AssemblyNamespaceSegment, ImmutableArray<ExternalModuleInfo> ExternalModules);
