# 03 - New generator diagnostic: request marker interface without a matching response case (INTERLOC012)

**What to build:** A new `Interlocutor.Generator` diagnostic that, for each discovered `TRequest`/`TResponse` handler pair (`HandlerModel`/`HandlerTypeAnalyzer` already correlate these to emit the compile-time dispatch switch), checks whether `TRequest` implements `IRequiresPolicy`, `ICancellable`, `IRateLimited`, or `IHasTimeout` and, if so, whether the paired `TResponse` union declares a case type assignable to what that behavior needs to construct (`Forbidden`, `Cancelled`, `RateLimited`, a timeout case respectively). Emit a new diagnostic (next in sequence after `INTERLOC011`, per `InterlocutorDiagnostics.cs`) at the request type's declaration when no matching case is found, with severity configurable the same way as the existing `INTERLOC001`-`011` diagnostics (`dotnet_diagnostic.INTERLOC012.severity`). The diagnostic's own doc comment must state its scope explicitly: only these four marker-interface-gated behaviors are covered - `ValidationBehavior`/`FluentValidationErrorsBehavior` are gated by DI-registered-validator presence, not a static marker, and are out of scope for this diagnostic (see issue 06).

**Blocked by:** 02 (cites ADR-0022's design reasoning and documented scope)

**Status:** open

- [ ] `INTERLOC012` is added to `InterlocutorDiagnostics.cs` following the existing diagnostic definitions' shape and doc-comment style
- [ ] The generator emits `INTERLOC012` for a `TRequest` implementing any of the four marker interfaces whose paired `TResponse` union has no assignable case for what that behavior constructs
- [ ] The generator stays silent for a correctly-matched `(TRequest, TResponse)` pair, and for any `TRequest` not implementing one of the four markers
- [ ] New generator tests cover both the firing and non-firing paths for each of the four marker interfaces
- [ ] The diagnostic's doc comment and any generator documentation explicitly state the validation-behavior blind spot
