# 01 — Interlocutor.Testing package: FakeSender and FakePublisher

**What to build:** A new, shipped `Interlocutor.Testing` package that gives consumers fake implementations of `ISender` and `IPublisher` for use in their own unit tests, without wiring a real pipeline. A consumer references the package, constructs a `FakeSender`, stubs a response for a given request type, calls `SendAsync`, and can assert on what was sent and what was returned. Symmetrically, a `FakePublisher` records every notification passed to `PublishAsync` (and, where a consumer supplies fake handlers, what those handlers observed), so notification-driven code can be asserted against without standing up real handlers. The package follows the existing layering rules: it depends only on `Interlocutor.CommonKernel` and `Interlocutor.Abstractions` — never on either DI package, the generator, or a persistence package. `documentation/testing.md` gains a section demonstrating both fakes, landing in the same iteration as the code per this repo's documentation-maintenance convention.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] `Interlocutor.Testing` package exists under `src/`, packaged the same way as the other shipped packages, with its own `tests/Interlocutor.Testing.Tests` project covering both fakes
- [ ] `FakeSender` implements `ISender`, lets a test stub a response per request type, and exposes the requests it received for assertion
- [ ] `FakePublisher` implements `IPublisher`, exposes the notifications it received for assertion, and supports assigning fake handlers so handler invocation can be asserted too
- [ ] `Interlocutor.Architecture.Tests` gains the dependency rule for the new package: depends only on CommonKernel + Abstractions
- [ ] `documentation/testing.md` documents both fakes with a compiling example
