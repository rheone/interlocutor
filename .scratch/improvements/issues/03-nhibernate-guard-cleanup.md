> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: the composite-condition pattern it describes no longer exists.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Clean up the `TransactionBehavior` in `Interlocutor.NHibernate` by extracting the `Lazy<ISession>` resolution into an explicit guard clause, removing the nested `&&` side-effect pattern that evaluates in a single composite condition.

Current pattern (nested in a single `if`):
```
if (request is ITransactionalRequest && ... s.Value ...
```

The EF Core counterpart resolves the context in a separate guard clause before the transaction block. The NHibernate version should mirror that pattern for clarity and safety.

## Acceptance criteria

- [ ] `Lazy<ISession>.Value` is resolved in a guard clause, not a composite `&&` condition
- [ ] Non-`ITransactionalRequest` requests never trigger `Lazy<ISession>.Value`
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
