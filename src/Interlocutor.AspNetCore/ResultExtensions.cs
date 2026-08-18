namespace Interlocutor.AspNetCore;

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

    /// <summary>Renders an <see cref="OutcomeHttpMapping"/> as an <see cref="IActionResult"/> for controller-based endpoints.</summary>
    private static IActionResult ToActionResult(this OutcomeHttpMapping mapping) =>
        mapping.Body is null
            ? new StatusCodeResult(mapping.StatusCode)
            : new ObjectResult(mapping.Body) { StatusCode = mapping.StatusCode };

    /// <summary>Renders an <see cref="OutcomeHttpMapping"/> as an <see cref="IResult"/> for minimal API endpoints.</summary>
    private static IResult ToMinimalApiResult(this OutcomeHttpMapping mapping) =>
        mapping.Body is null
            ? Results.StatusCode(mapping.StatusCode)
            : Results.Json(mapping.Body, statusCode: mapping.StatusCode);
}
