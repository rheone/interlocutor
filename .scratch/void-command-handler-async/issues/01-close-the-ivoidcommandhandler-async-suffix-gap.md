# 01 - Close the `IVoidCommandHandler` async-suffix gap

**What to build:** `IVoidCommandHandler<TCommand, TResult>` converges on the same `*Async` convention every other dispatch interface in the mediator already uses (`ICommandHandler.HandleAsync`, `IQueryHandler.HandleAsync`, `INotificationHandler.HandleAsync`, `IPipelineBehavior.HandleAsync`, `ISender.SendAsync`, `IPublisher.PublishAsync`, `IUnitOfWork.CancelAsync`). This closes the one deliberate exception the async-method-naming rename (`.scratch/async-method-naming/`) left open, and removes the workarounds that exception forced elsewhere: `Interlocutor.Generator`'s `DispatchWrapperEmitter` had to special-case void-command handlers with a per-kind branch (`handler.Kind == HandlerKind.VoidCommand ? "Handle" : "HandleAsync"`) purely because this one interface lagged behind, and a "known gap" comment was left on every affected fake across the test suite.

The blast radius is small - one production implementer (`TutorialProject`'s `ClearCompletedTodoTasksHandler`), roughly six test fakes/fixtures, and two framework call sites (`VoidCommandHandlerWrapper`, `DispatchWrapperEmitter`) - so this lands as a single, straight rename rather than an expand/migrate/contract sequence: no shipped consumers exist yet to protect mid-flight.

**Blocked by:** None - can start immediately.

**Status:** done - `IVoidCommandHandler` was renamed to `IBareCommandHandler` (see `IBareCommandHandler.cs`), which already declares `HandleAsync` with no `VSTHRD200` suppression; this ticket's goal was reached by that rename.

- [ ] `IVoidCommandHandler<TCommand, TResult>` declares `HandleAsync` instead of `Handle`; the old member and its `VSTHRD200` suppression (`#pragma warning disable/restore VSTHRD200` around the declaration) are removed.
- [ ] `TutorialProject`'s `ClearCompletedTodoTasksHandler` implements `HandleAsync` instead of `Handle`.
- [ ] Every test fake/fixture implementing or declaring `IVoidCommandHandler<,>` implements/declares `HandleAsync` instead of `Handle` - across `tests/Interlocutor.Abstractions.Tests` (`TracingSuccessCommandHandler`, `TestVoidHandler`), `tests/Interlocutor.DependencyInjection.Abstractions.Tests` (`SampleVoidCommandHandler`), `tests/Interlocutor.DependencyInjection.Autofac.Tests` and `tests/Interlocutor.DependencyInjection.Microsoft.Tests` (`GeneratedSwitchVoidCommandHandler`), and `tests/Interlocutor.Generator.Tests`' in-memory source-string fixtures (`RequestDispatchSwitchEmissionTests`, `MissingHandlerDiagnosticTests`).
- [ ] `VoidCommandHandlerWrapper<TCommand, TResult>` calls `handler.HandleAsync(...)` instead of `handler.Handle(...)`.
- [ ] `Interlocutor.Generator`'s `DispatchWrapperEmitter` drops its `HandlerKind.VoidCommand` special case and emits the same `HandleAsync` call for every handler kind, uniformly.
- [ ] Every "known gap" comment this exception left behind is removed now that the gap is closed - `IVoidCommandHandler`'s own XML remarks, `VoidCommandHandlerWrapper`, `DispatchWrapperEmitter`, and the explanatory comments on each of the test fakes listed above.
- [ ] A clean solution build shows zero `VSTHRD200` diagnostics anywhere in `src/`, `TutorialProject`, or `tests/` - no exception/pragma needed for this interface any more, consistent with every other dispatch interface.
- [ ] Full test suite passes with no behavior change.
