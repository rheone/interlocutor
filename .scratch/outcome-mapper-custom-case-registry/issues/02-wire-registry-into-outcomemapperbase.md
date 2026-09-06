# 02 — Wire the custom-type registry into `OutcomeMapperBase.MapUnrecognized`

**What to build:** a consumer deriving from `OutcomeMapperBase<TResult>` can supply a built custom-case registry (from ticket 01), and calling `Map<TFailure>` with a registered custom failure type now dispatches to that registered mapping instead of falling straight into `MapUnrecognized<TFailure>`. Every built-in case (`NotFound`, `Forbidden`, `Conflict`, `ValidationErrors`/`IValidationFailureCase`, `Failure`/`Failures`, `Cancelled`, `Timeout`, `RateLimited`, `NotFound<TId>`) keeps dispatching exactly as it does today, unchanged. A subclass with no registry supplied, or a call with an unregistered type, behaves identically to today - the subclass's own `MapUnrecognized` override still runs.

**Blocked by:** 01 (needs the registration builder to exist).

**Status:** ready-for-agent

- [ ] `OutcomeMapperBase<TResult>` gains a way to be constructed with (or supplied) a built custom-case registry from ticket 01; omitting it preserves today's exact behavior.
- [ ] `Map<TFailure>`'s dispatch, after exhausting every built-in case and the reflective `NotFound<TId>` check, consults the supplied registry (exact-type lookup) before calling the subclass's `MapUnrecognized<TFailure>` override.
- [ ] A registry hit maps the value using the registered delegate; a miss (or no registry supplied) falls through to `MapUnrecognized<TFailure>` exactly as today.
- [ ] The compiler still requires a concrete `OutcomeMapperBase<TResult>` subclass to implement every built-in case's abstract method - this ticket does not weaken that guarantee.
- [ ] Every existing built-in-case dispatch test for `OutcomeMapperBase<TResult>`/`DefaultHttpOutcomeStatusMapper` continues to pass unmodified.
- [ ] New integration tests prove: a concrete mapper subclass with a populated registry maps a registered custom type distinctly; the same subclass with an unregistered type still reaches its own `MapUnrecognized` override.
- [ ] `documentation/extensibility.md`'s "Mapping failure cases to any output shape: `IOutcomeMapper<TResult>`" section is updated to show registering a custom-type rule alongside the existing per-case-override example.
- [ ] `dotnet build` and `dotnet test` pass for `Interlocutor.Abstractions` and `Interlocutor.AspNetCore`; `dotnet csharpier .` produces no diff.
