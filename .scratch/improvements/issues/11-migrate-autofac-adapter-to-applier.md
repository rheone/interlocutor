# 11 - Migrate the Autofac adapter onto RegistrationApplier

**What to build:** `Interlocutor.DependencyInjection.Autofac`'s `AddInterlocutor` drives registration through the shared `RegistrationApplier` (ticket 09) instead of its own `Apply`/`ApplyFactories`/`TryApplyDefaults`/`ApplyLifetime`. Externally nothing changes - the same registrations happen in the same order, wired through the same `ContainerBuilder`.

**Blocked by:** 09

- [ ] `ContainerBuilderExtensions.AddInterlocutor` calls the shared `RegistrationApplier`, supplying `ContainerBuilder`-backed callbacks, instead of its own per-registration-kind methods
- [ ] `ContainerBuilderExtensions.cs`'s own `Apply`/`ApplyFactories`/`TryApplyDefaults`/`ApplyLifetime` are no longer called from `AddInterlocutor` (may still exist dead until ticket 12 removes them, or be removed here if trivial - implementer's call)
- [ ] All existing `Interlocutor.DependencyInjection.Autofac.Tests` pass unchanged, including `InterlocutorBuilderIntegrationTests.cs`
- [ ] Root-scope wiring and report logging are unaffected
