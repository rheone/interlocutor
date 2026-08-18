## Labels

ready-for-agent

## What to build

Extend `HandlerDiscoveryTests.cs` (`tests/Interlocutor.DependencyInjection.Abstractions.Tests/`) to cover realistic handler shapes beyond the current one-handler-per-contract-type coverage. `HandlerDiscovery.Discover`'s own doc comment explicitly calls out that a single type may implement more than one handler contract simultaneously - that claim is currently untested. `ICommandHandler<,>` (the response-bearing command contract) isn't covered at all, and multiple `INotificationHandler<T>` implementations for the same notification, and open-generic handler classes, are also unexercised.

## Acceptance criteria

- [x] A type implementing `ICommandHandler<,>` is discovered and registered correctly
- [x] A single type implementing more than one handler contract (e.g. a query handler and a notification handler) is discovered for both
- [x] Multiple `INotificationHandler<T>` implementations for the same notification type are all discovered
- [x] An open-generic handler class is discovered in whatever form current discovery supports (or the test documents that it is correctly *not* discovered, matching ticket 11's open-generic-handlers scope in `di-builder-and-discovery`)
- [x] Existing `HandlerDiscoveryTests.cs` assertions still pass

## Blocked by

None - can start immediately
