# 04 — Interlocutor.IntegrationTests: Autofac container parity

**What to build:** Run the ticket-02 tracer-bullet scenario a second time through Autofac's `AddInterlocutor()` registration instead of Microsoft DI, proving the cross-package pipeline wiring is container-agnostic rather than incidentally dependent on Microsoft.Extensions.DependencyInjection behavior.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] The same command/handler/pipeline scenario from ticket 02 passes when resolved from an Autofac container
- [ ] Any container-specific setup differences are isolated to test fixture code, not to the handlers or behaviors under test
- [ ] Both the Microsoft DI and Autofac variants of the scenario run in CI
