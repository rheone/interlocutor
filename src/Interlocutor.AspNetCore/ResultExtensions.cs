namespace Interlocutor.AspNetCore;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Pipeline.Mapping;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Converts Interlocutor <c>Outcome</c> unions to ASP.NET Core result types.
/// Provides both controller-based (<see cref="IActionResult"/>) and minimal API mappings, both delegating to
/// an <see cref="IOutcomeStatusMapper"/> for the failure-to-status-code decision so the two cannot drift.
/// </summary>
/// <remarks>
/// <para>
/// The dedicated, per-case-arity <c>ToActionResult</c>/<c>ToMinimalApiResult</c> overloads (the non-generic
/// <c>Outcome</c> and every <c>Outcome&lt;TSuccess, ...&gt;</c> arity) are source-generated into a second
/// partial declaration by <c>Interlocutor.Generator</c>'s <c>ResultExtensionsGenerator</c>, marked via
/// <see cref="GenerateResultExtensionsAttribute"/> above - see that attribute's remarks. This file keeps only
/// the reflective catch-all overload (for a hand-rolled <see cref="IOutcome"/> implementation) and the
/// private <see cref="OutcomeHttpMapping"/> rendering helpers both partials share.
/// </para>
/// <para>
/// The type's members are ordered public-surface-first (the catch-all <c>extension</c> block, then the
/// private <see cref="OutcomeHttpMapping"/> one) then private implementation last (fields, then the private
/// helpers) rather than the usual fields-first canonical order - StyleCop.Analyzers 1.2.0-beta.556 (this
/// repo's pinned version) doesn't classify C# 14 extension block declarations for its SA1201 member-ordering
/// check, so it misreports "A &lt;unknown&gt; should not follow a field/method" whenever an extension block
/// sits immediately after a field or method. The diagnostic carries no source location, so it can't be
/// suppressed per-line or per-block; this ordering simply never produces that adjacency, keeping SA1201 fully
/// live (not switched off) for the rest of the file. Tracked as
/// .scratch/extension-members/issues/06-stylecop-extension-block-support.md - revisit this ordering once
/// StyleCop.Analyzers ships extension-block support.
/// </para>
/// </remarks>
[GenerateResultExtensions]
public static partial class ResultExtensions
{
    extension<TResponse>(TResponse response)
        where TResponse : IOutcome
    {
        /// <summary>
        /// Converts an arbitrary <see cref="IOutcome"/> to an <see cref="IActionResult"/> with the
        /// appropriate HTTP status code.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Unlike the dedicated arity overloads above, this overload is constrained only to
        /// <see cref="IOutcome"/>, so it works for a hand-rolled response type as well as any of the
        /// built-in <c>Outcome</c> union arities. It uses <paramref name="caseRegistry"/> to extract the
        /// active case at runtime, then dispatches to <paramref name="mapper"/> reflectively for failures - the
        /// dedicated overloads above dispatch to the same mapper via a compile-time <c>switch</c> instead.
        /// </para>
        /// <para>
        /// The dedicated overloads distinguish success from failure at compile time, via a <c>switch</c> against
        /// the statically known <c>TSuccess</c>/<c>TCaseN</c> type parameters. This overload has no such static
        /// information, so it classifies the extracted case against a small set of recognized shapes instead:
        /// the bare <see cref="Success"/> marker (success), a resolved case whose payload is legitimately
        /// <see langword="null"/> (success - see <see cref="ExtractCase{TResponse}"/>'s remarks), or one of the
        /// built-in failure case types/interfaces <see cref="DefaultHttpOutcomeStatusMapper"/> recognizes
        /// (failure). A case that matches none of those - notably a hand-rolled or generic response type whose
        /// success case carries some other, unrecognized payload type - throws
        /// <see cref="OutcomeCaseNotMappableException"/> rather than guessing: unlike a failure value the
        /// mapper doesn't recognize (which always resolves to some status code via <c>MapUnrecognized</c>),
        /// there is no default HTTP status for "don't know if this is even a failure."
        /// </para>
        /// <para>
        /// C# overload resolution always prefers a dedicated overload over this one for the six built-in union
        /// arities: a constructed generic formal parameter (e.g. <c>Outcome&lt;TSuccess, TFailure&gt;</c>)
        /// is more specific than this overload's bare type-parameter formal parameter (<typeparamref name="TResponse"/>),
        /// so callers get the faster, compile-time-checked path automatically and only pay for reflection when
        /// this overload is genuinely the only option.
        /// </para>
        /// <para>
        /// <paramref name="caseRegistry"/> has no default value and is declared before <paramref name="mapper"/>
        /// (which keeps its default), even though it is the second concern conceptually: C# overload resolution
        /// only prefers a "more specific" candidate over a "less specific" one when every other tie-break,
        /// including the number of parameters actually supplied, is equal. A three-parameter catch-all with two
        /// trailing optional parameters is a strictly worse (higher-arity) match than a dedicated overload's
        /// single optional <c>mapper</c> parameter for a bare <c>result.ToActionResult()</c> call, so the compiler
        /// cannot get far enough to apply the specificity tie-break at all and reports the call ambiguous instead
        /// - see the arity-2/arity-5/void overload-resolution regression tests for the failure this parameter
        /// order avoids. Requiring <paramref name="caseRegistry"/> keeps every dedicated overload's zero- and
        /// one-argument call shapes resolvable only against the dedicated overload, while this overload remains
        /// reachable by name (<c>caseRegistry: myRegistry</c>) for the hand-rolled types it exists for.
        /// </para>
        /// </remarks>
        /// <param name="caseRegistry">
        /// The registry used to extract <paramref name="response"/>'s active case. Pass
        /// <see langword="null"/> (or <see cref="DefaultOutcomeCaseRegistry.Instance"/> explicitly) to use the
        /// default registry, which handles any compiler-generated union but throws for a hand-rolled
        /// <see cref="IOutcome"/> implementation unless a custom registry covering it is supplied.
        /// </param>
        /// <param name="mapper">
        /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
        /// <see cref="DefaultHttpOutcomeStatusMapper"/> is used, preserving the dedicated overloads' status codes.
        /// </param>
        /// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
        public IActionResult ToActionResult(IOutcomeCaseRegistry? caseRegistry, IOutcomeStatusMapper? mapper = null)
        {
            var (isFailure, caseType, caseValue) = ExtractCase(response, caseRegistry);
            return isFailure
                ? InvokeMap(mapper ?? DefaultMapper, caseType, caseValue).ToActionResult()
                : new OkObjectResult(caseValue);
        }

        /// <summary>
        /// Converts an arbitrary <see cref="IOutcome"/> to a minimal API result with the appropriate
        /// HTTP status code.
        /// </summary>
        /// <remarks>
        /// See the <see cref="ResultExtensions.ToActionResult{TResponse}(TResponse, IOutcomeCaseRegistry?, IOutcomeStatusMapper?)"/>
        /// remarks - the same reflective case-extraction and mapper dispatch, and the same overload-resolution
        /// reasoning for why <paramref name="caseRegistry"/> is required and ordered before <paramref name="mapper"/>,
        /// apply here.
        /// </remarks>
        /// <param name="caseRegistry">
        /// The registry used to extract <paramref name="response"/>'s active case. Pass
        /// <see langword="null"/> (or <see cref="DefaultOutcomeCaseRegistry.Instance"/> explicitly) to use the
        /// default registry, which handles any compiler-generated union but throws for a hand-rolled
        /// <see cref="IOutcome"/> implementation unless a custom registry covering it is supplied.
        /// </param>
        /// <param name="mapper">
        /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
        /// <see cref="DefaultHttpOutcomeStatusMapper"/> is used, preserving the dedicated overloads' status codes.
        /// </param>
        /// <returns>An HTTP 200 with the success value on success, or the mapped error result on failure.</returns>
        public IResult ToMinimalApiResult(IOutcomeCaseRegistry? caseRegistry, IOutcomeStatusMapper? mapper = null)
        {
            var (isFailure, caseType, caseValue) = ExtractCase(response, caseRegistry);
            return isFailure
                ? InvokeMap(mapper ?? DefaultMapper, caseType, caseValue).ToMinimalApiResult()
                : Results.Ok(caseValue);
        }
    }

    extension(OutcomeHttpMapping mapping)
    {
        /// <summary>Renders an <see cref="OutcomeHttpMapping"/> as an <see cref="IActionResult"/> for controller-based endpoints.</summary>
        private IActionResult ToActionResult() =>
            mapping.Body is null
                ? new StatusCodeResult(mapping.StatusCode)
                : new ObjectResult(mapping.Body) { StatusCode = mapping.StatusCode };

        /// <summary>Renders an <see cref="OutcomeHttpMapping"/> as an <see cref="IResult"/> for minimal API endpoints.</summary>
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:RequiresUnreferencedCode",
            Justification = "Results.Json serializes an arbitrary failure body reflectively - a real AOT caveat, not "
                + "eliminated here. Documented in documentation/trim-and-aot-support.md: a consumer publishing "
                + "trimmed/AOT should configure a System.Text.Json source-generated JsonSerializerContext for their "
                + "own failure/success types via ConfigureHttpJsonOptions, which Results.Json already picks up."
        )]
        private IResult ToMinimalApiResult() =>
            mapping.Body is null
                ? Results.StatusCode(mapping.StatusCode)
                : Results.Json(mapping.Body, statusCode: mapping.StatusCode);
    }

    /// <summary>The mapper applied when a caller doesn't supply one, matching the library's built-in status-code choices.</summary>
    private static readonly IOutcomeStatusMapper DefaultMapper = new DefaultHttpOutcomeStatusMapper();

    /// <summary>
    /// The open <see cref="IOutcomeMapper{TResult}.Map{TFailure}"/> generic method definition, closed per
    /// failure <see cref="Type"/> below.
    /// </summary>
    /// <remarks>
    /// Looked up via <see cref="IOutcomeMapper{TResult}"/> - the interface that actually declares
    /// <c>Map</c> - rather than <see cref="IOutcomeStatusMapper"/>: <see cref="Type.GetMethod(string)"/> on an
    /// interface only finds members declared directly on that interface, not ones it inherits from a base
    /// interface, so looking this up via <see cref="IOutcomeStatusMapper"/> would return <see langword="null"/>.
    /// The resulting <see cref="MethodInfo"/> still invokes correctly against any <see cref="IOutcomeStatusMapper"/>
    /// instance below, since every such instance implements <see cref="IOutcomeMapper{TResult}"/> too.
    /// </remarks>
    private static readonly MethodInfo MapMethodDefinition = typeof(IOutcomeMapper<OutcomeHttpMapping>).GetMethod(
        nameof(IOutcomeMapper<OutcomeHttpMapping>.Map)
    )!;

    // The catch-all overloads below don't know the active case's type at compile time - only IOutcomeCaseRegistry
    // reveals it, reflectively, at the call site. Resolving IOutcomeStatusMapper.Map<TFailure> for that runtime
    // Type via reflection on every call would make the catch-all far slower than the dedicated arity overloads
    // (which resolve their Map<TFailure> call at compile time). Caching the closed MethodInfo per failure Type,
    // for the lifetime of the process, keeps repeated calls for the same failure type cheap after the first.
    private static readonly ConcurrentDictionary<Type, MethodInfo> MapMethodCache = new();

    /// <summary>
    /// Extracts <paramref name="response"/>'s active case via <paramref name="caseRegistry"/> (or the default
    /// registry) and determines whether it should be mapped as a failure.
    /// </summary>
    /// <typeparam name="TResponse">The <see cref="IOutcome"/> type to extract from.</typeparam>
    /// <param name="response">The response to extract the active case from.</param>
    /// <param name="caseRegistry">The registry to use, or <see langword="null"/> to use the default registry.</param>
    /// <returns>Whether the active case is a failure, its runtime type, and its value.</returns>
    /// <exception cref="OutcomeCaseNotMappableException">
    /// The active case is a non-<see langword="null"/> value that is neither the bare <see cref="Success"/>
    /// marker nor one of the built-in failure case types/interfaces <see cref="DefaultHttpOutcomeStatusMapper"/>
    /// recognizes (see <see cref="IsRecognizedFailureCase"/>) - there is no way to tell whether it represents
    /// success or failure, so no HTTP status can be chosen for it without guessing.
    /// </exception>
    private static (bool IsFailure, Type CaseType, object? CaseValue) ExtractCase<TResponse>(
        TResponse response,
        IOutcomeCaseRegistry? caseRegistry
    )
        where TResponse : IOutcome
    {
        var registry = caseRegistry ?? DefaultOutcomeCaseRegistry.Instance;
        var found = registry.TryGetActiveCase(response, out var caseType, out var caseValue);

        if (!found || caseValue is Success)
        {
            // !found: no case at all (TryGetActiveCase only returns false for a null response - see its own
            // remarks) - fail-closed, same policy the retired outcome classifier applied, since there is
            // nothing here to call a success. caseValue is Success: the bare marker is unambiguous.
            return (!found, caseType, caseValue);
        }

        if (caseValue is null)
        {
            // A *resolved* active case whose value is legitimately null is ambiguous on its own - a null
            // success payload and a null failure payload look identical once extracted, and there is no
            // per-instance success/failure signal left to ask (IOutcomeCase exposes only ActiveCaseType, not
            // a success/failure judgment - see outcome-classifier-retirement). Defaults to success: the far
            // more common shape in practice, and the one existing tests exercise (e.g. Outcome<string?, ...>
            // with a null success payload).
            return (false, caseType, caseValue);
        }

        if (IsRecognizedFailureCase(caseValue))
        {
            return (true, caseType, caseValue);
        }

        // Genuinely unmappable: caseType is neither Success nor a recognized failure shape. Unlike a
        // recognized-but-untaught failure value (which DefaultHttpOutcomeStatusMapper still maps, to 500, via
        // MapUnrecognized), this method cannot even tell whether the case represents success or failure, so
        // guessing either way risks silently misreporting one for the other - see this method's own
        // <exception> doc and OutcomeCaseNotMappableException's remarks for why that guess is refused rather
        // than made fail-closed.
        throw new OutcomeCaseNotMappableException(
            typeof(TResponse),
            caseType,
            $"Cannot determine an HTTP status for {typeof(TResponse).Name}'s active case: {caseType.Name} is "
                + $"neither the bare {nameof(Success)} marker nor one of the built-in failure case types/interfaces "
                + $"{nameof(DefaultHttpOutcomeStatusMapper)} recognizes, so this reflective catch-all cannot tell "
                + $"whether {caseType.Name} represents success or failure - unlike a *recognized* failure type this "
                + $"mapper wasn't taught, {nameof(IOutcomeStatusMapper)} is never even consulted here, since that "
                + $"question is settled before any mapper (default or custom) is selected. Call the dedicated "
                + $"Outcome<...> overload against the concrete type instead: it resolves success-versus-failure at "
                + $"compile time, so {caseType.Name} reaches {nameof(IOutcomeStatusMapper)}.Map directly whenever "
                + $"it is the failure case - customizable there via a custom mapper's MapUnrecognized override, "
                + $"with no classification ambiguity to hit in the first place."
        );
    }

    /// <summary>
    /// Reports whether <paramref name="caseValue"/> is one of the failure case types/interfaces
    /// <see cref="DefaultHttpOutcomeStatusMapper"/> recognizes by name (mirroring
    /// <see cref="Interlocutor.Abstractions.Pipeline.Mapping.OutcomeMapperBase{TResult}.Map{TFailure}"/>'s own
    /// dispatch branches, minus its always-succeeding <c>MapUnrecognized</c> fallback - that fallback is exactly
    /// the guess <see cref="ExtractCase{TResponse}"/> refuses to make on <paramref name="caseValue"/>'s behalf).
    /// </summary>
    /// <remarks>
    /// This list is necessarily separate from <c>OutcomeMapperBase.Map{TFailure}</c>'s own switch, not merely
    /// duplicated by oversight: that switch's job is "map this already-known failure to a status", and it always
    /// succeeds (falling through to <c>MapUnrecognized</c>) - it has no way to answer "would you actually
    /// recognize this, or just shrug and return your generic default", which is the question this method exists
    /// to answer instead, before any mapper is even selected. Adding a new built-in failure case (or a new
    /// recognized <c>I*FailureCase</c> interface) to <c>OutcomeMapperBase</c> should add the matching branch
    /// here too, or this reflective catch-all starts throwing <see cref="OutcomeCaseNotMappableException"/> for a
    /// type the dedicated arity overloads already map correctly.
    /// </remarks>
    /// <param name="caseValue">The non-<see langword="null"/> case value to check.</param>
    /// <returns><see langword="true"/> if <paramref name="caseValue"/> is a recognized failure shape.</returns>
    private static bool IsRecognizedFailureCase(object caseValue) =>
        caseValue switch
        {
            ValidationErrors or IValidationFailureCase => true,
            Forbidden or IForbiddenFailureCase => true,
            Conflict => true,
            Failures or Failure => true,
            NotFound => true,
            Cancelled => true,
            Timeout or ITimeoutFailureCase => true,
            RateLimited or IRateLimitedFailureCase => true,
            _ => caseValue.GetType() is { IsGenericType: true } caseType
                && caseType.GetGenericTypeDefinition() == typeof(NotFound<>),
        };

    /// <summary>
    /// Invokes <see cref="IOutcomeMapper{TResult}.Map{TFailure}"/> reflectively for a failure value whose static
    /// type is unknown at compile time, caching the closed generic <see cref="MethodInfo"/> per <paramref name="failureType"/>.
    /// </summary>
    /// <param name="mapper">The mapper to invoke.</param>
    /// <param name="failureType">The failure value's runtime type.</param>
    /// <param name="failureValue">The failure value.</param>
    /// <returns>The mapped <see cref="OutcomeHttpMapping"/>.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2060:MakeGenericMethod",
        Justification = "Safe when trimming, NOT when compiling ahead of time. TFailure is always a failure "
            + "case type the caller's own code already constructed to produce this outcome, so the trimmer "
            + "keeps it and its Map<TFailure> instantiation reachable. That argument does not extend to "
            + "ahead-of-time compilation: every case type is a readonly record struct, and value-type generic "
            + "instantiations are not shared, so MakeGenericMethod needs a JIT here. This package is marked "
            + "IsTrimmable, deliberately not IsAotCompatible, for exactly that reason - see "
            + "maintainer_docs/research/native-aot-and-trimming-options.md."
    )]
    private static OutcomeHttpMapping InvokeMap(IOutcomeStatusMapper mapper, Type failureType, object? failureValue)
    {
        var closedMapMethod = MapMethodCache.GetOrAdd(failureType, static ft => MapMethodDefinition.MakeGenericMethod(ft));
        return (OutcomeHttpMapping)closedMapMethod.Invoke(mapper, [failureValue])!;
    }
}
