# Interlocutor — Developer Documentation

This directory contains internal documentation for people building and maintaining *this repository*. If you're using Interlocutor as a library in your own application, you want [`documentation/`](../documentation/index.md) instead.

## Table of contents

- [Start here](#start-here) — the current spec and open questions
- [Architecture Decision Records](#architecture-decision-records-adr) — why the library is shaped the way it is, grouped by theme
- [Contributor guides](#contributor-guides-guides) — worked examples for contributors
- [Agent process docs](#agent-process-docs-agents) — how agents work this repo's issue tracker and triage
- [Historical: bootstrapreference/](#historical-bootstrapreference) — superseded, context only

## Start here

- [PRD.md](PRD.md) — the current specification: package structure, type catalog, CQRS interfaces, pipeline behaviors, DI registration, HTTP mapping, notifications, ambient context, testing strategy. Start here for "what does the shipped API actually look like."
- [TODO.md](TODO.md) — open questions and rough backlog items not yet promoted to a ticket or ADR.

## Architecture Decision Records ([adr/](adr/))

Immutable once accepted — a changed decision gets a new ADR that supersedes the old one; existing ADRs are never edited to reverse a decision. Grouped by theme below rather than listed in number order, so you can find "why does X work this way" without already knowing which ADR number covers it.

**Result modeling & types** — how outcomes are represented and constrained
- [ADR-0001](adr/0001-use-csharp-15-unions-for-result-types.md) — Use C# 15 Union Types for Result Modeling: closed unions (`Result<T>`, `Failure`) give compile-time exhaustiveness without a third-party dependency like `OneOf`.
- [ADR-0004](adr/0004-marker-interfaces-for-cross-cutting.md) — Marker Interfaces for Cross-Cutting Opt-In: a request opts into transactions/auditing/authorization by implementing a marker interface, not by editing a central config list.
- [ADR-0005](adr/0005-void-command-pipeline-support.md) — Void Command Pipeline Support: commands with no return payload still run through the same behavior pipeline as typed commands, via a dedicated wrapper.
- [ADR-0013](adr/0013-response-constrained-to-outcome-no-streaming.md) — Response Type Constrained to the Outcome Interface; Streaming Excluded: every response implements `IOperationOutcome`; streaming is deliberately excluded on purpose, not merely unaddressed.

**Pipeline & behaviors** — how cross-cutting concerns compose around a handler
- [ADR-0002](adr/0002-pipeline-behavior-onion-model.md) — Pipeline Behavior Onion/Russian-Doll Model: one `IPipelineBehavior<TRequest,TResponse>` wraps pre/post logic around a single `next()` call.
- [ADR-0009](adr/0009-no-default-pipeline.md) — No Default Pipeline — Flat Ordered Filtered Behavior List: the pipeline is empty until the consumer composes it; nothing runs unless explicitly added.

**Handler discovery & dispatch** — how a request finds its handler
- [ADR-0003](adr/0003-mediator-as-service-locator.md) — Mediator as Service Locator — No Internal Handler Registry: *(superseded by [ADR-0008](adr/0008-compile-time-handler-discovery.md))* the original runtime-resolution design.
- [ADR-0008](adr/0008-compile-time-handler-discovery.md) — Compile-Time Handler Discovery via Source Generator: a Roslyn generator emits registration and dispatch code, replacing runtime reflection — enables build-time diagnostics and AOT/trim compatibility.
- [ADR-0010](adr/0010-dispatch-scope-fan-out-open-generics-ambiguity.md) — Dispatch Scope — Notification Fan-Out, Open Generics, and Build-Error Ambiguity: closed handlers beat open generic ones, ambiguous matches fail the build, and notification fan-out is contravariant (deliberately different from query/command dispatch).

**Mediator interface & lifecycle** — the shape and lifetime of the mediator itself
- [ADR-0006](adr/0006-interface-segregation-mediator.md) — Interface Segregation for Mediator: `ISender`/`IPublisher`/`IMediator` are split so a consumer depends only on the capability it actually uses.
- [ADR-0011](adr/0011-mediator-scoped-lifetime-and-root-scope-guard.md) — Mediator Scoped Lifetime, Non-Configurable, with a Root-Scope Guard: the mediator is always scoped, never singleton or transient, and a guard catches accidental root-scope use.

**Notifications** — fire-and-forget domain events
- [ADR-0007](adr/0007-notification-publish-strategies.md) — Notification Publish Strategies: `Sequential` (default) vs. `Parallel` fan-out, and why handler ordering is unspecified by default with a relative-ordering escape hatch.

**HTTP integration** — where transport concerns are allowed to live
- [ADR-0012](adr/0012-http-agnostic-core-with-pluggable-mapping.md) — HTTP-Agnostic Core with Pluggable Status Mapping: core result types carry zero HTTP knowledge; `Interlocutor.AspNetCore` owns a pluggable status-code mapper instead.

## Contributor guides ([guides/](guides/))

- [Composing OperationOutcome Arities](guides/composing-operationoutcome-arities.md) — worked examples showing how to pick the right `OperationOutcome` arity (2 through 6 cases) for a given command or query, with the reasoning for each choice.

## Agent process docs ([agents/](agents/))

- [domain.md](agents/domain.md) — how engineering skills consume this repo's domain documentation.
- [issue-tracker.md](agents/issue-tracker.md) — the local `.scratch/` markdown issue tracker: file layout, ticket format, numbering convention.
- [triage-labels.md](agents/triage-labels.md) — triage role → label mapping used across `.scratch/` tickets.

## Historical: bootstrapreference/

> [!NOTE]
> Superseded. `bootstrapreference/` holds the original bootstrap PRD, ADR, and code sketches that predate this repo's implementation. It has been reconciled into [PRD.md §15](PRD.md#15-comparison-with-bootstrap-prd), which is the current source of truth. Consult `bootstrapreference/` only for historical context on why an early design differs from what shipped — never as an authoritative source for current behavior.
