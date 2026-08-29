# 13 - Extend SA1402 pragma-suppression precedent to remaining src/ files

**What to build:** `src/Interlocutor.CommonKernel/ResultTypes/` already uses `#pragma warning disable SA1402` / `restore` (with an inline "Allowing multiple types of same name with different arity" comment) to co-locate same-named/different-arity types instead of splitting into separate per-arity files. Three more `src/` files carry genuine, unsuppressed `SA1402` violations of the same shape:

- `src/Interlocutor.Abstractions/Commands/ICommand.cs`
- `src/Interlocutor.Abstractions/Pipeline/Classifiers/OutcomeClassifierBuilder.cs`
- `src/Interlocutor.CommonKernel/ResultTypes/Failures.cs`

Apply the same narrowly-scoped pragma pattern to each, matching the existing comment style - not splitting the files, and not relaxing `SA1402` in `.editorconfig`. `SA1402` stays fully enforced everywhere else in `src/`.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Each of the three files gets a `#pragma warning disable SA1402` / `restore SA1402` pair scoped to just the offending declarations, with an inline comment explaining why (matching the `ResultTypes/` precedent's wording)
- [x] `dotnet build` reports zero `SA1402` warnings for these three files
- [x] No other `src/` `SA1402` violations were introduced or newly suppressed
- [x] `dotnet test` passes for `Interlocutor.Abstractions.Tests` and `Interlocutor.CommonKernel.Tests`

## Comments

Already implemented in commit `56141a9`. Verified: all three files (`ICommand.cs`, `OutcomeClassifierBuilder.cs`, `Failures.cs`) carry a `#pragma warning disable SA1402` / `restore` pair scoped to the offending declarations, each with its own accurate inline reason rather than a blind copy-paste of the `ResultTypes/` precedent's wording (`OutcomeClassifierBuilder.cs`'s reason - "allowing this small support type to stay co-located with the builder that owns it" - correctly describes its different shape from the same-name/different-arity case the other two share). Zero `SA1402` warnings for these three files in a full rebuild, and no other `src/` `SA1402` suppression was introduced. `Interlocutor.Abstractions.Tests`/`Interlocutor.CommonKernel.Tests` pass.
