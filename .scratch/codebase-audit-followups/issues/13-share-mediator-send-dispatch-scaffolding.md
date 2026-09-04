# 13 - Share dispatch-and-trace scaffolding across Mediator.Send overloads

**What to build:** `Mediator`'s three `Send<T>` overloads (`IQuery<TResponse>`, `ICommand<TResponse>`, `ICommand`) each repeat the same shape: look up (or `Activator.CreateInstance` into) a wrapper from a static `ConcurrentDictionary` cache, start an `Activity`, take the fast path when unobserved, otherwise route through `TraceAsync`. Extract a small private generic helper (e.g. `DispatchWithTrace<TKey, TWrapper, TResponse>(cache, key, wrapperFactory, request, services, ct)`) that each overload calls with its own cache, key shape, and wrapper interface.

This is a speculative/low-priority cleanup - the duplication is mechanical, not structural (deleting one overload wouldn't cause its scaffolding to reappear elsewhere). Fine to do opportunistically; not urgent.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] A single shared dispatch-and-trace helper backs all three `Send` overloads
- [ ] Each overload supplies only its own cache, key, and wrapper type to the shared helper
- [ ] `Interlocutor.Abstractions.Tests` (`MediatorTests.cs`) pass without change to their assertions
