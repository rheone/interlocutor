# Interlocutor Documentation

This directory contains reference documentation for developers using Interlocutor. If you're building or maintaining *this repository* rather than consuming it as a library, see [maintainer_docs/README.md](../maintainer_docs/README.md) instead.

## Table of contents

A suggested reading order - each section builds on the ones above it. Jump straight to any file if you already know what you're looking for.

### Start here

- [Glossary](glossary.md) - domain vocabulary used throughout the project. Skim this first; every other doc assumes these terms.

### Tutorial

- [Beginner's Guide: Building a To-Do API](beginners-guide-todo-api.md) - a start-to-finish walkthrough building a small API from an empty project: Autofac DI, NHibernate persistence, the full recommended pipeline chain, both authorization styles, and a multi-subscriber notification. Complements, rather than duplicates, [Sample App Walkthrough](sample-app-walkthrough.md) below, which tours a different (EF Core / Microsoft DI / minimal API) runnable sample.

### Core concepts

The fundamentals of writing a request and getting a response back.

- [Messages, Result Types & Variance](messages-and-result-types.md) - `IQuery<T>`, `ICommand<T>`, `ICommand`; the `IOperationOutcome` constraint; outcome unions and arities; pattern matching; choosing specific arities; marker interfaces; [!CAUTION] why response-type variance is impossible.
- [Composing OperationOutcome Arities](composing-operationoutcome-arities.md) - worked examples across three arities (a command, a query, and DI composition root for each) showing how to pick the right shape for a given operation.
- [Handler Selection & Dispatch](handler-selection-and-dispatch.md) - compile-time discovery, closed beats open, duplicate/ambiguous-handler diagnostics, resource-based authorization, multiple handlers for notifications, runtime-typed dispatch.
- [Pipeline & Behavior Composition](pipeline-and-behaviors.md) - the no-default-pipeline model, the flat ordered behavior list, the recommended chain with its rationale, validation/authorization/auditing behaviors, pipeline stages & short-circuiting.

### Wiring it up

Registering the library and understanding how a dispatch's scope and context flow.

- [Builder & Registration](builder-and-registration.md) - `InterlocutorBuilder` as the single entry point, assembly scanning, predicate-based registration, extending ambiguity checking to your own open-generic contracts, pipeline composition, dependency defaults, cross-assembly aggregation, startup validation.
- [Dependency Injection: Abstractions & Registration Model](dependency-injection-abstractions.md) - the container-neutral registration model (`InterlocutorRegistration`, `RegistrationLifetime`, `RegistrationReport`), validation mechanisms, and compile-time generated-registration attributes shared by both container packages.
- [Dependency Injection: Microsoft.Extensions.DependencyInjection](dependency-injection-microsoft.md) - registration, injection, and validation examples for `Interlocutor.DependencyInjection.Microsoft`, plus root-scope detection internals.
- [Dependency Injection: Autofac](dependency-injection-autofac.md) - the equivalent surface for `Interlocutor.DependencyInjection.Autofac`.
- [Mediator, Lifetimes, Scope & Ambient Context](mediator-lifetimes-and-scope.md) - `ISender`/`IPublisher`/`IMediator` interfaces, why scoped lifetime is non-configurable, the root-scope exception, dispatching correctly from web/background contexts, `IRequestContextAccessor` and `RequestContext`.

### Cross-cutting features

- [Notifications & Fan-out](notifications-and-fan-out.md) - `INotification` and multiple handlers (contravariant fan-out), execution order, Sequential vs. Parallel publish strategies, failure semantics, the notification+command pattern.
- [Runtime-Typed Dispatch](runtime-typed-dispatch.md) - dispatching when the request type is only known at runtime, outbox/inbox/queue patterns, the `IOperationOutcome` base interface, error handling.

### Framework & store integration

- [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md) - `ToActionResult()`/`ToMinimalApiResult()`, the status-code mapping table, `ProblemDetails` (RFC 7807), custom mapping via `IOutcomeStatusMapper`, error handling.
- [Transactions & Store Integration](transactions-and-store-integration.md) - `Interlocutor.Persistence.EntityFrameworkCore` and `Interlocutor.Persistence.NHibernate`'s `UnitOfWorkBehavior`, `IUnitOfWork` as transaction owner, begin-on-first-write, the `ITransactionalRequest`/`IExternalStoreRequest` markers, `Cancel`, commit/rollback semantics, nested dispatch, notifications after commit.
- [Persistence Abstraction](persistence-abstraction.md) - the optional, opt-in `IUnitOfWork`/`IDomainRepository<TEntity, TKey>` CRUD/paging surface, `Page<T>`, per-container registration, NHibernate session concurrency.
- [Persistence Implementations](persistence-implementations.md) - what `EntityFrameworkUnitOfWork<TContext>`/`EntityFrameworkRepository<TEntity, TKey, TContext>` and `NHibernateUnitOfWork`/`NHibernateRepository<TEntity, TKey>` actually do underneath the interfaces, the shared filter/sort/paging query-composition helpers, and the behavioral differences between the two stores.
- [Trim & AOT Support](trim-and-aot-support.md) - what "trim-compatible, not yet Native AOT-compatible" means in practice, the runtime-scanning trim cost, publishing trimmed, known limitations.

### Extending the library

- [Extensibility](extensibility.md) - a map of every point where a consumer plugs their own type in: marker interfaces, custom `IPipelineBehavior`/`IPipelineStage`, custom `IOperationOutcome` implementations, predicate-based registration, extending ambiguity checking to your own contracts, and per-request-type pipeline overrides.

### Testing

- [Testing Handlers & Pipelines](testing.md) - unit testing handlers by mocking dependencies, integration testing pipeline composition, testing behaviors & stages, DI startup validation, in-memory databases, best practices.

### Worked example

- [Sample App Walkthrough](sample-app-walkthrough.md) - a guided tour of the runnable sample under `samples/`: query/command/generic handlers, cross-assembly discovery, an explicitly composed pipeline with a short-circuiting stage, a multi-subscriber notification, and transport conversion at the edge.
