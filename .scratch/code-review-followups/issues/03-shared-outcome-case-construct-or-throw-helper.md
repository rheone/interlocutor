# 03 - Extract a shared "construct outcome case or throw" helper

**What to build:** `AuthorizationBehavior.ConstructForbiddenResponse`, `CancellationBehavior.ConstructCancelledResponse`, `TimeoutBehavior.ConstructTimeoutResponse`, `RateLimitedBehavior.ConstructRateLimitedResponse`, and `ValidationBehavior.ConstructValidationErrorsResponse` each hand-roll an identical try/catch wrapper around `caseRegistry.Construct<TResponse, TCase>(...)`, re-throwing `OutcomeCaseNotSupportedException` as `InvalidOperationException` with only the case type and behavior name substituted. The XML doc comments on these methods already cross-reference each other's "preserve the pre-registry exception style" note - a signal the logic should be unified rather than copy-pasted five times across two packages (`Interlocutor.Abstractions`, `Interlocutor.FluentValidation`).

**Blocked by:** None - can start immediately

**Status:** done

- [x] A single shared helper (e.g. an extension method on `IOutcomeCaseRegistry` taking the case value, behavior name, and case name) replaces the five near-identical try/catch/rewrap blocks
- [x] All five call sites (`AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, `ValidationBehavior`) use the shared helper and produce the same exception messages as before
- [x] Existing tests for all five behaviors still pass unchanged in observed behavior

## Comments

Verified already implemented: `OutcomeCaseRegistryExtensions.ConstructOrThrow<TResponse, TCase>` (`Interlocutor.Abstractions/Pipeline/Cases`) is the shared helper, and all five call sites (`AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior` in `Interlocutor.Abstractions`, `ValidationBehavior` in `Interlocutor.FluentValidation`) call it. `dotnet test` passes for `Interlocutor.Abstractions.Tests` (153/153) and `Interlocutor.FluentValidation.Tests` (9/9).
