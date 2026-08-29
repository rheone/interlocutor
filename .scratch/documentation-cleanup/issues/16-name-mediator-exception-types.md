> **Status: done**

## Labels

ready-for-agent

## What to build

Three real, catchable exception types are described only by behavior, never by name, anywhere in `documentation/`:

- `MediatorResolvedFromRootScopeException` - thrown by the root-scope guard (`mediator-lifetimes-and-scope.md` § Root-Scope Exception & Guard says "throws an exception" without naming it).
- `HandlerNotRegisteredException` - thrown when a handler is missing at resolution time (referenced generically in `builder-and-registration.md` § Verifying Your Registration: "GetRequiredService throws when you actually dispatch it").
- `UnsupportedRequestTypeException` - thrown by runtime-typed `Send(object, ct)` when the runtime type isn't a recognized request shape (`runtime-typed-dispatch.md`'s error-handling examples only catch generic `Exception`/`JsonException`).

Name each exception type explicitly at its point of description, so a reader can write a targeted `catch` clause without reading source. No behavior change - purely naming what's already documented by effect.

## Acceptance criteria

- [x] All three exception types are named by their actual type in the doc file that already describes when they're thrown
- [x] `runtime-typed-dispatch.md`'s error-handling section catches `UnsupportedRequestTypeException`/`HandlerNotRegisteredException` specifically where appropriate, alongside the existing generic handling

## Findings beyond the original scope

- `mediator-lifetimes-and-scope.md`'s root-scope example was subtly wrong about *when* the guard fires: it implied `GetRequiredService<IMediator>()` itself throws, but the actual `RootScopeGuardedMediator` defers the check to the `Send`/`Publish` call - resolution always succeeds, even from the root provider. Fixed the example and comment placement, not just the exception name.
- `HandlerNotRegisteredException` only applies to runtime-typed dispatch (`Send(object, ct)`) - typed dispatch (`Send<TResponse>(IQuery<TResponse>, ...)`) surfaces a missing handler as the container's own generic `InvalidOperationException` from `GetRequiredService`, per the exception's own XML remarks drawing that exact distinction. Kept `builder-and-registration.md`'s example correctly generic rather than misnaming it, and cross-linked to where the named exception actually applies.
