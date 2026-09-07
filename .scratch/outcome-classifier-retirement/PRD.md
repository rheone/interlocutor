# PRD: Retire `IOutcomeClassifier`'s success/failure judgment; explicit transaction disposition

Status: **not started**. Tracked as issues under `.scratch/outcome-classifier-retirement/issues/`.

## Problem

`IOutcomeCase.IsSuccess` and `IOutcomeClassifier.IsFailure` both assert that every `IOutcome` has a universal, inferable success/failure meaning - `IOutcomeCase.IsSuccess` by convention ("case 1 is always success"), `IOutcomeClassifier` by a seeded per-kernel-type dictionary plus a fail-closed default (unregistered = failure) for anything it doesn't recognize. A long design conversation (see this session's transcript) concluded this is backwards: a case type's name carries no inherent transactional or business meaning - `Conflict` doesn't intrinsically mean "roll back," `Success` doesn't intrinsically mean "commit," and a handler is free to construct or `Eject` with either at any point without that implying anything about how the operation should conclude. The one real consumer that needs a binary answer - `CommitGateDriver`'s commit-vs-rollback decision - was getting it by inspecting the response's type, which is the exact "framework asserts a universal judgment" problem the rest of this design work has been removing everywhere else (see `.scratch/pipeline-case-fidelity/`).

Two things made this decision tractable rather than merely aspirational:
- `TResponse` reaching any pipeline behavior is *never* actually arbitrary - `ICommand<TResponse>`/`IQuery<TResponse>` already constrain it to `IOutcome` at compile time ([ICommand.cs:23-24](../../src/Interlocutor.Abstractions/Commands/ICommand.cs#L23-L24)) - so the residual gap this whole classifier apparatus exists for is narrower than "any object": it's specifically a hand-rolled, non-`union` `IOutcome` implementation, or (for `CommitGateDriver` specifically) a genuinely non-`IOutcome` response since that driver carries no such constraint.
- `IUnitOfWork.Cancel()` ([IUnitOfWork.cs:43-63](../../src/Interlocutor.Persistence.Abstractions/IUnitOfWork.cs#L43-L63)) already ships the explicit, ambient "don't commit" signal this redesign needs - "a rare-case escape hatch for a handler that wants to force a rollback without being obligated to also return a classified failure-shaped `Outcome`." ADR-0018 already started down this road; this work finishes it.

## Decision

- Drop `IsSuccess` from `IOutcomeCase` (keep `ActiveCaseType` - still needed by `IOutcomeCaseRegistry`/AspNetCore's HTTP mapping to identify *which* case is active, independent of any success/failure judgment).
- Drop `IsFailure` from `IOutcomeClassifier` entirely, and the kernel-type seed registrations in `OutcomeClassifierBuilder` that exist only to answer it. `GetLabel`/`Describe` survive as a pure, lenient labeling concern (never throws - a display string for logs/audit is not a correctness decision).
- `CommitGateDriver` stops calling the classifier. New model: **commit by default**, blocked only by an explicit no-commit signal (a call-site override on return/`Eject`, or the already-shipped `IUnitOfWork.Cancel()`) or a thrown exception (relies on the store session rolling back an uncommitted transaction on disposal - an assumption to pin down with an explicit test, not leave implicit).
- AspNetCore's generic catch-all HTTP mapping (`ResultExtensions.ExtractCase`) keeps a small default mapping set as a convenience, but throws when it genuinely can't map a case to a status code - HTTP-status concerns stay entirely inside `Interlocutor.AspNetCore`, per house style.
- Case *construction* (`IOutcomeCaseRegistry`/`ConstructOrThrow`) is unaffected and stays exactly as it is - already limited to a response's actually-declared case types, which is the correct, unrelated half of this design.

## Explicitly rejected / deferred

- **Folding `IsSuccess` into `IOutcome` as a required member** ("fix the classifier by making every outcome state its own truth") - rejected outright, not just costed out: it still forces a universal success/failure judgment onto every declared type, which is exactly the assumption this whole PRD removes. A result may be neither success nor failure in any general sense - it's just the declared type the handler chose to return.
- **Eliminating `ITransactionalRequest` in favor of a purely handler-local eager-begin call** - deferred, not rejected. When this was written, a validator needing a DB-backed check-then-act read (e.g. a uniqueness check) ran *before* the handler with no way to share its later transaction, so a handler-local call couldn't retroactively protect that earlier read. [ADR-0018](../../maintainer_docs/adr/0018-unit-of-work-as-transaction-owner.md) has since closed that gap generically - the scoped session is pipeline-wide from dispatch start, and `ITransactionalRequest`'s eager-begin now protects whatever runs after wherever `AddUnitOfWorkBehavior()` is registered, validation included, once registered outside it. Issue 07 (the research ticket this bullet pointed to) closed `wontfix` on that basis; whether `ITransactionalRequest` itself is still worth retiring given the now-generic reordering mechanism is undecided and not reopened here.

## Verification

- `dotnet build`/`dotnet test` across `Interlocutor.CommonKernel`, `Interlocutor.Abstractions`, `Interlocutor.Persistence.Abstractions`, `Interlocutor.Persistence.EntityFrameworkCore`/`.NHibernate`, and `Interlocutor.AspNetCore` - no lingering reference to the removed members.
- A new test proves `CommitGateDriver`'s three paths explicitly: normal return with nothing said → commit; explicit no-commit signal → rollback; unhandled exception → transaction left uncommitted and confirmed rolled back via session disposal (EF Core and NHibernate each).
- `PublicAPI.Unshipped.txt`/`PublicAPI.Shipped.txt` updated for every removed member across affected packages.
