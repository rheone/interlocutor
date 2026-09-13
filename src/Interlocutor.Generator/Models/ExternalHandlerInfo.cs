namespace Interlocutor.Generator.Models;

using System.Diagnostics;
using Interlocutor.Generator.Emission;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one query, command, or bare-command handler declared
/// by a referenced assembly's own generator run (see <see cref="RegistrationModuleEmitter.EmitAssemblyAttributes"/>
/// on that earlier run), read back from that assembly's <c>InterlocutorGeneratedHandlerAttribute</c> metadata
/// (ticket 12) rather than re-discovered by walking the referenced assembly's types.
/// </summary>
/// <param name="Kind">
/// Which handler contract the referenced assembly's handler implements - always <see cref="HandlerKind.Query"/>,
/// <see cref="HandlerKind.Command"/>, or <see cref="HandlerKind.BareCommand"/> in practice: a notification
/// handler is never restated via <c>InterlocutorGeneratedHandlerAttribute</c>, since it never participates in
/// the missing-handler diagnostic this snapshot exists for.
/// </param>
/// <param name="RequestTypeName">The request type's fully-qualified (<c>global::</c>-prefixed) type name.</param>
/// <param name="ResponseTypeName">
/// The response (or, for a bare-command handler, result) type's fully-qualified (<c>global::</c>-prefixed) type
/// name. Every handler kind this snapshot actually represents carries one, so this is <see langword="null"/>
/// only if the referenced assembly's attribute metadata itself recorded no response type - a shape this
/// generator does not currently emit for any handler it restates this way, but the reader does not assume that
/// and treats a <see langword="null"/> value defensively rather than as an error.
/// </param>
[DebuggerDisplay("{Kind} {RequestTypeName}")]
internal sealed record ExternalHandlerInfo(HandlerKind Kind, string RequestTypeName, string? ResponseTypeName);
