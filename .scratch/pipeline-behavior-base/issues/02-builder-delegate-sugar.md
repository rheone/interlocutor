# 02 — InterlocutorPipelineBuilder.AddBehavior delegate overloads

**What to build:** Builder sugar for registering a `PipelineBehaviorBase`-style enter/exit behavior as a pair of lambdas, without writing a dedicated class, per [../PRD.md](../PRD.md).

Add two overloads of `InterlocutorPipelineBuilder.AddBehavior<TRequest, TResponse>`, mirroring the existing `AddStage` delegate overloads exactly:

- A plain-delegate overload: `(Func<TRequest, CancellationToken, Task<StageResult<TResponse>>>? onEnter, Func<TRequest, TResponse, CancellationToken, Task<TResponse>>? onExit, RegistrationLifetime lifetime = Singleton)` — defaults to `Singleton` since the delegates already capture whatever state they need.
- A service-provider-aware overload: `(Func<IServiceProvider, TRequest, CancellationToken, Task<StageResult<TResponse>>>? onEnter, Func<IServiceProvider, TRequest, TResponse, CancellationToken, Task<TResponse>>? onExit, RegistrationLifetime lifetime = Scoped)` — defaults to `Scoped` for a hook that wants to resolve a scoped dependency fresh per dispatch.

Both overloads construct a `DelegatePipelineBehavior<TRequest, TResponse>` via the same factory-registration path (`InterlocutorFactoryRegistration` against `IPipelineBehavior<TRequest, TResponse>`) the `AddStage` delegate overloads already use, so entries land in the same flat, ordered `Behaviors` list as any type-based `AddBehavior` registration — no new collection, no new resolution path.

**Blocked by:** 01 — needs `DelegatePipelineBehavior<TRequest, TResponse>` to exist

**Status:** ready-for-agent

- [ ] `InterlocutorPipelineBuilder.AddBehavior<TRequest, TResponse>` plain-delegate overload added, default lifetime `Singleton`
- [ ] `InterlocutorPipelineBuilder.AddBehavior<TRequest, TResponse>` service-provider-aware overload added, default lifetime `Scoped`
- [ ] Both overloads register a factory entry against the closed `IPipelineBehavior<TRequest, TResponse>` interface, alongside (not separate from) type-based `AddBehavior` entries in `Behaviors`
- [ ] Either delegate on either overload may be omitted for a no-op default, consistent with `DelegatePipelineBehavior`'s own no-op defaults
- [ ] Tests added to `tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorPipelineBuilderTests.cs`, mirroring `AddStage_WithDelegates_RegistersFactoryForClosedPipelineStageInterface` and `AddStage_WithServiceProviderDelegates_FactoryReceivesTheProvider`: factory registers against the closed `IPipelineBehavior<,>` interface, and the service-provider overload's delegates receive the resolving `IServiceProvider`
- [ ] A behavior registered via either new overload runs in chain position exactly like a type-based `AddBehavior` registration (ordering, short-circuit, and exit-override all work identically)
