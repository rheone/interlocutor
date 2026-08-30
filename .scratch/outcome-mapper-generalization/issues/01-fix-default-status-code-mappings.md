# 01 - Fix the two backwards default status-code mappings

**What to build:** `DefaultOutcomeStatusMapper`'s built-in HTTP status codes for `ValidationErrors`/validation-shaped failures and `Timeout` currently pick codes that are semantically backwards for what they represent. A consumer calling `ToActionResult()`/`ToMinimalApiResult()` with the default mapper (no custom `IOutcomeStatusMapper` supplied) should get:

- A `ValidationErrors` (or any other `IValidationFailureCase`-shaped) failure → **400 Bad Request** (was 422 Unprocessable Entity).
- A `Timeout` failure → **504 Gateway Timeout** (was 408 Request Timeout — 408 means the *client* was too slow sending its request, not that a server-side operation exceeded its own time budget).

No other mapping in the table changes. This ticket does not touch the mapper's location, name, or shape — only the two numbers (and their `Status` field inside the `ProblemDetails` body, which mirrors the status code).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `DefaultOutcomeStatusMapper.MapValidationErrors` and `MapValidationFailureCase` return `OutcomeHttpMapping` with `StatusCode = StatusCodes.Status400BadRequest` (and the `ProblemDetails.Status` field updated to match) instead of 422.
- [x] `DefaultOutcomeStatusMapper.MapTimeout` returns `OutcomeHttpMapping` with `StatusCode = StatusCodes.Status408RequestTimeout` replaced by `StatusCodes.Status504GatewayTimeout` (and the `ProblemDetails.Status` field updated to match).
- [x] Existing `Interlocutor.AspNetCore.Tests` coverage for these two mappings is updated to assert the new codes (400 and 504 respectively); no other existing test in the project changes.
- [x] `documentation/aspnetcore-integration.md`'s status-code mapping table and any JSON response examples showing 422 (validation) or 408 (timeout) are updated to the new codes. (`maintainer_docs/PRD.md`'s equivalent table updated too, in the same pass, per the documentation-maintenance rule.)
- [x] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.

**Landed as:** commit `7f29e6c`.
