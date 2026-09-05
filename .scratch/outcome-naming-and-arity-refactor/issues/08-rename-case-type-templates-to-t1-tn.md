# 08 - Rename `Outcome<...>`'s and `ResultExtensionsEmitter`'s case-type templates to `T1..TN`

**What to build:** `Interlocutor.CommonKernel/Outcome.cs` names every shipped arity's case types so the first one always reads as "the" success case (`TSuccessResult`), even though an `IOutcome`-implementing union may not have anything resembling a success case at all - the same universal assumption `outcome-classifier-retirement` is removing from `IsSuccess`/`IsFailure` elsewhere. Rename every case-type parameter across all six shipped arities (the two-case `Outcome<TSuccessResult, TFailureResult>` through the six-case form) to a fully symmetric `T1, T2, ..., TN` scheme - no case name implies a privileged role. Do the same for the generator template that produces `Interlocutor.AspNetCore.ResultExtensions`'s extension members: `ResultExtensionsEmitter`'s hardcoded case-name array (`arity == 2 ? ["TSuccess", "TFailure"] : ["TSuccess", .. Enumerable.Range(2, arity - 1).Select(static i => $"TCase{i}")]`) becomes `T1..TN` the same way.

This is a pure identifier rename with no behavioral change: C# has no named-type-argument syntax, so nothing that closes `Outcome<Foo, Bar>` references a generic parameter by name - the blast radius is limited to declaration-site doc comments, `PublicAPI.Unshipped.txt` entries (which do encode parameter names as text), and this repo's own prose. Matches this repo's existing precedent for pre-release renames (no shipped consumers yet) - no compatibility shim.

Leave `ResourceCommandHandler<TCommand, TEntity, TSuccess>`, `AuthorizationBehavior<TRequest, TSuccess>`, and `OutcomeClassifierBuilder.RegisterSuccessType<TSuccess>` untouched - each names a real, contractual success payload for that specific construct, not a positional union-case template.

**Blocked by:** `outcome-classifier-retirement` issues 01-05 (same `Outcome.cs`/`messages-and-result-types.md` member-set prose the `IsSuccess` removal touches - land that first to avoid rework).

**Status:** ready-for-agent

- [ ] All six arities of `Outcome<...>` in `Interlocutor.CommonKernel/Outcome.cs` (two-case through six-case) declare `T1..TN` generic parameters instead of `TSuccessResult`/`TCase2`.../`TFailureResult`; every XML doc comment on those declarations is updated to match - describe case 1 as a convention the shipped family follows, not a compiler-enforced meaning.
- [ ] `ResultExtensionsEmitter`'s case-name template array emits `T1..TN` instead of `["TSuccess", "TFailure"]`/`["TSuccess", "TCase2", ...]`; the generated `Interlocutor.AspNetCore.ResultExtensions` extension members and their doc comments reflect the new names.
- [ ] `ResourceCommandHandler`, `AuthorizationBehavior`, and `OutcomeClassifierBuilder.RegisterSuccessType` are unchanged - confirm with `rg -n "TSuccess"` that only their three declarations (and any doc-comment prose describing them) still use that name.
- [ ] `PublicAPI.Unshipped.txt` (`Interlocutor.CommonKernel`) updated for every renamed generic parameter.
- [ ] `documentation/messages-and-result-types.md` and any other `documentation/` page naming `TSuccessResult`/`TCase2`... on the shipped family or on `ResultExtensions` is updated to `T1..TN`.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.
