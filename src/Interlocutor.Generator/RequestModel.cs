namespace Interlocutor.Generator;

using Microsoft.CodeAnalysis;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one discovered request type (a type implementing
/// <c>IQuery&lt;TResponse&gt;</c>, <c>ICommand&lt;TResponse&gt;</c>, or <c>ICommand</c>) — the input to the
/// missing-handler diagnostic.
/// </summary>
/// <param name="Kind">Which request marker interface <see cref="RequestTypeName"/>'s type implements.</param>
/// <param name="RequestTypeName">The request type's fully-qualified (<c>global::</c>-prefixed) type name.</param>
/// <param name="ResponseTypeName">
/// The response type's fully-qualified (<c>global::</c>-prefixed) type name, or <see langword="null"/> for
/// <see cref="RequestKind.VoidCommand"/> — a unit command's result type is chosen by the caller, not declared
/// on the request, so matching ignores it (see <see cref="RequestKind.VoidCommand"/>).
/// </param>
/// <param name="Location">
/// The request type declaration's identifier location, used to anchor the missing-handler diagnostic at the
/// request type itself rather than at the compilation as a whole.
/// </param>
internal sealed record RequestModel(RequestKind Kind, string RequestTypeName, string? ResponseTypeName, Location Location);
