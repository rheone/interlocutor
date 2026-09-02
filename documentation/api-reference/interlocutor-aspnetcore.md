# Interlocutor.AspNetCore API Reference

Every public type in `Interlocutor.AspNetCore`, grouped by namespace - [outcome-to-HTTP mapping](glossary.md#outcome-mapping) for both controller-based and minimal API endpoints.

## Interlocutor.AspNetCore

### `AspNetCoreRequestContextAccessor` sealed class

ASP.NET Core `IRequestContextAccessor` implementation that populates request context from the authenticated caller.

| Member | Summary |
| --- | --- |
| `public AspNetCoreRequestContextAccessor(IHttpContextAccessor httpContextAccessor)` |  |
| `public RequestContext? Context` |  |

### `DefaultHttpOutcomeStatusMapper` class

The `IOutcomeStatusMapper` applied by `ResultExtensions` when a consumer registers none of their own. Recognizes the built-in Interlocutor.CommonKernel.ResultTypes failure cases and their aggregate forms via `OutcomeMapperBase`'s dispatch; anything else - including a consumer's own custom failure type - falls back to `MapUnrecognized`.

| Member | Summary |
| --- | --- |
| `public DefaultHttpOutcomeStatusMapper()` |  |

### `IOutcomeStatusMapper` interface

Maps a failure case value from an Interlocutor Outcome union to the `OutcomeHttpMapping` (status code and optional body) that represents it over HTTP.


### `OutcomeHttpMapping` record

A transport-agnostic description of how a failure case should be represented in an HTTP response: a status code and an optional response body.

| Member | Summary |
| --- | --- |
| `public OutcomeHttpMapping(int StatusCode, object? Body = null)` |  |
| `public object? Body` | The response body, or `null` when the response should carry no body (e.g. a bare status code). |
| `public int StatusCode` | The HTTP status code to use for the response. |
| `public void Deconstruct(out int StatusCode, out object? Body)` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(OutcomeHttpMapping? other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(OutcomeHttpMapping? left, OutcomeHttpMapping? right)` |  |
| `public static bool operator !=(OutcomeHttpMapping? left, OutcomeHttpMapping? right)` |  |

### `ResultExtensions` class

Converts Interlocutor Outcome unions to ASP.NET Core result types. Provides both controller-based (`IActionResult`) and minimal API mappings, both delegating to an `IOutcomeStatusMapper` for the failure-to-status-code decision so the two cannot drift.

| Member | Summary |
| --- | --- |
| `public static IActionResult ToActionResult<TSuccess, TFailure>(Outcome<TSuccess, TFailure> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to an `IActionResult` with the appropriate HTTP status code. |
| `public static IActionResult ToActionResult(Outcome result, IOutcomeStatusMapper? mapper = null)` | Converts a void `Outcome` to an `IActionResult` with the appropriate HTTP status code. |
| `public static IActionResult ToActionResult<TSuccess, TCase2, TCase3>(Outcome<TSuccess, TCase2, TCase3> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to an `IActionResult` with the appropriate HTTP status code. |
| `public static IActionResult ToActionResult<TSuccess, TCase2, TCase3, TCase4>(Outcome<TSuccess, TCase2, TCase3, TCase4> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to an `IActionResult` with the appropriate HTTP status code. |
| `public static IActionResult ToActionResult<TSuccess, TCase2, TCase3, TCase4, TCase5>(Outcome<TSuccess, TCase2, TCase3, TCase4, TCase5> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to an `IActionResult` with the appropriate HTTP status code. |
| `public static IActionResult ToActionResult<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6>(Outcome<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to an `IActionResult` with the appropriate HTTP status code. |
| `public static IActionResult ToActionResult<TResponse>(TResponse response, IOutcomeCaseRegistry? caseRegistry, IOutcomeStatusMapper? mapper = null)` | Converts an arbitrary `IOutcome` to an `IActionResult` with the appropriate HTTP status code. |
| `public static IResult ToMinimalApiResult<TSuccess, TFailure>(Outcome<TSuccess, TFailure> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to a minimal API result with the appropriate HTTP status code. |
| `public static IResult ToMinimalApiResult(Outcome result, IOutcomeStatusMapper? mapper = null)` | Converts a void `Outcome` to a minimal API result with the appropriate HTTP status code. |
| `public static IResult ToMinimalApiResult<TSuccess, TCase2, TCase3>(Outcome<TSuccess, TCase2, TCase3> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to a minimal API result with the appropriate HTTP status code. |
| `public static IResult ToMinimalApiResult<TSuccess, TCase2, TCase3, TCase4>(Outcome<TSuccess, TCase2, TCase3, TCase4> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to a minimal API result with the appropriate HTTP status code. |
| `public static IResult ToMinimalApiResult<TSuccess, TCase2, TCase3, TCase4, TCase5>(Outcome<TSuccess, TCase2, TCase3, TCase4, TCase5> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to a minimal API result with the appropriate HTTP status code. |
| `public static IResult ToMinimalApiResult<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6>(Outcome<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6> result, IOutcomeStatusMapper? mapper = null)` | Converts a `Outcome` to a minimal API result with the appropriate HTTP status code. |
| `public static IResult ToMinimalApiResult<TResponse>(TResponse response, IOutcomeCaseRegistry? caseRegistry, IOutcomeStatusMapper? mapper = null)` | Converts an arbitrary `IOutcome` to a minimal API result with the appropriate HTTP status code. |

