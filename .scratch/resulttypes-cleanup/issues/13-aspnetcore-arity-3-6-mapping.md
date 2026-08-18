# 13 - AspNetCore arity 3-6 mapping + new failure-case handling

## Labels

ready-for-agent

> **Partial-overlap note (2026-08-18):** `audit-followups/05-aspnetcore-multi-arity-outcome-mapping-tests.md` already added the 8 arity 3-6 `ToActionResult`/`ToMinimalApiResult` overloads described in the first acceptance criterion below, as a prerequisite for the test coverage that ticket required (`src/Interlocutor.AspNetCore/ResultExtensions.cs`, commit `38bff74`). When this ticket is picked up, verify that work against the acceptance criteria here rather than re-adding it - what's left is the `Cancelled`/`Timeout`/`RateLimited` mapper methods and the `.Reasons` swap, both still genuinely blocked on tickets 04/05.

**What to build:** `ToActionResult`/`ToMinimalApiResult` today only have overloads for the non-generic `OperationOutcome` and the 2-case `OperationOutcome<TSuccess,TFailure>` - a handler returning `OperationOutcome<UserDto, NotFound, Conflict>` has no mapping helper to call at all. Add the missing overloads for arities 3 through 6, direct extensions of the existing arity-2 pattern (`.Value switch { ... }` dispatching through `IOutcomeStatusMapper`). Also extend `DefaultOutcomeStatusMapper` to handle the three new case types (`Cancelled`, `Timeout`, `RateLimited` from ticket 05) via new protected virtual `Map*` methods, following the existing per-case override-point pattern, and swap `MapFailures`'s use of `failures.Values` to the new `.Reasons` projection (ticket 04).

**Blocked by:** 02 (fixture rename), 04 (Failure/Failures redesign, for `.Reasons`), 05 (new case types)

- [x] `ToActionResult`/`ToMinimalApiResult` overloads exist for `OperationOutcome<TSuccess, TCase2, TCase3>` through `OperationOutcome<TSuccess, TCase2, TCase3, TCase4, TCase5, TCase6>` (8 new overloads total), each delegating to `IOutcomeStatusMapper` exactly like the existing arity-2 overload - **already satisfied by prior work**: `audit-followups/05` (commit `38bff74`) added these to `src/Interlocutor.AspNetCore/ResultExtensions.cs`; verified in place, not re-added
- [x] `DefaultOutcomeStatusMapper` gains `MapCancelled`, `MapTimeout`, `MapRateLimited` protected virtual methods, wired into its case-dispatch switch - **newly added** in this ticket
- [x] `MapFailures` uses `failures.Reasons` instead of `failures.Values.Select(f => f.Reason ?? "Failure")` - **newly fixed** in this ticket (was still using `failures.Items.Select(f => f.Reason ?? "Failure")`, the post-`.Values`-removal form; swapped to `failures.Reasons`)
- [x] No HTTP status code numbers appear in this ticket's description or in any doc other than `aspnetcore-integration.md` (house style) - the actual numeric choices live only in code and that one doc
- [x] Unit tests cover success and every failure-case branch for each new arity overload - added a full success + 5-failure-case sweep of the 6-arity overload, plus dedicated mapper-level tests for `Cancelled`/`Timeout`/`RateLimited`
- [x] `dotnet test tests/Interlocutor.AspNetCore.Tests/Interlocutor.AspNetCore.Tests.csproj` passes
