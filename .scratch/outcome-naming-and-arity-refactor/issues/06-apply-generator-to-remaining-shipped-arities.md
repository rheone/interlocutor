# 06 - Apply the generator to the rest of the shipped family

**What to build:** Ticket 05 proved the attribute/generator mechanism on `Outcome<TSuccess, TFailure>`. Apply the same attribute to every remaining shipped shape: the non-generic `Outcome` (`Success | Failure`, no type parameters - a degenerate case of the same template, with concrete case types instead of open ones) and `Outcome<TSuccessResult, TCase2, TCase3>` through `Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6>`. Delete every hand-written `IsSuccess`/`GetValue()` this replaces - after this ticket, `OperationOutcome.cs` (or its post-rename equivalent) should contain attributed `partial union` declarations only, with zero hand-maintained member bodies left for the generator to duplicate or drift from.

**Blocked by:** Ticket 05.

**Status:** ready-for-agent

- [ ] The non-generic `Outcome` and every generic arity (`Outcome<TSuccessResult, TFailureResult>` through the six-case form) are attributed `partial union` declarations, each with the full generated member set (`Match`, `Switch`, per-case `TryGetValue`, `ToString`, `IsSuccess`) and no hand-written equivalent left behind.
- [ ] The now-unused hand-written `GetValue(): object?` methods (arity three and up) are removed - `TryGetValue` is their generated replacement; note in the ticket's landing commit if any existing caller needs updating to the new API shape.
- [ ] Existing arity-specific test classes (renamed in Ticket 01, e.g. `OutcomeOfThreeCasesTests`) are extended to cover the same `Match`/`Switch`/`TryGetValue`/`ToString` surface Ticket 05 established for the two-case form, for every arity.
- [ ] `documentation/messages-and-result-types.md`'s `## Outcome Unions` section reflects that every shipped arity now carries the same generated member set, not just the two-case form.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.

