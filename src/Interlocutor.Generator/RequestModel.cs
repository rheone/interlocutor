namespace Interlocutor.Generator;

using Microsoft.CodeAnalysis;

/// <summary>
/// An equatable, incremental-pipeline-safe snapshot of one discovered request type (a type implementing
/// <c>IQuery&lt;TResponse&gt;</c>, <c>ICommand&lt;TResponse&gt;</c>, or <c>ICommand</c>) - the input to the
/// missing-handler diagnostic.
/// </summary>
/// <param name="Kind">Which request marker interface <see cref="RequestTypeName"/>'s type implements.</param>
/// <param name="RequestTypeName">The request type's fully-qualified (<c>global::</c>-prefixed) type name.</param>
/// <param name="ResponseTypeName">
/// The response type's fully-qualified (<c>global::</c>-prefixed) type name, or <see langword="null"/> for
/// <see cref="RequestKind.VoidCommand"/> - a unit command's result type is chosen by the caller, not declared
/// on the request, so matching ignores it (see <see cref="RequestKind.VoidCommand"/>).
/// </param>
/// <param name="Location">
/// The request type declaration's identifier location, used to anchor the missing-handler diagnostic at the
/// request type itself rather than at the compilation as a whole.
/// </param>
/// <remarks>
/// <see cref="Equals(RequestModel?)"/>/<see cref="GetHashCode"/> deliberately exclude <see cref="Location"/>
/// from equality (ticket 53) - see <see cref="HandlerModel.Equals(HandlerModel?)"/>'s remarks for the full
/// rationale: <see cref="Microsoft.CodeAnalysis.Location"/> compares the declaring
/// <see cref="Microsoft.CodeAnalysis.SyntaxTree"/> by reference, which changes on every re-parse regardless of
/// whether this request declaration itself changed, which would otherwise defeat incremental-pipeline caching
/// for reasons unrelated to this request's own content.
/// </remarks>
internal sealed record RequestModel(RequestKind Kind, string RequestTypeName, string? ResponseTypeName, Location Location)
{
    /// <inheritdoc/>
    public bool Equals(RequestModel? other) =>
        other is not null
        && this.Kind == other.Kind
        && this.RequestTypeName == other.RequestTypeName
        && this.ResponseTypeName == other.ResponseTypeName;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // netstandard2.0 (this project's target - see ADR-0008) has no System.HashCode, so combine by hand
        // rather than pull in a polyfill package for one method. Location is deliberately excluded - see
        // this type's own remarks.
        var hash = 17;
        hash = (hash * 31) + this.Kind.GetHashCode();
        hash = (hash * 31) + this.RequestTypeName.GetHashCode();
        hash = (hash * 31) + (this.ResponseTypeName?.GetHashCode() ?? 0);
        return hash;
    }
}
