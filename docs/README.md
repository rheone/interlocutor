# Interlocutor — Developer Documentation

This directory contains internal documentation for people building and maintaining *this repository*. If you're using Interlocutor as a library in your own application, you want [`documentation/`](../documentation/index.md) instead.

## Start here

- [PRD.md](PRD.md) — the current specification: package structure, type catalog, CQRS interfaces, pipeline behaviors, DI registration, HTTP mapping, notifications, ambient context, testing strategy.
- [TODO.md](TODO.md) — open questions and rough backlog items not yet promoted to a ticket or ADR.

## Architecture Decision Records ([adr/](adr/))

Immutable once accepted — a changed decision gets a new ADR that supersedes the old one; existing ADRs are never edited to reverse a decision.

- [ADR-0001](adr/0001-use-csharp-15-unions-for-result-types.md) — Use C# 15 Union Types for Result Modeling
- [ADR-0002](adr/0002-pipeline-behavior-onion-model.md) — Pipeline Behavior Onion/Russian-Doll Model
- [ADR-0003](adr/0003-mediator-as-service-locator.md) — Mediator as Service Locator — No Internal Handler Registry
- [ADR-0004](adr/0004-marker-interfaces-for-cross-cutting.md) — Marker Interfaces for Cross-Cutting Opt-In
- [ADR-0005](adr/0005-void-command-pipeline-support.md) — Void Command Pipeline Support
- [ADR-0006](adr/0006-interface-segregation-mediator.md) — Interface Segregation for Mediator
- [ADR-0007](adr/0007-notification-publish-strategies.md) — Notification Publish Strategies
- [ADR-0008](adr/0008-compile-time-handler-discovery.md) — Compile-Time Handler Discovery via Source Generator
- [ADR-0009](adr/0009-no-default-pipeline.md) — No Default Pipeline — Flat Ordered Filtered Behavior List
- [ADR-0010](adr/0010-dispatch-scope-fan-out-open-generics-ambiguity.md) — Dispatch Scope — Notification Fan-Out, Open Generics, and Build-Error Ambiguity
- [ADR-0011](adr/0011-mediator-scoped-lifetime-and-root-scope-guard.md) — Mediator Scoped Lifetime, Non-Configurable, with a Root-Scope Guard
- [ADR-0012](adr/0012-http-agnostic-core-with-pluggable-mapping.md) — HTTP-Agnostic Core with Pluggable Status Mapping
- [ADR-0013](adr/0013-response-constrained-to-outcome-no-streaming.md) — Response Type Constrained to the Outcome Interface; Streaming Excluded

## Contributor guides ([guides/](guides/))

- [Composing OperationOutcome Arities](guides/composing-operationoutcome-arities.md)

## Agent process docs ([agents/](agents/))

- [domain.md](agents/domain.md) — how engineering skills consume this repo's domain documentation
- [issue-tracker.md](agents/issue-tracker.md) — the local `.scratch/` markdown issue tracker
- [triage-labels.md](agents/triage-labels.md) — triage role → label mapping

## Historical: bootstrapreference/

> [!NOTE]
> Superseded. `bootstrapreference/` holds the original bootstrap PRD, ADR, and code sketches that predate this repo's implementation. It has been reconciled into [PRD.md §15](PRD.md#15-comparison-with-bootstrap-prd), which is the current source of truth. Consult `bootstrapreference/` only for historical context on why an early design differs from what shipped — never as an authoritative source for current behavior.
