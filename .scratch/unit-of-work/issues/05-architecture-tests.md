Status: needs-triage

## Labels

ready-for-agent

## What to build

Add `NetArchTest` rules to `tests/Interlocutor.Architecture.Tests/DependencyTests.cs`, following the exact pattern of the existing rules (see [research.md](../research.md) §1.6 for the current rule inventory):

- `Interlocutor.Persistence.Abstractions` must not depend on `Interlocutor.AspNetCore`, `Interlocutor.EntityFrameworkCore`, `Interlocutor.Persistence.EntityFrameworkCore`, `Interlocutor.FluentValidation`, `Interlocutor.NHibernate`, `Interlocutor.Persistence.NHibernate`, `Interlocutor.DependencyInjection.Microsoft`, or `Interlocutor.DependencyInjection.Autofac`.
- `Interlocutor.Persistence.EntityFrameworkCore` must depend on `Interlocutor.Persistence.Abstractions` and must not depend on `Interlocutor.Persistence.NHibernate`.
- `Interlocutor.Persistence.NHibernate` must depend on `Interlocutor.Persistence.Abstractions` and must not depend on `Interlocutor.Persistence.EntityFrameworkCore`.

## Acceptance criteria

- [ ] New rules added, named consistently with existing test method names in the same file
- [ ] `dotnet test tests/Interlocutor.Architecture.Tests` passes against the packages built in issues 01-03
- [ ] Deliberately break one rule locally (e.g. add a stray reference) to confirm the new test actually fails before submitting - then revert

## Blocked by

01
