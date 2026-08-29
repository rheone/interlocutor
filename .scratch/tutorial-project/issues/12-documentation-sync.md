Status: open

## Labels

ready-for-agent

## What to build

Bring the documentation that references the old `samples/` project (deleted in ticket 01) in line with the new `TutorialProject/TodoTasks.Api`, per this repo's own `maintainer_docs/agents/documentation-maintenance.md` policy (docs updated in the same effort as the code change that makes them stale, not deferred). Use the `github-markdown` skill for the prose and `mermaid-diagram-generator` only if a diagram genuinely clarifies something the existing prose doesn't.

- **`documentation/sample-app-walkthrough.md`**: currently describes `samples/Interlocutor.Sample.Handlers`/`Interlocutor.Sample.WebApi` (EF Core, generic handlers, cross-assembly discovery, an explicitly-composed non-recommended pipeline). Rewrite it to walk through `TutorialProject/TodoTasks.Api` instead: its vertical-slice-by-directory structure, the User/TodoTask multi-repository unit-of-work example (ticket 11) as the walkthrough's own highlight (this project doesn't demonstrate cross-assembly discovery or a hand-composed pipeline the way the old sample did - don't claim it does; describe what it actually demonstrates instead, per this repo's House Style truthfulness rule). Every code excerpt must compile against the shipped project - verify against the actual files, don't carry forward anything from the old sample.
- **`CLAUDE.md`**: update the `samples/` bullet under "Interlocutor" (currently: "`samples/` is a runnable worked example (cross-assembly handler discovery, an explicitly composed pipeline, a multi-subscriber notification) - see `documentation/sample-app-walkthrough.md`...") to describe `TutorialProject/` instead, matching what it now actually contains.
- **`documentation/beginners-guide-todo-api.md`**: the `[!NOTE]` in its "What you'll build" section currently says "this guide is documentation, not a shipped sample - unlike `samples/`, there's no runnable project behind it... The domain and package choices here are independent of that sample; don't expect the two to share code." That's no longer true once `TutorialProject/TodoTasks.Api` exists - it uses the *same* stack (NHibernate, Autofac, FluentValidation) and implements this exact guide. Correct the note to point at `TutorialProject/TodoTasks.Api` as the runnable counterpart to this guide instead of contrasting against it. Do not otherwise touch the guide's own content (it's the spec every earlier ticket implemented against - it should already be accurate).
- **`documentation/index.md`** (line ~59) and **`documentation/transactions-and-store-integration.md`** (two references, ~lines 105 and 287) also link into the old `samples/Interlocutor.Sample.WebApi` (found during ticket 01's pre-delete reference sweep - broader than originally scoped here). Update `index.md`'s "Sample App Walkthrough" blurb to describe what the rewritten walkthrough now actually covers. `transactions-and-store-integration.md`'s two citations point at `SampleUnitOfWorkBehavior.cs` and `DuplicateTitleGuardStage` specifically - re-point each at the equivalent real thing in `TutorialProject/TodoTasks.Api` if one exists (ticket 11's `DeleteUserHandler` for the unit-of-work citation; there is no pipeline-stage short-circuit example in the new project, so either drop that citation or reword the surrounding prose to make the point without a runnable-code pointer - don't leave a dead link either way).
- **`tests/Interlocutor.DependencyInjection.Microsoft.EntityFrameworkCore.Tests/UnitOfWorkBehaviorArityTests.cs`** (~line 103) has an XML doc comment citing `samples/Interlocutor.Sample.WebApi/SampleUnitOfWorkBehavior.cs` as a real-world precedent. It's just a doc-comment string (doesn't affect compilation), but update it to stop citing a deleted file - reword to describe the pattern generically, or drop the specific path.
- **`maintainer_docs/research/native-aot-and-trimming-options.md`** also references the old sample repeatedly, but it's a point-in-time empirical research record (per this repo's ADR-immutability convention for historical records) - leave it untouched.

## Acceptance criteria

- [ ] `documentation/sample-app-walkthrough.md` accurately describes `TutorialProject/TodoTasks.Api` as it actually exists after tickets 01-11, with no leftover references to the deleted EF Core sample
- [ ] `CLAUDE.md`'s `samples/` reference is updated to `TutorialProject/`
- [ ] `documentation/beginners-guide-todo-api.md`'s "unlike samples/..." note is corrected to reference the new runnable project instead of contradicting its existence
- [ ] Every code excerpt in the rewritten walkthrough compiles against the actual shipped `TutorialProject/TodoTasks.Api` code
- [ ] House style respected: no enumerated counts in prose (except closed sets), no HTTP status codes outside `aspnetcore-integration.md`, glossary-linked on first mention, no citations to `maintainer_docs/`

## Blocked by

06, 07, 08, 09, 10, 11
