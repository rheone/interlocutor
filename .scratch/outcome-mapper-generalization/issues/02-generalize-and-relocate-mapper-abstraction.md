# 02 - Generalize the mapper abstraction into Abstractions and relocate the AspNetCore implementation onto it

**What to build:** Today, `IOutcomeStatusMapper`/`OutcomeHttpMapping` are pure C# (no ASP.NET Core types in their own signatures) but live in `Interlocutor.AspNetCore` anyway, so a non-HTTP consumer (a gRPC service, a CLI tool, a queue handler) who wants the same pluggable "map each failure case to some result" ergonomic has nothing reachable to build on — they'd have to hand-roll their own switch statement.

Introduce a generic `IOutcomeMapper<TResult>` interface and an `OutcomeMapperBase<TResult>` abstract class in `Interlocutor.Abstractions` (the same package that already hosts the structurally similar `IOutcomeCaseRegistry`), carrying the same protected-virtual-per-case dispatch pattern `DefaultOutcomeStatusMapper` already implements today (one hook per failure case: `NotFound`, `Forbidden`, `Conflict`, `ValidationErrors`, generic `IValidationFailureCase`, `Failure`/`Failures`, `Cancelled`, `Timeout`, `RateLimited`, plus the generic `NotFound<TId>`/unrecognized fallback) — generalized to return `TResult` instead of the HTTP-specific `OutcomeHttpMapping`.

Then refactor `Interlocutor.AspNetCore` to sit on top of it instead of duplicating the dispatch logic:
- `IOutcomeStatusMapper : IOutcomeMapper<OutcomeHttpMapping>` — kept as a thin, discoverable HTTP-flavored name (not replaced everywhere by the raw generic type).
- `DefaultOutcomeStatusMapper` renamed to `DefaultHttpOutcomeStatusMapper : OutcomeMapperBase<OutcomeHttpMapping>, IOutcomeStatusMapper`, with each `MapXxx` override filling in the HTTP-specific body/status (identical behavior to today, now including Ticket 01's fixed 400/504 codes) instead of re-declaring the dispatch switch itself.
- `OutcomeHttpMapping` stays in `Interlocutor.AspNetCore` — it's HTTP vocabulary (status code + body), meaningless outside HTTP.
- `ResultExtensions` and every reference to the old `DefaultOutcomeStatusMapper` name updated accordingly.

This is a clean-break rename (library is pre-release, `1.0.0-dev`, no shipped consumers) — no `[Obsolete]` forwarding shims.

**Blocked by:** Ticket 01 (touches the same file — landing the status-code fix first avoids rework/merge noise on top of the rename).

**Status:** ready-for-agent

- [ ] `Interlocutor.Abstractions` gains `IOutcomeMapper<TResult>` (single method: `TResult Map<TFailure>(TFailure failure)`) and `OutcomeMapperBase<TResult>` (abstract class, protected virtual per-case hook methods mirroring today's `DefaultOutcomeStatusMapper` cases, each returning `TResult`), each with full XML doc comments.
- [ ] New `Interlocutor.Abstractions.Tests` coverage exercises `OutcomeMapperBase<TResult>` with a **non-HTTP** `TResult` (e.g. a trivial `int`/`string`-returning test double), proving the abstraction is genuinely usable outside an HTTP context — not just a renamed copy of the AspNetCore tests.
- [ ] `Interlocutor.AspNetCore.IOutcomeStatusMapper` now extends `Interlocutor.Abstractions.IOutcomeMapper<OutcomeHttpMapping>`.
- [ ] `DefaultOutcomeStatusMapper` is renamed to `DefaultHttpOutcomeStatusMapper`, derives from `OutcomeMapperBase<OutcomeHttpMapping>`, implements `IOutcomeStatusMapper`, and every `MapXxx` override preserves its existing HTTP behavior (including Ticket 01's fixed codes) with no functional regression.
- [ ] `ResultExtensions` and any other in-repo reference to `DefaultOutcomeStatusMapper` (source, tests, docs, samples, `TutorialProject/`) updated to the new type name.
- [ ] `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` still passes unmodified (or is extended if a new dependency-direction rule is warranted for the new Abstractions types) — confirm `Interlocutor.Abstractions` gains no dependency on ASP.NET Core.
- [ ] `documentation/extensibility.md` updated: the extension-point table entry for the mapper reflects the new `Interlocutor.Abstractions` types, and prose explains how a non-HTTP consumer writes/configures their own `IOutcomeMapper<TResult>`/`OutcomeMapperBase<TResult>` (same "pass an instance, nothing to register in DI" convention used today for `IOutcomeStatusMapper`).
- [ ] `documentation/aspnetcore-integration.md`'s "Custom HTTP Mapping" section updated for the renamed/relocated types.
- [ ] `documentation/glossary.md` updated for the renamed/relocated/new terms (`IOutcomeMapper<TResult>`, `OutcomeMapperBase<TResult>`, `DefaultHttpOutcomeStatusMapper`).
- [ ] A new ADR under `maintainer_docs/adr/` records the relocation/rename/status-code rationale (mirroring ADR-0020's format for the sibling `FluentValidationErrors` fidelity gap).
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.
