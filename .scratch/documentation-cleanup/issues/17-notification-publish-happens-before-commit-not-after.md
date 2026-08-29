Status: done

## Labels

needs-triage

## What to build

`documentation/transactions-and-store-integration.md`'s "Transactions with Notifications" section states, with a code comment as evidence, that calling `SaveChangesAsync` mid-handler is what commits the transaction:

```csharp
await context.SaveChangesAsync(ct);

// At this point, the transaction commits (UnitOfWorkBehavior)
// Then notification handlers run outside the transaction
await this.publisher.Publish(new OrderCreated(order.Id, /* ... */), ct);
```

This does not match `UnitOfWorkBehavior<TRequest, TResponse[, TContext]>`'s actual shipped implementation (verified against both the EF Core and NHibernate variants): the behavior commits *after* `next()` returns - i.e., after the handler's entire `Handle` method, publish call included, has already run to completion - not when `SaveChangesAsync`/`IUnitOfWork.SaveChangesAsync` is called partway through the handler. `SaveChangesAsync` only flushes pending changes to the open transaction; it does not commit it (confirmed independently while building `TutorialProject/TodoTasks.Api`'s `DeleteUserHandler` persistence test - see `.scratch/tutorial-project/issues/11-users-feature-delete-multi-repo-cascade.md`'s Comments for how that surfaced).

Two consequences worth checking, not just the doc prose:

- The claim "a notification handler failure does not roll back the command" may be backwards for the common case shown (`Publish` called from inside `Handle`, before it returns): if a subscriber throws, the exception propagates out of `next()` before `UnitOfWorkBehavior` ever reaches its commit/rollback decision, so the transaction is left open and gets implicitly rolled back when the session disposes - the opposite of the guarantee currently documented.
- `TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/CompleteTodoTaskHandler.cs` carries the same claim in its own XML doc remarks ("Persist first, publish second... after this handler's own transaction has already committed") - same likely inaccuracy, same root cause, needs the same fix once the actual intended semantics are confirmed.

## Acceptance criteria

- [x] Confirm against `UnitOfWorkBehavior`'s actual source (both `Interlocutor.Persistence.EntityFrameworkCore` and `Interlocutor.Persistence.NHibernate` variants) exactly when the commit happens relative to a `Publish` call made from inside a handler
- [x] Either fix `documentation/transactions-and-store-integration.md`'s example/prose to describe the real ordering (commit happens after the whole handler, including any in-handler `Publish` call, returns - so a handler that wants its notification to genuinely run post-commit needs to publish from the *caller* of `Send`, not from inside the handler, unless there's a mechanism this note is missing), or confirm there's a mechanism (e.g. a pipeline-level deferred-publish behavior) that actually delivers the documented guarantee and point to that instead
- [x] Fix `TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/CompleteTodoTaskHandler.cs`'s matching doc-comment claim to match whatever the confirmed real behavior turns out to be
- [x] House style: no HTTP status codes outside `aspnetcore-integration.md`, no `maintainer_docs/` citations

## Blocked by

None - can start immediately, but needs the source-level confirmation step done first before any doc prose is rewritten

## Comments

- Confirmed against both `UnitOfWorkBehavior` variants (`Interlocutor.Persistence.EntityFrameworkCore` and `Interlocutor.Persistence.NHibernate`): both commit strictly after `next()` returns, i.e. after the handler's `Handle` method itself returns - not at the point `SaveChangesAsync`/`IUnitOfWork.SaveChangesAsync` is called mid-handler. There is no deferred-publish mechanism that delivers the previously-documented guarantee for an in-handler `Publish` call; option (a) it is.
- No pipeline-level fix exists (and none was added) - the correct pattern for a genuinely-after-commit publish is publishing from the *caller* of `Send`, once it returns, not from inside the handler. Both `documentation/transactions-and-store-integration.md`'s "Transactions with Notifications" section and `documentation/beginners-guide-todo-api.md` now show and explain that pattern alongside the (still shown, still used) in-handler pattern and its real trade-off - a subscriber failure rolls back the command's own transaction too, not the "safely decoupled" behavior a reader might assume from the word "notification."
- Fixed every occurrence of the false claim found across the repo, not just the one originally flagged: `documentation/transactions-and-store-integration.md` (the main example + a rollback-guarantee sentence), `documentation/beginners-guide-todo-api.md` (the "What you'll build" bullet, the Step 8 code comment, the sequential-handlers paragraph, the multi-repository Bonus section's intro sentence and its `[!IMPORTANT]` callout), `documentation/index.md`'s blurb, and `TutorialProject/TodoTasks.Api/Features/Tasks/CompleteTodoTask/CompleteTodoTaskHandler.cs` and `TodoTaskCompletedNotification.cs`'s own doc comments.
- Deliberately did **not** change any actual runtime behavior (`CompleteTodoTaskHandler` still publishes from inside `Handle`, matching the guide's own Step 8 example) - this was a documentation-accuracy ticket, not a request to redesign the notification-timing behavior. The corrected docs now show *both* the existing in-handler pattern (with its real trade-off honestly stated) and the alternative caller-side pattern that actually achieves the previously-claimed guarantee, so a reader can choose deliberately instead of relying on a false assumption.
