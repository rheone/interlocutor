# 08 - Marker interfaces for `RateLimited`/`Timeout`/`Forbidden` (interface + retrofit only)

Status: done

## Labels

ready-for-agent

## What to build

Apply the same, narrow move ticket 01 makes for `ValidationErrors` to the three other pipeline result types that carry exactly one shipped implementation today - add a minimal marker interface per type in `Interlocutor.CommonKernel`, and retrofit the existing struct to implement it. No new case type, no new behavior, no `DefaultOutcomeStatusMapper` change - that work is the separate, still-parked second-implementation work in issues 05-07, which this ticket does not do.

```csharp
public interface IRateLimitedFailureCase
{
    string? Message { get; }
}

public interface ITimeoutFailureCase
{
    string? Message { get; }
}

public interface IForbiddenFailureCase
{
    string? Reason { get; }
}
```

Each interface member matches its struct's existing property name exactly (`RateLimited.Message`, `Timeout.Message`, `Forbidden.Reason`) - no renaming, no new-named surface, no mapping. `RateLimited`, `Timeout`, and `Forbidden` each implement their corresponding interface.

This is purely a structural-consistency change: each interface has exactly one implementer today and no consumer pattern-matches on it yet (`DefaultOutcomeStatusMapper` still matches the concrete types directly, unchanged). The payoff is that *if* a second implementation of any of these ever arrives, the CommonKernel struct needs no retrofit at that point - only ticket 05, 06, or 07 (whichever applies) needs doing, exactly mirroring how issue 02 built on issue 01 for validation.

Extend `maintainer_docs/adr/0020-validation-failure-case-fidelity.md` (written in ticket 01) with a short addendum recording this as a deliberate policy - marker interfaces for a result-type family are added ahead of a second implementation for naming/pattern consistency, while the type/behavior/mapper work for that family stays gated on an actual second implementation being wanted. Not a new ADR.

## Acceptance criteria

- [x] `IRateLimitedFailureCase`, `ITimeoutFailureCase`, `IForbiddenFailureCase` added to `Interlocutor.CommonKernel` with XML doc comments
- [x] `RateLimited`, `Timeout`, `Forbidden` each implement their corresponding interface with no observable behavior change to any existing consumer
- [x] `Interlocutor.CommonKernel/PublicAPI.Unshipped.txt` updated for all three new interfaces and the three interface implementations
- [x] Unit tests confirm each struct satisfies its interface and the exposed member matches the struct's existing property value
- [x] `maintainer_docs/adr/0020-validation-failure-case-fidelity.md` extended with the consistency-policy addendum described above
- [x] `dotnet build` and `dotnet test` remain clean

## Blocked by

01 - mirrors its pattern and extends the same ADR; should land after `IValidationFailureCase` sets the precedent.
