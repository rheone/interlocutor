# 21 - S1694: decide whether `HandlerWrapper`'s abstract base should become an interface

**What to build:** SonarAnalyzer's S1694 ("Convert this 'abstract' class to an interface") fires twice on `src/Interlocutor.Abstractions/Core/HandlerWrapper.cs(14,25)` - it has no fields and no non-abstract members, the classic interface-shaped abstract class. Investigate whether it was deliberately made an abstract class instead of an interface for a real reason (e.g. non-public accessibility scoped to the assembly, a future extension point that needs shared state, or a generic-variance concern an interface can't express as cleanly) - if so, suppress with that reason recorded; if not, converting it to an interface is a public API shape change for a mediator-core type, so treat it as a deliberate, reviewed decision rather than a mechanical rename, and check `tests/Interlocutor.Architecture.Tests` for any assumption tied to it being a class.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] A decision is made and recorded (converted to an interface, or suppressed with a genuine reason) for `HandlerWrapper`
- [ ] If converted, every consumer (production and test) still compiles and behaves identically - this is a public API surface, so check for breaking-change implications before shipping
- [ ] `dotnet build` reports zero `S1694` warnings
- [ ] `dotnet test` passes solution-wide
