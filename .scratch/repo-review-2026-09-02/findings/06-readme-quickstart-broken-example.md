Status: needs-triage
Severity: HIGH
Category: documentation
Source: documentation.md lane, confirmed by antagonist lane with a correction to the fix guidance

# README.md Quick Start example uses a non-existent `Outcome.SuccessCase<T>` type

## Summary

The Quick Start's final code sample — the first, most-visible code example a new reader sees — would not compile against the shipped API.

```csharp
var message = result switch
{
    Outcome.SuccessCase<UserDto> success => $"Found: {success.Value.Name}",
    NotFound notfound => $"Not found: {notfound.Message}",
    _ => "Unexpected"
};
```

`Outcome.SuccessCase<T>` does not exist anywhere in `src/` (`rg -n "SuccessCase" src/Interlocutor.CommonKernel` is empty).

## Corrected diagnosis

The draft finding also flagged switching on `result` instead of `result.Value` as wrong — that part is **not** a defect. Verified empirically: switching directly on the union instance compiles and runs correctly (`Outcome.cs`'s own XML doc: "switch expressions over it are exhaustive without a discard arm," describing exactly this form). `messages-and-result-types.md`'s `result.Value switch {...}` form (with a discard arm, since `.Value`'s static type is `object?`) is *a* correct form, not *the only* correct form.

## Suggested action

The minimal fix: replace `Outcome.SuccessCase<UserDto> success` with `UserDto success`. Keep switching on `result` (not `.Value`), and keep it discard-free — the original form was already fine except for the fictitious wrapper type name.
