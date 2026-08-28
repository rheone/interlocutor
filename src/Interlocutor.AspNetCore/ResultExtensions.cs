namespace Interlocutor.AspNetCore;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Converts Interlocutor <c>OperationOutcome</c> unions to ASP.NET Core result types.
/// Provides both controller-based (<see cref="IActionResult"/>) and minimal API mappings, both delegating to
/// an <see cref="IOutcomeStatusMapper"/> for the failure-to-status-code decision so the two cannot drift.
/// </summary>
public static class ResultExtensions
{
    /// <summary>The mapper applied when a caller doesn't supply one, matching the library's built-in status-code choices.</summary>
    private static readonly IOutcomeStatusMapper DefaultMapper = new DefaultOutcomeStatusMapper();

    /// <summary>The open <see cref="IOutcomeStatusMapper.Map{TFailure}"/> generic method definition, closed per failure <see cref="Type"/> below.</summary>
    private static readonly MethodInfo MapMethodDefinition = typeof(IOutcomeStatusMapper).GetMethod(
        nameof(IOutcomeStatusMapper.Map)
    )!;

    // The catch-all overloads below don't know the active case's type at compile time - only IOutcomeCaseRegistry
    // reveals it, reflectively, at the call site. Resolving IOutcomeStatusMapper.Map<TFailure> for that runtime
    // Type via reflection on every call would make the catch-all far slower than the dedicated arity overloads
    // (which resolve their Map<TFailure> call at compile time). Caching the closed MethodInfo per failure Type,
    // for the lifetime of the process, keeps repeated calls for the same failure type cheap after the first.
    private static readonly ConcurrentDictionary<Type, MethodInfo> MapMethodCache = new();

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TFailureResult}"/> to an <see cref="IActionResult"/>
    /// with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TFailure">The failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult<TSuccess, TFailure>(
        this OperationOutcome<TSuccess, TFailure> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => new OkObjectResult(ok),
            TFailure failure => (mapper ?? DefaultMapper).Map(failure).ToActionResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>Converts a void <see cref="OperationOutcome"/> to an <see cref="IActionResult"/> with the appropriate HTTP status code.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>A <see cref="NoContentResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult(this OperationOutcome result, IOutcomeStatusMapper? mapper = null) =>
        result.Value switch
        {
            Success => new NoContentResult(),
            Failure failure => (mapper ?? DefaultMapper).Map(failure).ToActionResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3}"/> to an <see cref="IActionResult"/>
    /// with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TCase2">The first alternative failure case type.</typeparam>
    /// <typeparam name="TCase3">The second alternative failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult<TSuccess, TCase2, TCase3>(
        this OperationOutcome<TSuccess, TCase2, TCase3> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => new OkObjectResult(ok),
            TCase2 case2 => (mapper ?? DefaultMapper).Map(case2).ToActionResult(),
            TCase3 case3 => (mapper ?? DefaultMapper).Map(case3).ToActionResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4}"/> to an <see cref="IActionResult"/>
    /// with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TCase2">The first alternative failure case type.</typeparam>
    /// <typeparam name="TCase3">The second alternative failure case type.</typeparam>
    /// <typeparam name="TCase4">The third alternative failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult<TSuccess, TCase2, TCase3, TCase4>(
        this OperationOutcome<TSuccess, TCase2, TCase3, TCase4> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => new OkObjectResult(ok),
            TCase2 case2 => (mapper ?? DefaultMapper).Map(case2).ToActionResult(),
            TCase3 case3 => (mapper ?? DefaultMapper).Map(case3).ToActionResult(),
            TCase4 case4 => (mapper ?? DefaultMapper).Map(case4).ToActionResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4, TCase5}"/> to an
    /// <see cref="IActionResult"/> with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TCase2">The first alternative failure case type.</typeparam>
    /// <typeparam name="TCase3">The second alternative failure case type.</typeparam>
    /// <typeparam name="TCase4">The third alternative failure case type.</typeparam>
    /// <typeparam name="TCase5">The fourth alternative failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult<TSuccess, TCase2, TCase3, TCase4, TCase5>(
        this OperationOutcome<TSuccess, TCase2, TCase3, TCase4, TCase5> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => new OkObjectResult(ok),
            TCase2 case2 => (mapper ?? DefaultMapper).Map(case2).ToActionResult(),
            TCase3 case3 => (mapper ?? DefaultMapper).Map(case3).ToActionResult(),
            TCase4 case4 => (mapper ?? DefaultMapper).Map(case4).ToActionResult(),
            TCase5 case5 => (mapper ?? DefaultMapper).Map(case5).ToActionResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6}"/> to an
    /// <see cref="IActionResult"/> with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TCase2">The first alternative failure case type.</typeparam>
    /// <typeparam name="TCase3">The second alternative failure case type.</typeparam>
    /// <typeparam name="TCase4">The third alternative failure case type.</typeparam>
    /// <typeparam name="TCase5">The fourth alternative failure case type.</typeparam>
    /// <typeparam name="TCase6">The fifth alternative failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6>(
        this OperationOutcome<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => new OkObjectResult(ok),
            TCase2 case2 => (mapper ?? DefaultMapper).Map(case2).ToActionResult(),
            TCase3 case3 => (mapper ?? DefaultMapper).Map(case3).ToActionResult(),
            TCase4 case4 => (mapper ?? DefaultMapper).Map(case4).ToActionResult(),
            TCase5 case5 => (mapper ?? DefaultMapper).Map(case5).ToActionResult(),
            TCase6 case6 => (mapper ?? DefaultMapper).Map(case6).ToActionResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TFailureResult}"/> to a minimal API result
    /// with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TFailure">The failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used - the same instance the controller-based
    /// <see cref="ToActionResult{TSuccess, TFailure}(OperationOutcome{TSuccess, TFailure}, IOutcomeStatusMapper?)"/>
    /// defaults to, so the two paths cannot drift.
    /// </param>
    /// <returns>An HTTP 200 with the success value on success, or the mapped error result on failure.</returns>
    public static IResult ToMinimalApiResult<TSuccess, TFailure>(
        this OperationOutcome<TSuccess, TFailure> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => Results.Ok(ok),
            TFailure failure => (mapper ?? DefaultMapper).Map(failure).ToMinimalApiResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>Converts a void <see cref="OperationOutcome"/> to a minimal API result with the appropriate HTTP status code.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>HTTP 204 No Content on success, or the mapped error result on failure.</returns>
    public static IResult ToMinimalApiResult(this OperationOutcome result, IOutcomeStatusMapper? mapper = null) =>
        result.Value switch
        {
            Success => Results.NoContent(),
            Failure failure => (mapper ?? DefaultMapper).Map(failure).ToMinimalApiResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3}"/> to a minimal API result
    /// with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TCase2">The first alternative failure case type.</typeparam>
    /// <typeparam name="TCase3">The second alternative failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>An HTTP 200 with the success value on success, or the mapped error result on failure.</returns>
    public static IResult ToMinimalApiResult<TSuccess, TCase2, TCase3>(
        this OperationOutcome<TSuccess, TCase2, TCase3> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => Results.Ok(ok),
            TCase2 case2 => (mapper ?? DefaultMapper).Map(case2).ToMinimalApiResult(),
            TCase3 case3 => (mapper ?? DefaultMapper).Map(case3).ToMinimalApiResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4}"/> to a minimal API result
    /// with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TCase2">The first alternative failure case type.</typeparam>
    /// <typeparam name="TCase3">The second alternative failure case type.</typeparam>
    /// <typeparam name="TCase4">The third alternative failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>An HTTP 200 with the success value on success, or the mapped error result on failure.</returns>
    public static IResult ToMinimalApiResult<TSuccess, TCase2, TCase3, TCase4>(
        this OperationOutcome<TSuccess, TCase2, TCase3, TCase4> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => Results.Ok(ok),
            TCase2 case2 => (mapper ?? DefaultMapper).Map(case2).ToMinimalApiResult(),
            TCase3 case3 => (mapper ?? DefaultMapper).Map(case3).ToMinimalApiResult(),
            TCase4 case4 => (mapper ?? DefaultMapper).Map(case4).ToMinimalApiResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4, TCase5}"/> to a minimal API
    /// result with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TCase2">The first alternative failure case type.</typeparam>
    /// <typeparam name="TCase3">The second alternative failure case type.</typeparam>
    /// <typeparam name="TCase4">The third alternative failure case type.</typeparam>
    /// <typeparam name="TCase5">The fourth alternative failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>An HTTP 200 with the success value on success, or the mapped error result on failure.</returns>
    public static IResult ToMinimalApiResult<TSuccess, TCase2, TCase3, TCase4, TCase5>(
        this OperationOutcome<TSuccess, TCase2, TCase3, TCase4, TCase5> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => Results.Ok(ok),
            TCase2 case2 => (mapper ?? DefaultMapper).Map(case2).ToMinimalApiResult(),
            TCase3 case3 => (mapper ?? DefaultMapper).Map(case3).ToMinimalApiResult(),
            TCase4 case4 => (mapper ?? DefaultMapper).Map(case4).ToMinimalApiResult(),
            TCase5 case5 => (mapper ?? DefaultMapper).Map(case5).ToMinimalApiResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts a <see cref="OperationOutcome{TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6}"/> to a
    /// minimal API result with the appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TCase2">The first alternative failure case type.</typeparam>
    /// <typeparam name="TCase3">The second alternative failure case type.</typeparam>
    /// <typeparam name="TCase4">The third alternative failure case type.</typeparam>
    /// <typeparam name="TCase5">The fourth alternative failure case type.</typeparam>
    /// <typeparam name="TCase6">The fifth alternative failure case type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving today's status codes.
    /// </param>
    /// <returns>An HTTP 200 with the success value on success, or the mapped error result on failure.</returns>
    public static IResult ToMinimalApiResult<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6>(
        this OperationOutcome<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6> result,
        IOutcomeStatusMapper? mapper = null
    ) =>
        result.Value switch
        {
            TSuccess ok => Results.Ok(ok),
            TCase2 case2 => (mapper ?? DefaultMapper).Map(case2).ToMinimalApiResult(),
            TCase3 case3 => (mapper ?? DefaultMapper).Map(case3).ToMinimalApiResult(),
            TCase4 case4 => (mapper ?? DefaultMapper).Map(case4).ToMinimalApiResult(),
            TCase5 case5 => (mapper ?? DefaultMapper).Map(case5).ToMinimalApiResult(),
            TCase6 case6 => (mapper ?? DefaultMapper).Map(case6).ToMinimalApiResult(),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Converts an arbitrary <see cref="IOperationOutcome"/> to an <see cref="IActionResult"/> with the
    /// appropriate HTTP status code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike the dedicated arity overloads above, this overload is constrained only to
    /// <see cref="IOperationOutcome"/>, so it works for a hand-rolled response type as well as any of the
    /// built-in <c>OperationOutcome</c> union arities. It uses <paramref name="caseRegistry"/> to extract the
    /// active case at runtime, then dispatches to <paramref name="mapper"/> reflectively for failures - the
    /// dedicated overloads above dispatch to the same mapper via a compile-time <c>switch</c> instead.
    /// </para>
    /// <para>
    /// The dedicated overloads distinguish success from failure at compile time, via a <c>switch</c> against
    /// the statically known <c>TSuccess</c>/<c>TCaseN</c> type parameters. This overload has no such static
    /// information, so it classifies the extracted case via
    /// <see cref="Interlocutor.Abstractions.Pipeline.Classifiers.OutcomeClassifierBuilder.DefaultClassifier"/>
    /// instead - the same classifier <c>AuditingBehavior</c>/<c>UnitOfWorkBehavior</c> use to decide
    /// commit-vs-rollback. It is fail-closed: a case type it has no registration for is treated as a failure.
    /// The built-in kernel failure types (<c>NotFound</c>, <c>Forbidden</c>, <c>Conflict</c>, ...) and the
    /// bare <see cref="Success"/> marker are pre-registered, so a response built from those classifies
    /// correctly with no extra setup; a hand-rolled response type whose success case carries a different,
    /// unregistered payload type needs that type registered with a custom <c>IOutcomeClassifier</c> (built via
    /// <c>OutcomeClassifierBuilder</c>) for this overload to classify it as success rather than failure.
    /// </para>
    /// <para>
    /// C# overload resolution always prefers a dedicated overload over this one for the six built-in union
    /// arities: a constructed generic formal parameter (e.g. <c>OperationOutcome&lt;TSuccess, TFailure&gt;</c>)
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
    /// <typeparam name="TResponse">The <see cref="IOperationOutcome"/> type to convert.</typeparam>
    /// <param name="response">The response to convert.</param>
    /// <param name="caseRegistry">
    /// The registry used to extract <paramref name="response"/>'s active case. Pass
    /// <see langword="null"/> (or <see cref="DefaultOutcomeCaseRegistry.Instance"/> explicitly) to use the
    /// default registry, which handles any compiler-generated union but throws for a hand-rolled
    /// <see cref="IOperationOutcome"/> implementation unless a custom registry covering it is supplied.
    /// </param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving the dedicated overloads' status codes.
    /// </param>
    /// <returns>An <see cref="OkObjectResult"/> on success, or the mapped error result on failure.</returns>
    public static IActionResult ToActionResult<TResponse>(
        this TResponse response,
        IOutcomeCaseRegistry? caseRegistry,
        IOutcomeStatusMapper? mapper = null
    )
        where TResponse : IOperationOutcome
    {
        var (isFailure, caseType, caseValue) = ExtractCase(response, caseRegistry);
        return isFailure
            ? InvokeMap(mapper ?? DefaultMapper, caseType, caseValue).ToActionResult()
            : new OkObjectResult(caseValue);
    }

    /// <summary>
    /// Converts an arbitrary <see cref="IOperationOutcome"/> to a minimal API result with the appropriate
    /// HTTP status code.
    /// </summary>
    /// <remarks>
    /// See the <see cref="ToActionResult{TResponse}(TResponse, IOutcomeCaseRegistry?, IOutcomeStatusMapper?)"/>
    /// remarks - the same reflective case-extraction and mapper dispatch, and the same overload-resolution
    /// reasoning for why <paramref name="caseRegistry"/> is required and ordered before <paramref name="mapper"/>,
    /// apply here.
    /// </remarks>
    /// <typeparam name="TResponse">The <see cref="IOperationOutcome"/> type to convert.</typeparam>
    /// <param name="response">The response to convert.</param>
    /// <param name="caseRegistry">
    /// The registry used to extract <paramref name="response"/>'s active case. Pass
    /// <see langword="null"/> (or <see cref="DefaultOutcomeCaseRegistry.Instance"/> explicitly) to use the
    /// default registry, which handles any compiler-generated union but throws for a hand-rolled
    /// <see cref="IOperationOutcome"/> implementation unless a custom registry covering it is supplied.
    /// </param>
    /// <param name="mapper">
    /// The failure-to-status-code mapper to use. When <see langword="null"/>, the library's built-in
    /// <see cref="DefaultOutcomeStatusMapper"/> is used, preserving the dedicated overloads' status codes.
    /// </param>
    /// <returns>An HTTP 200 with the success value on success, or the mapped error result on failure.</returns>
    public static IResult ToMinimalApiResult<TResponse>(
        this TResponse response,
        IOutcomeCaseRegistry? caseRegistry,
        IOutcomeStatusMapper? mapper = null
    )
        where TResponse : IOperationOutcome
    {
        var (isFailure, caseType, caseValue) = ExtractCase(response, caseRegistry);
        return isFailure ? InvokeMap(mapper ?? DefaultMapper, caseType, caseValue).ToMinimalApiResult() : Results.Ok(caseValue);
    }

    /// <summary>
    /// Extracts <paramref name="response"/>'s active case via <paramref name="caseRegistry"/> (or the default
    /// registry) and classifies it as success or failure via <see cref="OutcomeClassifierBuilder.DefaultClassifier"/>.
    /// </summary>
    /// <typeparam name="TResponse">The <see cref="IOperationOutcome"/> type to extract from.</typeparam>
    /// <param name="response">The response to extract the active case from.</param>
    /// <param name="caseRegistry">The registry to use, or <see langword="null"/> to use the default registry.</param>
    /// <returns>Whether the active case is a failure, its runtime type, and its value.</returns>
    private static (bool IsFailure, Type CaseType, object? CaseValue) ExtractCase<TResponse>(
        TResponse response,
        IOutcomeCaseRegistry? caseRegistry
    )
        where TResponse : IOperationOutcome
    {
        var registry = caseRegistry ?? DefaultOutcomeCaseRegistry.Instance;
        registry.TryGetActiveCase(response, out var caseType, out var caseValue);
        return (OutcomeClassifierBuilder.DefaultClassifier.IsFailure(caseValue), caseType, caseValue);
    }

    /// <summary>
    /// Invokes <see cref="IOutcomeStatusMapper.Map{TFailure}"/> reflectively for a failure value whose static
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
            + "docs/research/native-aot-and-trimming-options.md."
    )]
    private static OutcomeHttpMapping InvokeMap(IOutcomeStatusMapper mapper, Type failureType, object? failureValue)
    {
        var closedMapMethod = MapMethodCache.GetOrAdd(failureType, static ft => MapMethodDefinition.MakeGenericMethod(ft));
        return (OutcomeHttpMapping)closedMapMethod.Invoke(mapper, [failureValue])!;
    }

    /// <summary>Renders an <see cref="OutcomeHttpMapping"/> as an <see cref="IActionResult"/> for controller-based endpoints.</summary>
    private static IActionResult ToActionResult(this OutcomeHttpMapping mapping) =>
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
    private static IResult ToMinimalApiResult(this OutcomeHttpMapping mapping) =>
        mapping.Body is null
            ? Results.StatusCode(mapping.StatusCode)
            : Results.Json(mapping.Body, statusCode: mapping.StatusCode);
}
