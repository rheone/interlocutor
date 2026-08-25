Status: closed (implemented)

# 10 - Migrate the Microsoft adapter onto RegistrationApplier

**What to build:** `Interlocutor.DependencyInjection.Microsoft`'s `AddInterlocutor` drives registration through the shared `RegistrationApplier` (ticket 09) instead of its own `Apply`/`ApplyFactories`/`TryApplyDefaults` methods. Externally nothing changes - the same registrations happen in the same order, wired through the same `IServiceCollection`.

**Blocked by:** 09

- [x] `ServiceCollectionExtensions.AddInterlocutor` calls the shared `RegistrationApplier`, supplying `IServiceCollection`-backed callbacks, instead of its own per-registration-kind methods
- [x] `ServiceCollectionExtensions.cs`'s own `Apply`/`ApplyFactories`/`TryApplyDefaults` are no longer called from `AddInterlocutor` (may still exist dead until ticket 12 removes them, or be removed here if trivial - implementer's call)
- [x] All existing `Interlocutor.DependencyInjection.Microsoft.Tests` pass unchanged, including `InterlocutorBuilderIntegrationTests.cs` and `GeneratedRegistrationIntegrationTests.cs`
- [x] Root-scope wiring (`RootScopeGuardedMediator`/`Mediator`/`ISender`/`IPublisher`/`RootScopeDetector`) and report logging are unaffected
