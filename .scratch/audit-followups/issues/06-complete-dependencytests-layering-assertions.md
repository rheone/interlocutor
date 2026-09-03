> **Status: done** - merged in `a207156`.
>
> **Reopened note (2026-09-02):** this ticket's criteria weren't durable across later package additions -
> `.scratch/repo-review-2026-09-02/findings/08-dependencytests-layering-gaps.md` found
> `Interlocutor.Persistence.EntityFrameworkCore`/`.NHibernate` were never checked against `Interlocutor.FluentValidation`
> (criterion 4's store-specific-package renames aside, the FluentValidation half of that check was never added for
> the packages that actually shipped), and `Interlocutor.CommonKernel` was never checked against
> `Interlocutor.DependencyInjection.Abstractions`/`.Microsoft`/`.Autofac` or `Interlocutor.Persistence.Abstractions`,
> despite CLAUDE.md's unqualified "must never depend on any other Interlocutor package" rule for CommonKernel. Closed
> again via `.scratch/repo-review-2026-09-02/issues/09-dependencytests-layering-gaps.md`, which added both gaps to
> `DependencyTests.cs`. Left as "done" rather than reverted to open, since the original criteria are still met -
> this note exists so a future ticket-completeness check doesn't cite this file as evidence the case is fully closed
> without also checking the newer ticket.

## Labels

ready-for-agent

## What to build

Close the gap between the package-layering rules documented in CLAUDE.md and what `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` actually asserts via NetArchTest. Today the file has only 5 facts and never uses `Interlocutor.DependencyInjection.Microsoft` or `Interlocutor.DependencyInjection.Autofac` as the subject assembly of a check - they only appear inside other assemblies' "ShouldNot" lists - and there is no assertion that EF Core and NHibernate don't depend on each other or on FluentValidation.

## Acceptance criteria

- [x] A test asserts `Interlocutor.DependencyInjection.Microsoft` does not depend on `Interlocutor.DependencyInjection.Autofac`, and vice versa
- [x] A test asserts neither DI package depends on the store-specific packages (`Interlocutor.EntityFrameworkCore`, `Interlocutor.NHibernate`) or `Interlocutor.FluentValidation`
- [x] A test asserts `Interlocutor.EntityFrameworkCore` does not depend on `Interlocutor.NHibernate`, and vice versa
- [x] A test asserts neither store-specific package depends on `Interlocutor.FluentValidation`
- [x] Every cross-package rule stated in CLAUDE.md's "Architecture" section has a corresponding assertion in this file
- [x] Existing dependency tests still pass

## Blocked by

None - can start immediately
