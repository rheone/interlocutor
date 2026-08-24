# 01 - Fix Autofac open-service/closed-implementation registration gap

**What to build:** Registering a pipeline behavior with a closed implementation type against the open `IPipelineBehavior<,>` service type - via `AddBehavior(Type, RegistrationLifetime)` or via a type-match rule (ticket 51) that resolves to one - must succeed under Autofac the same way it already does under Microsoft DI. Today, `InterlocutorPipelineBuilder.AddBehavior` always records the open service type paired with a closed implementation type; Microsoft DI's `Apply`/`TryApplyDefaults` translate that pairing via `ResolveServiceType` before registering, but Autofac's `ContainerBuilderExtensions` has no equivalent translation and calls `.As(registration.ServiceType)` with the open type directly - `builder.RegisterType(closedImpl).As(openGenericService)` throws `ArgumentException` at `ContainerBuilder.Build()` time. `ResolveServiceType`'s own doc comment claims Autofac "accepts the open/closed mismatch directly," which was verified false.

**Blocked by:** None - can start immediately

**Status:** done

- [x] Autofac registers a closed-implementation behavior against an open `IPipelineBehavior<,>` service type without throwing at `Build()` time, producing an equivalent registration to Microsoft DI's for the same input
- [x] The incorrect doc comment claiming Autofac needs no such translation is corrected
- [x] A test in `Interlocutor.DependencyInjection.Autofac.Tests` exercises `AddTypeMatch`/`MatchedRegistrations` resolving to a closed behavior type and confirms the container builds and resolves correctly (this path currently has no test coverage at all)

## Comments

Verified already implemented: `ContainerBuilderExtensions.Apply` calls its own `ResolveServiceType(registration)` for the non-open-generic-implementation branch, mirroring Microsoft DI's translation, with a doc comment explaining both containers need it for the identical reason. `ContainerBuilderExtensionsTests.AddTypeMatch_ClosedBehaviorMatchedAgainstOpenService_BuildsAndResolves` (via `ClosedMatchedBehavior`/`MatchedRequest`) covers the previously-untested path. `dotnet test tests/Interlocutor.DependencyInjection.Autofac.Tests` passes (39/39).
