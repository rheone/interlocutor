# 13 - AspNetCore arity 3-6 mapping + new failure-case handling

## Labels

ready-for-agent

**What to build:** `ToActionResult`/`ToMinimalApiResult` today only have overloads for the non-generic `OperationOutcome` and the 2-case `OperationOutcome<TSuccess,TFailure>` - a handler returning `OperationOutcome<UserDto, NotFound, Conflict>` has no mapping helper to call at all. Add the missing overloads for arities 3 through 6, direct extensions of the existing arity-2 pattern (`.Value switch { ... }` dispatching through `IOutcomeStatusMapper`). Also extend `DefaultOutcomeStatusMapper` to handle the three new case types (`Cancelled`, `Timeout`, `RateLimited` from ticket 05) via new protected virtual `Map*` methods, following the existing per-case override-point pattern, and swap `MapFailures`'s use of `failures.Values` to the new `.Reasons` projection (ticket 04).

**Blocked by:** 02 (fixture rename), 04 (Failure/Failures redesign, for `.Reasons`), 05 (new case types)

- [ ] `ToActionResult`/`ToMinimalApiResult` overloads exist for `OperationOutcome<TSuccess, TCase2, TCase3>` through `OperationOutcome<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6>` (8 new overloads total), each delegating to `IOutcomeStatusMapper` exactly like the existing arity-2 overload
- [ ] `DefaultOutcomeStatusMapper` gains `MapCancelled`, `MapTimeout`, `MapRateLimited` protected virtual methods, wired into its case-dispatch switch
- [ ] `MapFailures` uses `failures.Reasons` instead of `failures.Values.Select(f => f.Reason ?? "Failure")`
- [ ] No HTTP status code numbers appear in this ticket's description or in any doc other than `aspnetcore-integration.md` (house style) - the actual numeric choices live only in code and that one doc
- [ ] Unit tests cover success and every failure-case branch for each new arity overload
- [ ] `dotnet test tests/Interlocutor.AspNetCore.Tests/Interlocutor.AspNetCore.Tests.csproj` passes
