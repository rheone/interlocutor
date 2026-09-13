# 03 — Fail fast when untyped bare-command dispatch is ambiguous

**What to build:** `IMediator`'s untyped dispatch path (`SendAsync(object, CancellationToken)` — used for scenarios like outbox/inbox replay where no static `TResult` is available) resolves a bare command's result type via `ReflectionRequestDispatcher.ResolveBareCommandResultType`, which scans loaded assemblies for any `IBareCommandHandler<TCommand,_>` closing and returns the first match found. When more than one closing is registered for the same command type — a scenario `MediatorTests.cs` already builds test doubles for — the tie-break is assembly/type enumeration order, which is unspecified and untested.

After this ticket, untyped dispatch throws a named, descriptive exception identifying the command type and every ambiguous `TResult` closing found, instead of resolving nondeterministically — in line with the fail-fast, never-silent convention pipeline behaviors already follow for outcome-case construction (ADR-0023). A single registered closing continues to resolve exactly as today; only the genuinely ambiguous case changes behavior.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] `ResolveBareCommandResultType` throws a named exception (not a silent first-match return) when more than one `IBareCommandHandler<TCommand,_>` closing is found for the same command type
- [ ] The exception message names the command type and lists every ambiguous result type found
- [ ] A single registered closing continues to resolve exactly as it does today — no behavior change for the unambiguous case
- [ ] A test proves the ambiguous case throws, reusing/extending `MediatorTests.cs`'s existing dual-closing test doubles
