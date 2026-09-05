# 06 - Guard against a FluentValidation rule reading the DB outside the transaction boundary

**Status:** needs-triage

**What to build:** `AddValidationBehavior` runs before `AddUnitOfWorkBehavior` deliberately - "Validation before transaction so malformed input never opens a transaction that would only be rolled back" ([InterlocutorPipelineBuilder.cs:491](../../../src/Interlocutor.DependencyInjection.Abstractions/InterlocutorPipelineBuilder.cs#L491)). FluentValidation natively supports a rule resolving an injected dependency and querying it (`MustAsync`/`CustomAsync` against an injected `DbContext`, say) - nothing today stops a consumer from writing exactly that kind of rule, and if they do, it runs entirely outside any transaction, silently reintroducing the same check-then-act race `ITransactionalRequest` exists to close. `CreateTodoTaskCommand`/`CreateTodoTaskHandler` already avoid this by convention - the duplicate-title uniqueness check lives in the handler, not in `CreateTodoTaskValidator` - but nothing enforces that convention or warns a consumer away from the natural-looking alternative.

Needs triage before scoping: at minimum, a documentation warning next to `AddValidationBehavior`'s existing ordering rationale and in the `CreateTodoTaskCommand`/`CreateTodoTaskHandler` worked example explaining *why* the check lives where it does. Whether this also warrants a generator/analyzer diagnostic (flagging a validator that resolves a DB-touching dependency) is an open question - the difficulty of reliably detecting "this dependency touches the database" at compile time, versus the value of catching this before it reaches production, needs a real design pass rather than being assumed here.

**Blocked by:** None - can start immediately, independent of the rest of this PRD

- [ ] Scope decided: documentation-only, or documentation plus an analyzer
- [ ] `documentation/pipeline-and-behaviors.md`'s (or wherever `AddValidationBehavior`'s ordering is documented) rationale gains an explicit warning against DB-touching validation rules, naming the race it would reintroduce
- [ ] `documentation/sample-app-walkthrough.md`'s discussion of `CreateTodoTaskCommand` explains why the uniqueness check is in the handler, not the validator, if it doesn't already
