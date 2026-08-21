namespace Interlocutor.Generator;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one query, command, or void-command handler declared
/// by a referenced assembly's <see cref="ExternalHandlerInfo"/>-emitting generator run, read back from that
/// assembly's <c>InterlocutorGeneratedHandlerAttribute</c> metadata (ticket 12) rather than re-discovered by
/// walking the referenced assembly's types.
/// </summary>
/// <param name="Kind">Which handler contract the referenced assembly's handler implements.</param>
/// <param name="RequestTypeName">The request type's fully-qualified (<c>global::</c>-prefixed) type name.</param>
/// <param name="ResponseTypeName">
/// The response type's fully-qualified (<c>global::</c>-prefixed) type name, or <see langword="null"/> for a
/// void-command handler (mirrors <see cref="HandlerModel.ResponseTypeName"/>'s void-command convention).
/// </param>
internal sealed record ExternalHandlerInfo(HandlerKind Kind, string RequestTypeName, string? ResponseTypeName);
