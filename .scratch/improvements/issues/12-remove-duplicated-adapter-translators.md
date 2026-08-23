# 12 - Remove the duplicated per-adapter translators (contract)

**What to build:** The contract step - now that both adapters route through the shared `RegistrationApplier` (tickets 10, 11), delete the dead per-adapter `Apply`/`ApplyFactories`/`TryApplyDefaults`/`ApplyLifetime` methods and their duplicated lifetime-switch logic from `ServiceCollectionExtensions.cs` and `ContainerBuilderExtensions.cs`. A third container adapter, from here on, only has to implement the three `RegistrationApplier` callbacks - not re-derive the seven-step order by reading a sibling package.

**Blocked by:** 10, 11

- [ ] `ServiceCollectionExtensions.cs` no longer defines `Apply`/`ApplyFactories`/`TryApplyDefaults`
- [ ] `ContainerBuilderExtensions.cs` no longer defines `Apply`/`ApplyFactories`/`TryApplyDefaults`/`ApplyLifetime`
- [ ] No behavioral change: full Microsoft and Autofac test suites pass unchanged
- [ ] `documentation/builder-and-registration.md` (or wherever the adapter internals are described) updated if it references the removed methods by name
