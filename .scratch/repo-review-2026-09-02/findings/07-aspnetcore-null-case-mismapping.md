Status: needs-triage
Severity: HIGH
Category: bug
Source: antagonist lane's independent gap-hunt (same root cause as [01-outcome-null-payload-no-discriminator.md](01-outcome-null-payload-no-discriminator.md), distinct blast radius in a package that lane's review didn't examine)

# Null-payload discriminator bug silently corrupts `Interlocutor.AspNetCore`'s reflective outcome-to-HTTP mapping

## Summary

For a successful outcome with a null case payload, `IOutcomeCaseRegistry.TryGetActiveCase` returns `true` (not `false`, and doesn't throw the way other code paths do) but reports the *outcome's own container type* as the active case type — a value that is never a real, registered case — so `Interlocutor.AspNetCore`'s generic `ToActionResult()` catch-all silently gets fed garbage instead of failing loudly or succeeding correctly.

## Evidence

`src/Interlocutor.Abstractions/Pipeline/Cases/DefaultOutcomeCaseRegistry.cs:55-72`'s `TryGetActiveCase`:

```csharp
if (response is IUnion union) {
    caseValue = union.Value;
    caseType = caseValue?.GetType() ?? typeof(TResponse);
    return true;
}
```

For a null-payload success case, `union.Value` is `null`, so `caseValue = null` and — because `null?.GetType()` is `null` — `caseType` falls back to `typeof(TResponse)`, the entire outcome union type itself, while still returning `true` ("found it").

`src/Interlocutor.AspNetCore/ResultExtensions.cs:415-436`'s `ExtractCase<TResponse>` calls this and then classifies via `OutcomeClassifierBuilder.DefaultClassifier.IsFailure(caseValue)` with `caseValue = null` — the same fail-closed path issue 01 traced for `UnitOfWorkBehavior` — so a legitimately successful null result is reported as `IsFailure = true` with `CaseType = typeof(TResponse)`. The reflective catch-all overload (used, per `ResultExtensions.cs`'s own doc comments at lines 301, 324, 353, for "any compiler-generated union" not covered by a dedicated arity overload) then attempts `InvokeMap(mapper, typeof(TResponse), null)` — a type argument no `IOutcomeStatusMapper` implementation will have a real overload or registration for. Depending on the mapper implementation, this either throws an unhelpful reflection-shaped exception at the action boundary, or maps a legitimately successful null result to a failure HTTP status.

## Suggested action

Fix at the root — the same discriminator fix issue 01 calls for should make `TryGetActiveCase` correctly report the real case type (e.g. `string` for `Outcome<string?, NotFound>`'s success case) even when the boxed value is null. Once fixed, add a regression test exercising `ToActionResult()` (or the equivalent `IOutcomeStatusMapper` catch-all path) for a null-payload success outcome specifically — this is a different code path than what `Interlocutor.CommonKernel.Tests`/`UnitOfWorkBehavior` tests cover.
