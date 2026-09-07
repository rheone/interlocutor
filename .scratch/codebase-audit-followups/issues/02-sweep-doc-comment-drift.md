# 02 - Sweep doc/comment drift across CommonKernel, Abstractions, FluentValidation

**What to build:** Clean up a set of small, independent doc/comment inaccuracies found during the full-codebase standards audit:

- `CLAUDE.md`'s "Result-type convention" section cites `README.md § "Result Types Are Building Blocks"`, but that heading no longer exists in the current README - either restore the section in README.md or repoint/rephrase the CLAUDE.md citation to match the README's actual current structure.
- The copy-pasted comment typo "differet arity" appears verbatim in six `#pragma warning disable SA1402` comments (`Failure.cs`, `Failures.cs`, `NotFound.cs`, `ValidationError.cs`, `ValidationErrors.cs`, `Commands/ICommand.cs`) - fix the typo everywhere it appears.
- `DefaultResponseOutcomeDescriber`'s and `OutcomeClassifierBuilder`'s constructors carry a leftover generic doc-stub sentence ("Initializes a new instance of the X class.") alongside their real doc comment - remove the stub sentence, keep the real one.
- `Interlocutor.FluentValidation.csproj` has a comment claiming `DependencyInjection.Abstractions is auto-available in .NET 11 SDK` above a `PackageReference` block that doesn't reference that package at all - remove the stale comment.

**Blocked by:** None - can start immediately

**Status:** done

- [x] CLAUDE.md's README citation matches an actual current README.md heading (or the heading is restored)
- [x] "differet arity" no longer appears anywhere in the repo; the intended wording reads correctly in all six files
- [x] The leftover generic doc-stub sentence is gone from both constructors' doc comments, real doc comment retained
- [x] The stale dependency comment is removed from `Interlocutor.FluentValidation.csproj`
- [x] `dotnet build` succeeds; no new doc-warning suppressions needed

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: `differet arity` no longer appears anywhere in the tree; stale README/csproj citations were fixed. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
