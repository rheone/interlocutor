# 01 - DistinctItemSet + plural-type rewrite

## Labels

ready-for-agent

**What to build:** `Conflicts`, `Failures`, `ValidationErrors`, and `ValidationErrors<TErrorCode>` currently hand-duplicate the same dedup/count/items pattern four times. Introduce an internal `DistinctItemSet<TItem>` value type in `Interlocutor.CommonKernel` that captures the shared logic (validating constructor from `IEnumerable<T>`, dedup-preserving-order, "default value is inert, not throwing"), and rewrite all four plural types to compose it instead of hand-rolling their own `ImmutableArray<T>` field. Since C# `readonly record struct` can't use base-class inheritance, this must be composition, not a shared base class. `.Items` becomes the one canonical primary accessor across all plural types; drop pure-duplicate secondary projections (e.g. `Failures.Values`, which today just repeats `.Items`). Keep only genuinely distinct semantic projections per type (e.g. `.Messages`).

**Blocked by:** None - can start immediately

- [x] `Interlocutor.CommonKernel.ResultTypes.Internal.DistinctItemSet<TItem>` exists as an internal, non-public value type
- [x] `Conflicts`, `Failures`, `ValidationErrors`, `ValidationErrors<TErrorCode>` are rewritten to compose `DistinctItemSet<TItem>` internally
- [x] Each plural type keeps its own constructor overloads (single item, `IEnumerable<T>`, and any type-specific shapes like message-only or tuple constructors) - only the internal storage/dedup mechanism is shared
- [x] `.Items` is present and behaves identically to today on all four types (dedup-preserving-order, default-value-is-inert-not-throwing)
- [x] `Failures.Values` (pure duplicate of `.Items`) is removed
- [x] Existing semantic projections (`Conflicts.Messages`, `ValidationErrors.Messages`/`.Errors`, `ValidationErrors<TErrorCode>.Messages`/`.Errors`/`.ErrorCodes`) are preserved and still correct
- [x] All existing tests for these four types pass unchanged in behavior (extend rather than replace existing test coverage)
- [x] `dotnet build` and `dotnet test tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj` pass
