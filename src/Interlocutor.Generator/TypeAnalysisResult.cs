namespace Interlocutor.Generator;

using System.Collections.Immutable;

/// <summary>
/// What <see cref="HandlerTypeAnalyzer"/> found for one candidate type declaration: zero or more handler
/// contracts it implements, and - independently - the request marker it implements, if any.
/// </summary>
/// <remarks>
/// A single type is never both a handler and a request in practice, but the two facts are computed and
/// carried independently so the generator's two concerns (registration/dispatch emission and the
/// missing-handler diagnostic) stay decoupled from how discovery happens to be batched.
/// </remarks>
/// <param name="Handlers">The handler-contract matches this type declaration produced, if any.</param>
/// <param name="Request">The request-marker match this type declaration produced, or <see langword="null"/>.</param>
internal sealed record TypeAnalysisResult(ImmutableArray<HandlerModel> Handlers, RequestModel? Request)
{
    /// <summary>The shared empty result for a candidate that turned out to be neither a handler nor a request.</summary>
    public static readonly TypeAnalysisResult Empty = new([], null);
}
