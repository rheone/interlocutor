## Labels

ready-for-agent

## What to build

Let infrastructure code dispatch a request whose type is only known at runtime.

Anyone building an outbox, an inbox, or a queue consumer deserializes a message into an untyped object and needs to send it. Today that is impossible through the published interfaces: every dispatch overload infers its response type from the *static* type of the argument, and a deserialized message has no static type. Consumers are forced to write their own reflection layer around the library.

Add a dispatch overload taking an object and returning the library's base outcome interface. Returning the base outcome - rather than an untyped object - is what the union design uniquely allows: generic infrastructure can check success and pattern-match the failure cases to decide whether to retry, dead-letter, or log, without knowing the concrete result shape.

Implement it with reflection for now. Ticket 13 replaces that with a generated type switch once the generator exists; keeping the two tickets separate means outbox consumers are unblocked before the generator lands.

A request type with no registered handler must fail with a clear, named exception rather than a null reference or a cast error.

## Acceptance criteria

- [ ] A dispatch overload accepts an object and returns the base outcome interface
- [ ] A query, a value-returning command, and a unit command all dispatch correctly through it
- [ ] The returned outcome can be inspected for success and pattern-matched for failure cases without knowing the concrete shape
- [ ] An unregistered request type produces a named exception identifying the type
- [ ] An object that is not a request at all produces a distinct, clear error
- [ ] The overload is on the published send interface, not only the concrete mediator

## Blocked by

08 - Constrain response types to the outcome interface
