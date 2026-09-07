# 02 - Migrate TutorialProject and test-project usages

**What to build:** Every fake, test double, and `TutorialProject` command/query that implements one of the four renamed marker interfaces (`IRequiresPolicy` → `IRequiresPolicyRequest`, `ICancellable` → `ICancellableRequest`, `IHasTimeout` → `ITimeoutRequest`, `IRateLimited` → `IRateLimitedRequest`) is updated to implement the new name, so the solution builds and every existing test still passes with no behavior change.

**Blocked by:** 01.

**Status:** done - implemented in `ebdd4cd`.

- [ ] All references in `tests/Interlocutor.Abstractions.Tests`, `tests/Interlocutor.DependencyInjection.Abstractions.Tests`, `tests/Interlocutor.DependencyInjection.Autofac.Tests`, and `tests/Interlocutor.DependencyInjection.Microsoft.Tests` are updated to the new names.
- [ ] Any `TutorialProject/TodoTasks.Api` command/query implementing one of the four old markers, and its corresponding `tests/TodoTasks.Api.Tests` coverage, is updated to the new name.
- [ ] `dotnet test` passes solution-wide with no test changes beyond the identifier rename (no assertions or behavior altered).
- [ ] No reference to the old four names remains anywhere under `tests/` or `TutorialProject/`.
