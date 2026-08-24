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
