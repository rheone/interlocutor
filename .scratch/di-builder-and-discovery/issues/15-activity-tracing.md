> **Status: done** - merged in `148a0fc`.

## Labels

ready-for-agent

## What to build

Make dispatch visible in an operator's existing tracing tools with no adapter and no configuration.

Today the only observability the library offers is a logging behavior a developer must opt into, which writes text. An operator running distributed tracing gets nothing: a request dispatched through the mediator is an invisible gap between the incoming HTTP span and the outgoing database span.

Emit spans through the standard .NET activity mechanism from the mediator itself, not from an opt-in behavior - tracing that only works if you remembered to add a behavior is tracing you cannot rely on. One activity source, with spans covering send and publish, tagged with the request type, the resolved handler type, and the resulting outcome case. Failures recorded as the outcome they are, not as exceptions.

This must cost effectively nothing when nobody is listening, which the standard mechanism supports directly - no allocation and no string formatting on the un-listened path. Verify that rather than assuming it.

Because it is in the mediator rather than a behavior, it must not duplicate or conflict with the logging behavior when both are in use.

## Acceptance criteria

- [ ] One activity source emits spans for send and publish
- [ ] Spans carry request type, handler type, and outcome case
- [ ] A failure outcome is recorded as an outcome, not as an exception; a thrown exception is recorded as an error
- [ ] Nested dispatch produces correctly parented spans
- [ ] With no listener attached, no allocation or string formatting occurs on the dispatch path
- [ ] Tracing works without the logging behavior registered, and does not conflict when it is
- [ ] The activity source name is documented and stable

## Blocked by

03 - Builder API, alongside the legacy overloads
