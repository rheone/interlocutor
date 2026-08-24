# 02 - Align RegistrationLifetime defaults-handling between Microsoft and Autofac DI packages

**What to build:** `Interlocutor.DependencyInjection.Microsoft`'s `ServiceCollectionExtensions.TryApplyDefaults` builds its `ServiceDescriptor` directly from `registration.ServiceType`/`ImplementationType`, skipping the `ResolveServiceType` translation that `Apply` uses for behavior registrations. This is currently harmless (every entry in `Defaults` - `IAuditSink`, `IPolicyEvaluator`, `IRequestContextAccessor`, `IResponseOutcomeDescriber` - is a closed, non-generic type) but silent: nothing documents why `TryApplyDefaults` doesn't need the translation `Apply` does, so a future open-generic default would fail with no obvious link back to `ResolveServiceType`'s reasoning. Separately, `Apply`, `ApplyFactories`, and `TryApplyDefaults` each contain their own switch over `RegistrationLifetime`, where the Autofac package factored the equivalent logic into one shared `ApplyLifetime<>` helper used by all three call sites.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] Either `TryApplyDefaults` applies the same `ResolveServiceType` translation `Apply` does, or a comment on `TryApplyDefaults` explains why it's safe to skip (mirroring how thoroughly the rest of the file documents container-specific quirks)
- [ ] Microsoft DI's three separate `RegistrationLifetime` switches are collapsed into one shared helper, matching the pattern already used in `Interlocutor.DependencyInjection.Autofac`
