# 03 - Field-level validation error bodies from FluentValidationErrors

**What to build:** `Interlocutor.FluentValidation.FluentValidationErrors` (added under ADR-0020) already preserves each `FluentValidation.Results.ValidationFailure`'s `PropertyName` alongside its message, but the HTTP mapper only ever reads `.Messages` off the shared `IValidationFailureCase` marker — so today, a consumer using `FluentValidationErrorsBehavior` still gets a flat, joined-message `ProblemDetails` body over HTTP, with the per-field data silently discarded at the response boundary even though it survived the whole pipeline.

`Interlocutor.AspNetCore` is architecturally forbidden from depending on `Interlocutor.FluentValidation` directly (enforced by `DependencyTests.cs`), so this has to reuse the same duck-typed marker-interface technique already used for `IForbiddenFailureCase`/`ITimeoutFailureCase`/`IRateLimitedFailureCase`: add a new marker interface in `Interlocutor.CommonKernel` that exposes each failure's property name alongside its message, have `FluentValidationErrors` implement it, and have `DefaultHttpOutcomeStatusMapper` pattern-match on it to build a proper field-keyed `ValidationProblemDetails` body (grouped by property name, with an empty-string key for any property-less entries) instead of the flat `ProblemDetails`. Status code stays 400 (per Ticket 01/02) regardless of whether a failure carries field data or not — no separate status-code fork for field-level vs. flat validation failures.

**Blocked by:** Ticket 02 (edits `DefaultHttpOutcomeStatusMapper`, which doesn't exist under that name/shape until Ticket 02 lands).

**Status:** ready-for-agent

- [ ] `Interlocutor.CommonKernel` gains a new marker interface (e.g. `IFieldValidationFailureCase : IValidationFailureCase`, naming finalized during implementation to match existing conventions) exposing each failure's property name alongside its message, with full XML doc comments matching the style of `IForbiddenFailureCase`/`ITimeoutFailureCase`/`IRateLimitedFailureCase`.
- [ ] `Interlocutor.FluentValidation.FluentValidationErrors` implements the new marker interface, surfacing each underlying `ValidationFailure.PropertyName` (which may be null/empty for a failure not tied to a specific property).
- [ ] `DefaultHttpOutcomeStatusMapper` pattern-matches on the new marker interface: when present, it builds ASP.NET Core's `ValidationProblemDetails` (errors grouped by property name, empty-string key for property-less entries) at 400; when absent, it falls back to today's flat `ProblemDetails` at 400 (per Ticket 01) — behavior for `ValidationErrors` itself (which never carries a property name) is unchanged by this ticket.
- [ ] New end-to-end test: an invalid command routed through `FluentValidationErrorsBehavior` → `ToActionResult()` (or `ToMinimalApiResult()`) asserts a `ValidationProblemDetails` body with per-field errors keyed correctly.
- [ ] `documentation/aspnetcore-integration.md`'s ValidationErrors/Problem-Details sections updated to describe the new `ValidationProblemDetails` behavior and when it applies.
- [ ] `documentation/pipeline-and-behaviors.md`'s `FluentValidationErrors` description updated to note it now drives the HTTP mapper's field-keyed body.
- [ ] `documentation/glossary.md` updated with an entry for the new marker interface.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.
