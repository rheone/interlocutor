# 02 - Custom IOutcome example: SendWelcomeEmail unit command

Status: ready-for-agent

## Labels

ready-for-agent

## What to build

Add a new, standalone feature to `TutorialProject` (under the `Users/` vertical slice) that demonstrates `IOutcome` is an extension point, not just the compiler-generated `Outcome<...>` union - `documentation/messages-and-result-types.md` already documents the mechanism (a hand-rolled `IOutcome` implementer plus `OutcomeCaseRegistryBuilder`) but no runnable instance of it exists in the tutorial.

The feature: a `SendWelcomeEmail` unit command whose handler calls a fake third-party mailer-SDK-style client - a small in-repo stand-in with its own success/failure result shape, the way a real third-party client library would (consistent with `TutorialProject`'s existing precedent of small fake service abstractions for things it doesn't want to build for real, e.g. `ICompletionCounter`). The handler adapts that SDK-style result directly into a hand-rolled `IOutcome` implementer (not a compiler-generated `union`), and registers it with the pipeline's outcome-case registry so `AuthorizationBehavior`/`ValidationBehavior`/etc. can still construct and inspect its cases without knowing its concrete shape.

Keep this off the main CRUD paths - it's not triggered by `CreateUser` or any other existing flow. It should read as "here's a deliberate example of the extension point," not as the tutorial's house style for handler responses.

## Acceptance criteria

- [ ] `SendWelcomeEmail` is a real, working, tested feature (command, handler, endpoint, fake mailer-SDK client, tests) following `TutorialProject`'s existing project conventions (DI registration, FluentValidation if applicable, xunit.v3/NSubstitute tests)
- [ ] The handler's response type is a hand-rolled `IOutcome` implementer, not a compiler-generated `Outcome<...>` union
- [ ] The custom `IOutcome` type is registered with the pipeline's outcome-case registry (factory + extractor) rather than left for the default registry to fail to recognize
- [ ] The feature is not wired into any existing flow (`CreateUser`, etc.) - it has its own dedicated trigger
- [ ] `dotnet test` passes for the new feature's tests
- [ ] One top-level public type per file, filenames match type names, per house style
- [ ] Rich XML doc comments on public members, per house style

## Blocked by

None - can start immediately.
