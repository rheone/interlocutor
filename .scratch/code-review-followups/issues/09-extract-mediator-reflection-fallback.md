# 09 - Extract Mediator's reflection-based runtime-typed-dispatch fallback

**What to build:** `Mediator.cs` has grown to own four largely independent concerns: typed dispatch, activity tracing (`TraceSendAsync`/`TraceDispatchAsync`/`TracePublishAsync`), generated-dispatcher routing, and the full reflection-based runtime-typed-dispatch fallback (`DispatchViaReflectionAsync`, `ResolveSendMethod`, `InvokeSendAsync`, `FindClosedInterface`, `ResolveVoidCommandResultType`, ~200 lines). The reflection-fallback cluster reads as a self-contained unit - the class's own remarks already frame it as "the explicitly opted-in runtime-scanning fallback" and predict "a future generator-backed implementation replaces it" - and could live in its own internal type (e.g. `ReflectionRequestDispatcher`) without changing `Mediator`'s public surface.

**Blocked by:** None - can start immediately

**Status:** done

- [x] The reflection-based dispatch fallback is extracted into its own internal type, with `Mediator` delegating to it only when no generated dispatcher is available
- [x] `Mediator`'s public API and observable behavior (including activity tracing around both the generated and reflection paths) are unchanged
- [x] Existing tests covering runtime-typed dispatch (ticket 09) and activity tracing (ticket 15) still pass unchanged

## Comments

Verified already implemented: `ReflectionRequestDispatcher` (`Interlocutor.Abstractions/ReflectionRequestDispatcher.cs`) holds the extracted reflection-based fallback; `Mediator.DispatchAsync` delegates to `ReflectionRequestDispatcher.DispatchAsync` only when no generated dispatcher is registered. `dotnet test tests/Interlocutor.Abstractions.Tests` passes (153/153), covering both runtime-typed dispatch and activity tracing.
