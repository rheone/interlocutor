namespace Interlocutor.Generator;

using System.Collections.Immutable;
using System.Linq;

/// <summary>
/// What <see cref="HandlerTypeAnalyzer"/> found for one candidate type declaration: zero or more closed handler
/// contracts it implements, zero or more open generic handler contracts it implements, and - independently -
/// the request marker it implements, if any.
/// </summary>
/// <remarks>
/// A single type is never both a handler and a request in practice, but the two facts are computed and
/// carried independently so the generator's two concerns (registration/dispatch emission and the
/// missing-handler diagnostic) stay decoupled from how discovery happens to be batched.
/// </remarks>
/// <param name="Handlers">The closed handler-contract matches this type declaration produced, if any.</param>
/// <param name="OpenGenericHandlers">The open generic handler-contract matches this type declaration produced, if any (see <see cref="OpenGenericHandlerModel"/>).</param>
/// <param name="Request">The request-marker match this type declaration produced, or <see langword="null"/>.</param>
internal sealed record TypeAnalysisResult(
    ImmutableArray<HandlerModel> Handlers,
    ImmutableArray<OpenGenericHandlerModel> OpenGenericHandlers,
    RequestModel? Request
)
{
    /// <summary>The shared empty result for a candidate that turned out to be neither a handler nor a request.</summary>
    public static readonly TypeAnalysisResult Empty = new([], [], null);

    /// <inheritdoc/>
    /// <remarks>
    /// Overridden for the same reason as <see cref="CompilationInfo.Equals(CompilationInfo?)"/>:
    /// <see cref="ImmutableArray{T}"/>'s own <c>Equals</c> compares its backing array by reference, not by
    /// element, so the compiler-generated positional-record equality this type would otherwise get from
    /// <see cref="Handlers"/>/<see cref="OpenGenericHandlers"/> is useless for incremental-pipeline caching
    /// (ticket 53) - a syntax-provider re-run whose per-candidate result is content-identical to the previous
    /// run's would otherwise never compare equal, defeating <c>Collect()</c>'s own caching for the whole
    /// candidate list.
    /// </remarks>
    public bool Equals(TypeAnalysisResult? other) =>
        other is not null
        && this.Handlers.SequenceEqual(other.Handlers)
        && this.OpenGenericHandlers.SequenceEqual(other.OpenGenericHandlers)
        && this.Request == other.Request;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // netstandard2.0 (this project's target - see ADR-0008) has no System.HashCode, so combine by hand
        // rather than pull in a polyfill package for one method.
        var hash = 17;
        foreach (var handler in this.Handlers)
        {
            hash = (hash * 31) + handler.GetHashCode();
        }

        foreach (var handler in this.OpenGenericHandlers)
        {
            hash = (hash * 31) + handler.GetHashCode();
        }

        hash = (hash * 31) + (this.Request?.GetHashCode() ?? 0);
        return hash;
    }
}
