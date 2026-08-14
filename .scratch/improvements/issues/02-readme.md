> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: references removed result and case types.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Write the README (currently 0 bytes). This is the first thing a potential user sees. It should cover:

- **What Interlocutor is**: a mediator/CQRS pipeline library built on C# 15 union types for explicit, compiler-checked error handling
- **Quick-start**: add the DI package, register handlers and mediator, define a query/command with handler, call `IMediator.Send`, handle `Result<T>` with pattern matching
- **Package table**: list of NuGet packages and what each provides
- **Links** to PRD and ADRs in `bootstrapreference/` for deeper reading

Keep it concise — a new user should be productive within 60 seconds of reading the README.

## Acceptance criteria

- [ ] README has a meaningful title, one-paragraph description, and a runnable quick-start example
- [ ] At least one end-to-end example (define a query, register handler, send, match result)
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
