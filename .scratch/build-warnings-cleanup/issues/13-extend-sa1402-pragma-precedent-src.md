# 13 - Extend SA1402 pragma-suppression precedent to remaining src/ files

**What to build:** `src/Interlocutor.CommonKernel/ResultTypes/` already uses `#pragma warning disable SA1402` / `restore` (with an inline "Allowing multiple types of same name with different arity" comment) to co-locate same-named/different-arity types instead of splitting into separate per-arity files. Three more `src/` files carry genuine, unsuppressed `SA1402` violations of the same shape:

- `src/Interlocutor.Abstractions/Commands/ICommand.cs`
- `src/Interlocutor.Abstractions/Pipeline/Classifiers/OutcomeClassifierBuilder.cs`
- `src/Interlocutor.CommonKernel/ResultTypes/Failures.cs`

Apply the same narrowly-scoped pragma pattern to each, matching the existing comment style - not splitting the files, and not relaxing `SA1402` in `.editorconfig`. `SA1402` stays fully enforced everywhere else in `src/`.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Each of the three files gets a `#pragma warning disable SA1402` / `restore SA1402` pair scoped to just the offending declarations, with an inline comment explaining why (matching the `ResultTypes/` precedent's wording)
- [ ] `dotnet build` reports zero `SA1402` warnings for these three files
- [ ] No other `src/` `SA1402` violations were introduced or newly suppressed
- [ ] `dotnet test` passes for `Interlocutor.Abstractions.Tests` and `Interlocutor.CommonKernel.Tests`
