# 03 - Remove copy-paste debris in Interlocutor.Abstractions

**What to build:** Two small, independent cleanups in `Interlocutor.Abstractions`:

- `OutcomeClassifierBuilder`'s constructor registers `Conflict` via `RegisterFailureType<Conflict>("Conflict")` twice - remove the duplicate registration.
- `Mediator.PublishAsync` switches on `strategy` twice within the same method (once to pick the cache, once to pick the wrapper type) - collapse into a single switch that yields both the cache and wrapper type together.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `OutcomeClassifierBuilder` registers `Conflict` exactly once
- [ ] `Mediator.PublishAsync` contains a single switch over `strategy`, not two
- [ ] Existing behavior is unchanged - `Interlocutor.Abstractions.Tests` pass without modification to test expectations
