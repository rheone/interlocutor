Status: closed (implemented)

# 09 - Add a shared RegistrationApplier driver (expand)

**What to build:** A new container-neutral type that encodes the seven-step order `AddInterlocutor` applies a `RegistrationReport` in today (handlers → generated registrations → stages → stage factories → behaviors → matched registrations → try-add defaults), built against three small per-registration callbacks (add-handler-or-behavior, add-factory, try-add-default) that a container adapter supplies. This is the expand half of an expand-contract: the new driver is added and tested on its own, but no adapter switches over to it yet, so nothing existing breaks.

**Blocked by:** None - can start immediately

- [x] New type lives in a container-neutral package (alongside `InterlocutorBuilder`/`RegistrationReport`, not in the Microsoft or Autofac package)
- [x] It reproduces, from the shared `RegistrationReport`, the exact seven-step ordering both existing adapters currently hard-code, plus the three lifetime-switch translations (`Apply`/`ApplyFactories`/`TryApplyDefaults`) each currently reimplements independently
- [x] Callbacks are container-agnostic (e.g. delegates/small interfaces), not typed to `IServiceCollection` or `ContainerBuilder`
- [x] New unit tests cover the ordering and each lifetime branch directly against the driver, independent of either container package
- [x] Neither `ServiceCollectionExtensions.cs` nor `ContainerBuilderExtensions.cs` is modified by this ticket - it only adds the new driver
