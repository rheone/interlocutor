> **Status: done**

## Labels

ready-for-agent

## What to build

Cleanup pass over `documentation/pipeline-and-behaviors.md`, `documentation/mediator-lifetimes-and-scope.md`, `documentation/notifications-and-fan-out.md`, `documentation/runtime-typed-dispatch.md` following the standing decisions in `../PRD.md`. `pipeline-and-behaviors.md` currently gives `IHasTimeout`/`IRateLimited`/`ICancellable` only passing mentions compared to the deeper treatment other markers get elsewhere - consider whether each deserves a short dedicated subsection here, and add the `[Obsolete]` warning alerts for `Cancelled`/`RateLimited`/`Timeout` if this is a better home for them than unit 02's `messages-and-result-types.md` (pick one canonical home, cross-link from the other file instead of duplicating the alert).

## Acceptance criteria

- [x] No stale language remains
- [x] Every claim verified against current `src/Interlocutor.Abstractions` pipeline/marker types
- [x] `IHasTimeout`/`IRateLimited`/`ICancellable` get proportionate coverage - already had dedicated subsections in `pipeline-and-behaviors.md`; the original fact-finding pass undersold this file
- [x] Glossary-first-mention links added where appropriate

## Findings beyond the original scope

Verifying `mediator-lifetimes-and-scope.md`'s and `runtime-typed-dispatch.md`'s declared interfaces against `src/Interlocutor.Abstractions` turned up real bugs, now fixed:

- `ISender`'s unit-command overload was shown as non-generic (`Task<OperationOutcome> Send(ICommand, ct)`), and `IPublisher.Publish` was shown as non-generic with no `PublishStrategy` overload at all. Both are actually generic (`Send<TResult>(ICommand, ct)`, `Publish<TNotification>(TNotification, ct)` / `Publish<TNotification>(TNotification, PublishStrategy, ct)`). Also added `ISender`'s runtime-typed `Send(object, ct)` overload, which was missing entirely.
- Bigger: `messages-and-result-types.md`'s "ICommand (Unit Command)" section (from unit 02) claimed a unit command always returns the non-generic `OperationOutcome`. It doesn't - `IVoidCommandHandler<TCommand, TResult>` lets the outcome type be chosen **per call site** via `Send<TResult>`, not fixed by the command. Its own dispatch example (`var result = await mediator.Send(new DeleteAllUsers())`) wouldn't even compile, since `TResult` can't be inferred without an explicit type argument or target type. Fixed both the explanation and the example; mirrored the correction into `glossary.md`'s "Command"/"Unit command"/"Handler" entries, which also cited a nonexistent single-arity `ICommandHandler<TCommand>` (should be `IVoidCommandHandler<TCommand, TResult>`).
- `runtime-typed-dispatch.md` was built on a false premise throughout: it declared `IOperationOutcome` with `IsSuccess`/`Failure` members and used `result.IsSuccess`/`result.Failure` in every example. `IOperationOutcome` is actually a bare marker interface with zero members - none of that code would compile. The real mechanism for inspecting an outcome without knowing its concrete type is `IOutcomeCaseRegistry.TryGetActiveCase`. Rewrote the interface declaration and every downstream example (Outbox Pattern) to use it.
- `RequestContext` (from unit 02's neighborhood, caught while cross-checking `IRequestContextAccessor`) was declared with `CorrelationId` as nullable with no default and `Items` as a mutable `IDictionary<string, object?>`; the real type has `CorrelationId` non-nullable with an auto-generated GUID default, and `Items` as `IReadOnlyDictionary<string, object>?`. Fixed.
- `notifications-and-fan-out.md`'s "Error Handling in Notifications" section fully re-explained Sequential/Parallel failure semantics already covered (with a diagram) earlier in the same file. Trimmed to just the new content (a swallow-and-log handler pattern), removing ~25 lines of duplication.
