# 14 - AspNetCore generic catch-all mapping via IOutcomeCaseRegistry

## Labels

ready-for-agent

**What to build:** Add generic `ToActionResult<TResponse>`/`ToMinimalApiResult<TResponse>` overloads constrained only to `where TResponse : IOperationOutcome`, using `IOutcomeCaseRegistry.TryGetActiveCase` to extract the active case and `IOutcomeClassifier`/`IOutcomeStatusMapper` to classify and map it - this is what finally lets a hand-rolled, non-union `IOperationOutcome` type produce an `IActionResult`/`IResult` at all. Verify (with a compile-time-behavior regression test) that the closed-generic arity overloads from ticket 13 are still chosen by the compiler over this catch-all for the six built-in union arities, per C# overload-resolution rules (a constructed generic formal parameter is more specific than a bare type-parameter formal parameter).

**Blocked by:** 03 (IOutcomeCaseRegistry), 13 (arity 3-6 mapping)

- [x] `ToActionResult<TResponse>(this TResponse, IOutcomeStatusMapper?, IOutcomeCaseRegistry?)` and the `ToMinimalApiResult<TResponse>` equivalent exist, constrained to `IOperationOutcome`

  > [!NOTE]
  > Shipped as `ToActionResult<TResponse>(this TResponse, IOutcomeCaseRegistry?, IOutcomeStatusMapper? = null)` -
  > `caseRegistry` before `mapper`, and required (no `= null`) rather than optional. A three-parameter overload
  > with two *optional* trailing parameters is ambiguous against each dedicated overload's single optional
  > `mapper` parameter for a bare `result.ToActionResult()` call: once `TResponse` infers to the argument's
  > exact type, C#'s "constructed generic beats bare type parameter" tie-break never gets applied because the
  > two candidates already differ in arity, so the compiler reports `CS0121` instead of picking the dedicated
  > overload - confirmed against a minimal repro reproducing the exact shape before settling on this signature.
  > Making `caseRegistry` required removes the extra optional parameter from the ambiguous case entirely: every
  > dedicated overload's zero- and one-argument call shapes resolve only against the dedicated overload, and
  > this overload stays reachable via `caseRegistry: myRegistry` (or `caseRegistry: null` for the default).

- [x] Both use `IOutcomeCaseRegistry.TryGetActiveCase` to extract the case, then classify success/failure and map failures reflectively (caching resolved mapper methods per failure `Type`)
- [x] A test proves `OperationOutcome<TSuccess,TFailure>.ToActionResult()` still resolves to its dedicated arity-2 overload, not the catch-all (locks in the overload-resolution behavior)
- [x] A test exercises the catch-all end-to-end against a hand-rolled, non-union `IOperationOutcome` type with a registered `IOutcomeCaseRegistry` entry, producing the correct success/failure `IActionResult`/`IResult`
- [x] Manual sanity check: a minimal test host with one endpoint returning a built-in arity-4 union and one returning a custom `IOperationOutcome` type, hit with an HTTP client, confirming both produce the expected response shape/status
- [x] `dotnet test tests/Interlocutor.AspNetCore.Tests/Interlocutor.AspNetCore.Tests.csproj` passes
