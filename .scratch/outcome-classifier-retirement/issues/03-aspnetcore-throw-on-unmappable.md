# 03 - AspNetCore's catch-all case mapping throws when it can't map a case

**Status:** done - implemented in `311ce89`.

**What to build:** `ResultExtensions.ExtractCase` ([ResultExtensions.cs:199-220](../../../src/Interlocutor.AspNetCore/ResultExtensions.cs#L199-L220)) currently falls back to `OutcomeClassifierBuilder.DefaultClassifier.IsFailure(caseValue)` (line 217) for a case type the registry didn't resolve via the union fallback. Once issue 01 removes `IsFailure`, this call site needs its own resolution: keep a small default HTTP-status mapping set as a convenience for consumers who don't supply their own (most will), but when a case genuinely can't be mapped to a status code, throw a new named exception rather than guessing - this is the one place in the codebase where "can't decide" is itself the caller's actual problem (an HTTP response must have *some* status code), scoped entirely to this project per house style (only `aspnetcore-integration.md` references HTTP status codes).

**Blocked by:** 01 (removes the `IsFailure` call this method currently makes)

- [ ] `ExtractCase` no longer references `OutcomeClassifierBuilder`/`IOutcomeClassifier`
- [ ] The existing default mapping set for kernel case types is preserved unchanged for a consumer who supplies no custom mapper
- [ ] A case type the default mapping set and any supplied custom mapper both fail to resolve throws a new, clearly-named exception (naming to match `OutcomeCaseNotSupportedException`'s convention) rather than silently choosing a generic status
- [ ] `documentation/aspnetcore-integration.md` updated for the new throwing behavior
- [ ] `dotnet test tests/Interlocutor.AspNetCore.Tests/Interlocutor.AspNetCore.Tests.csproj` passes, including a new test for the throw path
