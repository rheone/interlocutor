# 02 — Retire `IOutcome` as the response constraint in `FluentValidation`, `AspNetCore`, and `DependencyInjection.Abstractions`

**What to build:** the remaining `where TResponse : IOutcome` constraint sites outside `Interlocutor.Abstractions` - in `Interlocutor.FluentValidation` (`ValidationBehavior`, `FluentValidationErrorsBehavior`), `Interlocutor.AspNetCore` (`ResultExtensions`), and `Interlocutor.DependencyInjection.Abstractions` (`RootScopeGuardedMediator`) - become `IUnion`, consistent with ticket 01. The documentation surfacing this constraint to consumers is updated in the same ticket.

**Blocked by:** 01 (these packages consume `Interlocutor.Abstractions`'s updated handler-contract shape).

**Status:** ready-for-agent

- [ ] Every `where TResponse : IOutcome` site in `Interlocutor.FluentValidation`, `Interlocutor.AspNetCore`, and `Interlocutor.DependencyInjection.Abstractions` reads `IUnion` instead.
- [ ] `documentation/messages-and-result-types.md`'s "IOutcome Constraint" section is rewritten to describe the `IUnion` constraint, and its "IOutcome as an Extension Point" section is updated to reflect that a hand-rolled response type now only needs to satisfy `IUnion` to be legal as `TResponse` - not implement a project-owned marker - while still noting `IOutcome`/`IOutcomeCase` remain available as an opt-in for the classification surface.
- [ ] `documentation/messages-and-result-types.md`'s "Variance: Response Type Is Never Variant" section's code samples and prose are updated to reference `IUnion` wherever they currently reference `IOutcome` as the constraint.
- [ ] `documentation/extensibility.md`'s "IOutcome as an Extension Point" section is updated to match.
- [ ] `documentation/glossary.md`'s `IOutcome` entry is updated to describe its narrowed role (classification-surface opt-in, no longer the response constraint) rather than removed.
- [ ] Every existing test in `tests/Interlocutor.FluentValidation.Tests`, `tests/Interlocutor.AspNetCore.Tests`, and `tests/Interlocutor.DependencyInjection.Abstractions.Tests` continues to pass unmodified in behavior.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.
