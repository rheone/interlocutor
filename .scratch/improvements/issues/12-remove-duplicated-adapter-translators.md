Status: closed (implemented)

# 12 - Remove the duplicated per-adapter translators (contract)

**What to build:** The contract step - now that both adapters route through the shared `RegistrationApplier` (tickets 10, 11), delete the dead per-adapter `Apply`/`ApplyFactories`/`TryApplyDefaults`/`ApplyLifetime` methods and their duplicated lifetime-switch logic from `ServiceCollectionExtensions.cs` and `ContainerBuilderExtensions.cs`. A third container adapter, from here on, only has to implement the three `RegistrationApplier` callbacks - not re-derive the seven-step order by reading a sibling package.

**Blocked by:** 10, 11

- [x] `ServiceCollectionExtensions.cs` no longer defines `Apply`/`ApplyFactories`/`TryApplyDefaults`
- [x] `ContainerBuilderExtensions.cs` no longer defines `Apply`/`ApplyFactories`/`TryApplyDefaults`/`ApplyLifetime`
- [x] No behavioral change: full Microsoft and Autofac test suites pass unchanged
- [x] `documentation/builder-and-registration.md` (or wherever the adapter internals are described) updated if it references the removed methods by name

**Implementation note:** `ServiceCollectionExtensions.cs` (Microsoft adapter) had no `Apply`/`ApplyFactories`/`TryApplyDefaults` methods left when this ticket was picked up - already folded into inline `RegistrationApplier` callback lambdas by ticket 10. `ContainerBuilderExtensions.cs` (Autofac adapter) still had `Apply`, `TryApplyDefault`, and `ApplyLifetime` as named private methods serving as its `RegistrationApplier` callback bodies - not dead code, but not yet contracted either. This ticket merges `Apply`/`TryApplyDefault` into one `Translate(builder, registration, tryAdd)` method (the two only differed by the `IfNotRegistered` guard and, for closed types, whether `ResolveServiceType` applies) and renames `ApplyLifetime` to `SetLifetime`, kept as the one shared lifetime-switch helper both branches of `Translate` and the `addFactory` callback call - inlining it separately at each call site would have reintroduced the duplication this ticket removes. No `documentation/` file referenced the old method names, so no doc update was needed. `documentation/builder-and-registration.md` does not exist in this repo; the adapter internals aren't documented by name anywhere consumer-facing.
