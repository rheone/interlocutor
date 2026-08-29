> **Status: open**

## Labels

ready-for-agent

## What to build

`MediatorActivitySource` (`Interlocutor.Abstractions.Diagnostics`) is a complete, undocumented observability feature: `Mediator` starts one `Activity` span per `Send`/`Publish` call via a stable, documented `ActivitySource` name (`Interlocutor.Mediator`), tagged with `interlocutor.request_type`, `interlocutor.handler_type`, and `interlocutor.outcome` - independent of whether `LoggingBehavior` is also registered, and zero-cost when unobserved (the standard `Activity` null-when-unlistened mechanism). None of this appears anywhere in `documentation/`.

Add a new section - either its own short doc or a subsection of `mediator-lifetimes-and-scope.md` (it documents the mediator itself) - covering: the `ActivitySource` name and tag keys, that it's independent of `LoggingBehavior`, and how to attach a listener (a plain `ActivityListener`, or OpenTelemetry's `TracerProviderBuilder.AddSource(MediatorActivitySource.Name)`). Link it from `documentation/index.md`.

## Acceptance criteria

- [ ] `MediatorActivitySource`'s name, tag keys, and relationship to `LoggingBehavior` are documented
- [ ] A worked example shows attaching a listener/exporter
- [ ] Linked from `documentation/index.md`
