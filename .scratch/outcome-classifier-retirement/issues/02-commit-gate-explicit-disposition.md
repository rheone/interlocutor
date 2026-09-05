# 02 - `CommitGateDriver`: commit by default, explicit no-commit signal instead of classification

**Status:** ready-for-agent

**What to build:** Replace `CommitGateDriver.Handle`'s `if (!classifier.IsFailure(result))` branch ([CommitGateDriver.cs:94](../../../src/Interlocutor.Persistence.Abstractions/CommitGateDriver.cs#L94)) with a model that never inspects the response's type at all:

- **Default: commit.** Once there's an active transaction with pending work (the existing early-exit logic at lines 84-87 is unchanged - `NotApplicable`, in effect, stays exactly as it is today), the driver commits unless told otherwise.
- **Explicit no-commit, call-site override:** add a way to signal "don't commit" alongside a specific returned or ejected value - most naturally an `Eject` overload (e.g. `Eject.With(value, commit: false)`) for the short-circuit path, and an equivalent for an ordinary `return` (this half needs a concrete API shape decided during implementation - the PRD does not prescribe one).
- **Explicit no-commit, ambient:** `IUnitOfWork.Cancel()` already ships this ([IUnitOfWork.cs:43-63](../../../src/Interlocutor.Persistence.Abstractions/IUnitOfWork.cs#L43-L63)) - confirm/exercise it as the ambient half of this mechanism rather than introducing a second, redundant API for the same thing.
- **Implicit no-commit via exception:** unchanged in effect (an unhandled exception already propagates past `CommitGateDriver` untouched, at line 79, with no `try`/`catch`) - but add an explicit test proving the store session actually rolls back an uncommitted transaction on disposal for both EF Core and NHibernate, since this now carries the full weight of the "implicit failure" path rather than being a backstop alongside a classifier.

No case type - `Success`, `Conflict`, `ValidationErrors`, none of them - determines this decision. A handler may construct or `Eject` with `Success` mid-flow and separately signal no-commit; the two are fully decoupled.

**Blocked by:** 01 (removes `IOutcomeClassifier.IsFailure`, which this driver currently calls)

- [ ] `CommitGateDriver` no longer references `IOutcomeClassifier`/`IsFailure` anywhere; its `classifier` parameter is removed from `Handle`'s signature
- [ ] A request that writes and returns/ejects normally, with no explicit signal, commits
- [ ] A request that explicitly signals no-commit (via the new call-site mechanism or `IUnitOfWork.Cancel()`) rolls back, regardless of what value it returns alongside that signal
- [ ] A request whose handler throws leaves the transaction uncommitted, and a new test confirms the underlying EF Core/NHibernate session actually rolls it back on disposal
- [ ] Both `Interlocutor.Persistence.EntityFrameworkCore.UnitOfWorkBehavior` and `Interlocutor.Persistence.NHibernate.UnitOfWorkBehavior` (and `TutorialProject`'s `TodoTasksUnitOfWorkBehavior`) updated for the new `CommitGateDriver.Handle` signature
- [ ] `documentation/transactions-and-store-integration.md`'s existing code example (its `UnitOfWorkBehavior` subclass constructor at line 94 takes an `IOutcomeClassifier classifier` parameter) is not left describing a removed constructor shape - full documentation pass is issue 05, but this ticket must not merge with that doc in a broken state
