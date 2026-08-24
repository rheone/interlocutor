# 02 - Align RegistrationLifetime defaults-handling between Microsoft and Autofac DI packages

**What to build:** `Interlocutor.DependencyInjection.Microsoft`'s `ServiceCollectionExtensions.TryApplyDefaults` builds its `ServiceDescriptor` directly from `registration.ServiceType`/`ImplementationType`, skipping the `ResolveServiceType` translation that `Apply` uses for behavior registrations. This is currently harmless (every entry in `Defaults` - `IAuditSink`, `IPolicyEvaluator`, `IRequestContextAccessor`, `IResponseOutcomeDescriber` - is a closed, non-generic type) but silent: nothing documents why `TryApplyDefaults` doesn't need the translation `Apply` does, so a future open-generic default would fail with no obvious link back to `ResolveServiceType`'s reasoning. Separately, `Apply`, `ApplyFactories`, and `TryApplyDefaults` each contain their own switch over `RegistrationLifetime`, where the Autofac package factored the equivalent logic into one shared `ApplyLifetime<>` helper used by all three call sites.

**Blocked by:** None - can start immediately

**Status:** done

- [x] Either `TryApplyDefaults` applies the same `ResolveServiceType` translation `Apply` does, or a comment on `TryApplyDefaults` explains why it's safe to skip (mirroring how thoroughly the rest of the file documents container-specific quirks)
- [x] Microsoft DI's three separate `RegistrationLifetime` switches are collapsed into one shared helper, matching the pattern already used in `Interlocutor.DependencyInjection.Autofac`

## Comments

Verified already implemented, via a larger `RegistrationApplier` (`Interlocutor.DependencyInjection.Abstractions`) refactor shared by both container packages: `TryApplyDefaults`'s `addRegistration`/`tryAddDefault` delegates both call `ResolveServiceType(registration)` identically to `Apply`, so the translation is applied uniformly rather than skipped. Lifetime translation is centralized in a single `ToServiceLifetime`/`CreateDescriptor` pair that every call site (`Apply`, `ApplyFactories`, `TryApplyDefaults`) routes through - one switch, not three. `dotnet test tests/Interlocutor.DependencyInjection.Microsoft.Tests` passes (50/50).
