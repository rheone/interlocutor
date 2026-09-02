Status: needs-triage
Severity: MEDIUM
Category: architecture / test-coverage
Source: architecture.md lane, confirmed by antagonist lane

# `DependencyTests.cs` still has layering gaps despite a "done" ticket claiming they were closed

## Summary

`.scratch/audit-followups/issues/06-complete-dependencytests-layering-assertions.md` is marked "Status: done" with all acceptance boxes checked, but the current file still:
1. Never asserts `Interlocutor.Persistence.EntityFrameworkCore`/`Interlocutor.Persistence.NHibernate` don't depend on `Interlocutor.FluentValidation` — an explicit, checked-off acceptance criterion of that ticket.
2. Never checks `Interlocutor.CommonKernel` against `Interlocutor.DependencyInjection.Abstractions/.Microsoft/.Autofac` or `Interlocutor.Persistence.Abstractions` — all four exist as static `Assembly` fixtures at the top of the same file already, so extending the list is a one-line change.

## Evidence

`PersistenceEntityFrameworkCore_ShouldDependOnPersistenceAbstractions_AndNotOnPersistenceNHibernate_Test` (lines 209-237) and its NHibernate counterpart (239-262) each check only the sibling store package, never `FluentValidation`. `CommonKernel_ReferencedAssembly_HasNoDependencyOnOtherInterlocutorPackages_Test` (lines 29-49) checks exactly 5 packages, omitting the 4 named above. CLAUDE.md's rule for CommonKernel is unqualified ("Must never depend on any other Interlocutor package").

Consequence: a future change that quietly adds an EF Core/NHibernate → FluentValidation reference, or a CommonKernel → DI-package reference, would build and pass `dotnet test` without this suite catching it, contrary to what the closed ticket claims is guaranteed.

## Suggested action

Add `FluentValidation` to both `Persistence.EntityFrameworkCore`/`Persistence.NHibernate` `ShouldNot` lists, and widen `CommonKernel`'s `ShouldNot` list to the four newer assemblies already available as fixtures in this file. Then either reopen `.scratch/audit-followups/issues/06-complete-dependencytests-layering-assertions.md` or leave a note that its acceptance criteria weren't durable across later package additions.
