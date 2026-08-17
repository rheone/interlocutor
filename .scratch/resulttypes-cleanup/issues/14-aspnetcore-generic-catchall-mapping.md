# 14 - AspNetCore generic catch-all mapping via IOutcomeCaseRegistry

## Labels

ready-for-agent

**What to build:** Add generic `ToActionResult<TResponse>`/`ToMinimalApiResult<TResponse>` overloads constrained only to `where TResponse : IOperationOutcome`, using `IOutcomeCaseRegistry.TryGetActiveCase` to extract the active case and `IOutcomeClassifier`/`IOutcomeStatusMapper` to classify and map it - this is what finally lets a hand-rolled, non-union `IOperationOutcome` type produce an `IActionResult`/`IResult` at all. Verify (with a compile-time-behavior regression test) that the closed-generic arity overloads from ticket 13 are still chosen by the compiler over this catch-all for the six built-in union arities, per C# overload-resolution rules (a constructed generic formal parameter is more specific than a bare type-parameter formal parameter).

**Blocked by:** 03 (IOutcomeCaseRegistry), 13 (arity 3-6 mapping)

- [ ] `ToActionResult<TResponse>(this TResponse, IOutcomeStatusMapper?, IOutcomeCaseRegistry?)` and the `ToMinimalApiResult<TResponse>` equivalent exist, constrained to `IOperationOutcome`
- [ ] Both use `IOutcomeCaseRegistry.TryGetActiveCase` to extract the case, then classify success/failure and map failures reflectively (caching resolved mapper methods per failure `Type`)
- [ ] A test proves `OperationOutcome<TSuccess,TFailure>.ToActionResult()` still resolves to its dedicated arity-2 overload, not the catch-all (locks in the overload-resolution behavior)
- [ ] A test exercises the catch-all end-to-end against a hand-rolled, non-union `IOperationOutcome` type with a registered `IOutcomeCaseRegistry` entry, producing the correct success/failure `IActionResult`/`IResult`
- [ ] Manual sanity check: a minimal test host with one endpoint returning a built-in arity-4 union and one returning a custom `IOperationOutcome` type, hit with an HTTP client, confirming both produce the expected response shape/status
- [ ] `dotnet test tests/Interlocutor.AspNetCore.Tests/Interlocutor.AspNetCore.Tests.csproj` passes
