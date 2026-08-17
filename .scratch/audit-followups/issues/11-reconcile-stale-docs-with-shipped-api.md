## Labels

ready-for-agent

## What to build

Strip documentation and PRD claims that describe features which are not actually shipped in `src/`, per the "strip docs to reality" decision - none of these are being built as part of this batch.

- `documentation/notifications-and-fan-out.md` shows worked examples of `[RunAfter(typeof(...))]`/`[RunBefore(typeof(...))]` notification-ordering attributes (lines 68-92) and `builder.SetPublishStrategy(PublishStrategy.Parallel)` (lines 148-151). Neither exists anywhere in `src/`. These are tracked as real future work by open ticket `14-notification-fan-out.md` in `.scratch/di-builder-and-discovery/issues/` - don't duplicate that ticket here, just remove the examples that currently claim the feature already works, and note in the doc that ordering/strategy selection is not yet available.
- The `Interlocutor.FluentValidation` package description and `documentation/glossary.md` both name a `BulkItemValidationBehavior` type that doesn't exist - only `ValidationBehavior` does. Remove the references.
- `docs/PRD.md` §5/§7 list `IEnumerableQuery<T>`, `IEnumerableCommand<T>`, `IBulkCommand<T,T>`, `IPagedQuery<T>`, and `PagedResult<T>` as shipped `CommonKernel` types. None exist in `src/`; `documentation/glossary.md` already correctly describes `PagedResult<T>` as consumer-defined. Remove or correct these PRD entries so they don't contradict the glossary.
- CLAUDE.md claims "`Interlocutor.FluentValidation` ... currently mid-migration - see commented-out assertion in `DependencyTests.cs`," but that assertion (`ExampleImplementations_ShouldDependOnAbstractions`) is live and uncommented today. Correct or remove this claim.

## Acceptance criteria

- [ ] `documentation/notifications-and-fan-out.md` no longer shows `[RunAfter]`/`[RunBefore]` or `SetPublishStrategy` as working examples; it states plainly that ordering is unspecified and publish strategy is not yet configurable, without citing ticket numbers (per the house style rule against citing `docs/` paths)
- [ ] `BulkItemValidationBehavior` is removed from the FluentValidation package description and `documentation/glossary.md`
- [ ] `docs/PRD.md`'s type catalog no longer lists `IEnumerableQuery<T>`, `IEnumerableCommand<T>`, `IBulkCommand<T,T>`, `IPagedQuery<T>`, or `PagedResult<T>` as shipped library types, and doesn't contradict `documentation/glossary.md`
- [ ] CLAUDE.md's FluentValidation layering claim matches the current state of `DependencyTests.cs` (live assertion, not commented out)
- [ ] No other README.md or documentation/ claim was found to regress in the same sweep (spot-check truthfulness against `src/` for the files touched)

## Blocked by

None - can start immediately
