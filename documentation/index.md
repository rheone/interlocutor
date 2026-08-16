# Interlocutor Documentation

This directory contains reference documentation for developers using Interlocutor. If you're building or maintaining *this repository* rather than consuming it as a library, see [docs/README.md](../docs/README.md) instead.

## Table of contents

A suggested reading order — each section builds on the ones above it. Jump straight to any file if you already know what you're looking for.

### Start here

- [Glossary](glossary.md) — domain vocabulary used throughout the project. Skim this first; every other doc assumes these terms.

### Core concepts

The fundamentals of writing a request and getting a response back.

- [Messages, Result Types & Variance](messages-and-result-types.md) — `IQuery<T>`, `ICommand<T>`, `ICommand`; the `IOperationOutcome` constraint; outcome unions and arities; pattern matching; choosing specific arities; marker interfaces; [!CAUTION] why response-type variance is impossible.
- [Handler Selection & Dispatch](handler-selection-and-dispatch.md) — compile-time discovery, closed beats open, ambiguity as a build error, resource-based authorization, multiple handlers for notifications, runtime-typed dispatch.
- [Pipeline & Behavior Composition](pipeline-and-behaviors.md) — the no-default-pipeline model, the flat ordered behavior list, the recommended chain with its rationale, validation/authorization/auditing behaviors, pipeline stages & short-circuiting.

### Wiring it up

Registering the library and understanding how a dispatch's scope and context flow.

- [Builder & Registration](builder-and-registration.md) — `InterlocutorBuilder` as the single entry point, assembly scanning, pipeline composition, dependency defaults, cross-assembly aggregation, startup validation.
- [Mediator, Lifetimes, Scope & Ambient Context](mediator-lifetimes-and-scope.md) — `ISender`/`IPublisher`/`IMediator` interfaces, why scoped lifetime is non-configurable, the root-scope exception, dispatching correctly from web/background contexts, `IRequestContextAccessor` and `RequestContext`.

### Cross-cutting features

- [Notifications & Fan-out](notifications-and-fan-out.md) — `INotification` and multiple handlers (contravariant fan-out), execution order, Sequential vs. Parallel publish strategies, failure semantics, the notification+command pattern.
- [Runtime-Typed Dispatch](runtime-typed-dispatch.md) — dispatching when the request type is only known at runtime, outbox/inbox/queue patterns, the `IOperationOutcome` base interface, error handling.

### Framework & store integration

- [ASP.NET Core Integration & HTTP Mapping](aspnetcore-integration.md) — the `ToActionResult()` extension, status code mapping, `ValidationProblemDetails` and `ProblemDetails` (RFC 7807), custom mapping, minimal API usage, error handling.
- [Transactions & Store Integration](transactions-and-store-integration.md) — `Interlocutor.EntityFrameworkCore` and `Interlocutor.NHibernate`, `TransactionBehavior`, the `ITransactionalRequest`/`IExternalStoreRequest` markers, lazy session resolution, commit/rollback semantics, nested dispatch, notifications after commit.

### Testing

- [Testing Handlers & Pipelines](testing.md) — unit testing handlers by mocking dependencies, integration testing pipeline composition, testing behaviors & stages, DI startup validation, in-memory databases, best practices.

### Coming soon

- [Trim & AOT support](trim-and-aot-support.md) — [!NOTE] **TBD** — blocked on ticket 17 (Trim and AOT hardening).
- [Sample app walkthrough](sample-app-walkthrough.md) — [!NOTE] **TBD** — blocked on ticket 18 (Sample application).

## House style conventions

Every documentation file in this directory follows these conventions:

### GitHub Alerts

Use all five GitHub alert types purposefully:

- `[!NOTE]` — supplementary detail, in-the-weeds content, or TBD placeholders (always in this form: `> [!NOTE]\n> **TBD:** <what's missing>`, greppable)
- `[!TIP]` — optional but recommended practice
- `[!IMPORTANT]` — load-bearing design decision that's easy to miss
- `[!WARNING]` — common footgun or easy mistake
- `[!CAUTION]` — severe or hard-to-reverse consequence (e.g., a constraint that can never change without a breaking version bump)

### Content rules

- **No enumerated counts in prose**: say "a set of" / "several" and link to source instead. Exception: counts are fine when enumerating an explicit, closed set of type implementations (e.g., "the five `OperationOutcome` failure cases: `NotFound`, `Forbidden`, `Conflict`, `ValidationError`, `Failure`").
- **Auto-generated counts are OK**: badges, CI test counts, etc., since they can't go stale.
- **Diagrams**: inline `​```mermaid` fences only. No standalone `.mmd` files — we don't have reuse-across-docs at scale yet.
- **HTTP status codes**: only `aspnetcore-integration.md` is permitted to reference HTTP status codes. All other docs link there instead.
- **Truthfulness**: every code example compiles against the shipped API. Claims are verified against `src/` code, not carried forward from older docs if they've been superseded.
- **No internal design-record citations**: never cite or link to `docs/adr/`, `docs/PRD.md`, `docs/TODO.md`, or any other `docs/` path to justify a design choice — e.g. don't write "See ADR-0009 for the no-default-pipeline design" or "`docs/PRD.md` §9.2 has the full design." State the rationale itself, as fact, in this doc — the *why* stays, the citation to an internal decision record doesn't. `docs/` is internal maintainer documentation (see [`docs/README.md`](../docs/README.md)); a consumer reading this tree has no reason to open it. If a claim needs a pointer, link sideways to the sibling `documentation/` file that already covers it, never to `docs/`.

## TBD sections

Some topics remain TBD until related infrastructure work completes:

- **Trim & AOT support** — blocked on ticket 17 (Trim and AOT hardening)
- **Sample app walkthrough** — blocked on ticket 18 (Sample application)

These sections are written as TBD placeholders per the alert convention above, so nothing is forgotten.
