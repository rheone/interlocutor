# 07 - Fix Cancelled/Timeout/RateLimited ToString() XML docs

**What to build:** `Cancelled.cs`, `RateLimited.cs`, and `Timeout.cs` document their `ToString()` override with `<inheritdoc/>`, while every other case type in `Interlocutor.CommonKernel` (`Failure`, `NotFound`, `Forbidden`, `Conflict`, `ValidationError`, `Conflicts`, `Failures`) carries a full `<summary>`/`<returns>` block spelling out the exact output format. `<inheritdoc/>` on an override with no base XML doc risks emitting no useful text, and ticket 05's own acceptance criteria asked for docs "matching the existing case types' style."

**Blocked by:** None - can start immediately

**Status:** done

- [x] `Cancelled.ToString()`, `RateLimited.ToString()`, and `Timeout.ToString()` each carry a full `<summary>`/`<returns>` doc block matching the style and specificity of the other case types' `ToString()` docs

## Comments

Verified already implemented: all three types' `ToString()` overrides carry a full `<summary>`/`<returns>` doc block spelling out the exact output format (no `<inheritdoc/>` remains). `dotnet test tests/Interlocutor.CommonKernel.Tests` passes (166/166).
