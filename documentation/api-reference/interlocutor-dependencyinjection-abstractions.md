# Interlocutor.DependencyInjection.Abstractions API Reference

Every public type in `Interlocutor.DependencyInjection.Abstractions`, grouped by namespace - the container-neutral [DI registration](../glossary.md#di-registration) model shared by the Microsoft and Autofac packages.

## Interlocutor.DependencyInjection.Abstractions

### `GeneratedHandlerKind` enum

Discriminates which of the mediator handler contracts a compile-time discovered handler implements, as carried by `InterlocutorGeneratedHandlerAttribute` for cross-assembly discovery.

| Member | Summary |
| --- | --- |
| `Command = 1` | An ICommandHandler<TCommand, TResponse> implementation. |
| `Notification = 3` | An INotificationHandler<TNotification> implementation. |
| `Query = 0` | An IQueryHandler<TQuery, TResponse> implementation. |
| `BareCommand = 2` | An IBareCommandHandler<TCommand, TResult> implementation. |

### `HandlerDiscovery` class

Scans assemblies for concrete implementations of the mediator's handler contracts and expresses each match as a container-neutral `InterlocutorRegistration`.

| Member | Summary |
| --- | --- |
| `public static IReadOnlyList<InterlocutorRegistration> Discover(IEnumerable<Assembly> assembliesToScan)` | Scans the given assemblies for concrete, non-abstract classes implementing any of the mediator's handler contracts (`IQueryHandler`, `ICommandHandler`, `IBareCommandHandler`, or `INotificationHandler`). |
| `public static IReadOnlyList<InterlocutorRegistration> Discover(IEnumerable<(Assembly Assembly, RegistrationLifetime Lifetime)> assembliesToScan, IReadOnlyList<Type>? extraContracts = null, ICollection<TypeLoadFailure>? failures = null)` | Scans the given assemblies for concrete, non-abstract classes implementing any of the mediator's handler contracts, registering each assembly's matches at the lifetime paired with it - the overload `ScanAssembly` and its siblings use once a caller specifies a lifetime per Scan* call rather than accepting the blanket `Scoped` default. |

### `IRootScopeDetector` interface

Container-neutral abstraction the root-scope guard (see `RootScopeGuardedMediator`) consults to know whether the ambient scope it was resolved into is the root service provider/container itself, rather than a real per-request (or otherwise explicitly created) child scope.

| Member | Summary |
| --- | --- |
| `bool IsRootScope` | Gets a value indicating whether the ambient scope this detector was resolved into is the root service provider/container - i.e., no CreateScope()/BeginLifetimeScope() call sits between the application's root and this resolution. |

### `InterlocutorBuilder` sealed class

Container-neutral fluent builder that is the single configuration surface for Interlocutor - assembly scanning, pipeline behaviors, pipeline stages, and the try-add/override-able defaults a subset of the built-in behaviors depend on.

| Member | Summary |
| --- | --- |
| `public InterlocutorBuilder()` | Initializes a new instance of the `InterlocutorBuilder` class, seeded with the single default every configuration needs regardless of which behaviors are added: `IRequestContextAccessor`. |
| `public IReadOnlyList<Assembly> AssembliesToScan` | Gets the assemblies configured for handler discovery, in the order they were added. |
| `public IReadOnlyList<InterlocutorRegistration> Behaviors` | Gets the flat, ordered list of pipeline behavior registrations, in builder-call order. Empty on a bare builder. May contain the same implementation type more than once. |
| `public IReadOnlyCollection<InterlocutorRegistration> Defaults` | Gets the try-add-able default registrations, one per service type. Always contains exactly one entry per distinct service type - the most recently set entry for that type, whether set by an Add*Behavior convenience method or by the corresponding Override* method. |
| `public IReadOnlyList<Type> ExcludedImplementationTypes` | Gets every implementation type added via `Exclude`/`Exclude<TImplementation>()`, in call order across every call - each call is additive, never a replacement of the accumulated list. |
| `public IReadOnlyList<InterlocutorRegistration> GeneratedRegistrations` | Gets the compile-time generated handler registrations added via `AddGeneratedRegistrations`, in call order. |
| `public IReadOnlyList<InterlocutorRegistration> Handlers` | Gets the merged set of handler registrations from both assembly scanning (`ScanAssembly` and its siblings) and explicit per-type registration (`AddHandler`) - the single list both container packages translate instead of calling `Discover` directly. |
| `public IReadOnlyList<TypeMatchRule> MatchRules` | Gets every predicate-based type-matching rule added via `AddTypeMatch`, in call order. |
| `public IReadOnlyList<InterlocutorRegistration> MatchedRegistrations` | Gets every registration produced by a `TypeMatchRule` matching a candidate type found while scanning - strictly additive to `Handlers` and `Behaviors`, both of which are unaffected by whether any match rules are configured. |
| `public InterlocutorPipelineBuilder Pipeline` | Gets the sole entry point for pipeline composition - every behavior, stage, and behavior-dependency override lives here. See `InterlocutorPipelineBuilder` for the full method set. |
| `public IReadOnlyList<InterlocutorFactoryRegistration> StageFactories` | Gets the delegate-backed pipeline stage registrations added via the AddStage delegate overloads - the lambda-form counterpart to `Stages`. |
| `public IReadOnlyList<InterlocutorRegistration> Stages` | Gets the pipeline stage registrations added via `AddStage`, each keyed to the closed `IPipelineStage` interface it implements. |
| `public IReadOnlyList<Type> UniqueClosureContracts` | Gets every open-generic contract, alongside the three built-in handler contracts, added via `RequireUniqueClosure` for `ValidateOnBuild`'s `AmbiguousClosure` check to also cover - and, alongside a built-in handler contract, for scanning to register every closed implementation of. |
| `public InterlocutorBuilder AddGeneratedRegistrations(IEnumerable<InterlocutorRegistration> registrations)` | Adds handler registrations discovered at compile time by Interlocutor.Generator, alongside - never instead of - any registrations produced by `ScanAssembly`/`ScanAssemblies`. |
| `public InterlocutorBuilder AddHandler(Type handlerImplementationType, RegistrationLifetime lifetime = Scoped)` | Registers a single handler implementation type directly, without assembly scanning - the manual escape hatch for handlers that mirrors `AddBehavior`'s shape. Reflects over the type's implemented interfaces and registers one `Handler` entry per matching handler contract interface it implements (open or closed), each carrying `Explicit` provenance. |
| `public InterlocutorBuilder AddTypeMatch(Func<Type, bool> predicate, TypeMatchTarget target, RegistrationLifetime lifetime = Scoped, Func<Type, bool>? except = null, bool require = false, string? name = null)` | Adds a predicate-based type-matching rule: every candidate type found while scanning that `predicate` matches (and `except`, if given, does not) is registered as `target` describes. Strictly additive to the existing marker-interface-based behavior scoping (`ITransactionalRequest`, `IAuditableRequest`, `IRequiresPolicyRequest`, `IExternalStoreRequest`) - a behavior scoped by a marker interface constraint is unaffected by whether any match rules are configured. |
| `public RegistrationReport DescribeRegistrations()` | Builds a structured, container-neutral report of everything this builder has accumulated so far - every registration with its provenance, every optional-package probe that found nothing, and (only once `ValidateOnBuild` is configured) every validation problem found. Buildable and inspectable at any time; `Problems` stays empty when `ValidateOnBuild` was never called, since computing it means re-scanning every configured assembly and is therefore not free - consistent with `ValidateOnBuild` being opt-in. |
| `public InterlocutorBuilder Exclude(params Type[] implementationTypes)` | Removes one or more implementation types from `Handlers` entirely - the escape hatch for clearing a `AmbiguousClosure` conflict among candidates `ScanAssembly` (or its siblings) found, by dropping every losing candidate rather than picking a winner some other way. |
| `public InterlocutorBuilder Exclude<TImplementation>()` | Removes `TImplementation` from `Handlers` entirely. See `Exclude`. |
| `public InterlocutorBuilder OverrideRequestContextAccessor<TAccessor>(RegistrationLifetime lifetime = Scoped)` | Replaces the default `IRequestContextAccessor` implementation (`AmbientRequestContextAccessor`) seeded by the constructor. |
| `public InterlocutorBuilder RequireUniqueClosure(Type openGenericContract)` | Opts a consumer-supplied open-generic contract into the same "at most one implementation per closed instantiation" expectation the three built-in mediator handler contracts (IQueryHandler<,>, ICommandHandler<,>, IBareCommandHandler<,>) already carry - both `ValidateOnBuild`'s `AmbiguousClosure` check and `Exclude`'s conflict-clearing escape hatch cover it from then on. |
| `public InterlocutorBuilder ScanAssemblies(params Assembly[] assemblies)` | Adds one or more assemblies, all at the default scoped lifetime, to scan for handler implementations, in the given order. |
| `public InterlocutorBuilder ScanAssemblies(RegistrationLifetime lifetime, params Assembly[] assemblies)` | Adds one or more assemblies to scan for handler implementations, in the given order. |
| `public InterlocutorBuilder ScanAssembly(Assembly assembly, RegistrationLifetime lifetime = Scoped)` | Adds an assembly to scan for handler implementations. |
| `public InterlocutorBuilder ScanAssemblyContaining<TMarker>(RegistrationLifetime lifetime = Scoped)` | Adds the assembly containing `TMarker` to scan for handler implementations. |
| `public InterlocutorBuilder ScanEntryAssembly(RegistrationLifetime lifetime = Scoped)` | Adds the entry assembly to scan for handler implementations. |
| `public InterlocutorBuilder ValidateOnBuild()` | Opts into `DescribeRegistrations` computing `Problems` - checking the accumulated registration list, with no live container needed, for a query or command type discovered while scanning with no matching handler, a required type-matching rule (`AddTypeMatch`) that matched nothing, two registrations of the same (service type, implementation type) pair at conflicting lifetimes, more than one distinct implementation type closing the same instantiation of a single-winner contract (`RequireUniqueClosure`), and two pipeline override rules (`OverridePipelineFor`) matching the same request type. When the container translator finds any such problem, it throws a single `InterlocutorRegistrationValidationException` carrying every problem found, aggregated rather than failing on the first. |

### `InterlocutorFactoryRegistration` struct

A container-neutral description of a single factory-backed service registration - the delegate-form counterpart to `InterlocutorRegistration`, used where a registration is a caller-supplied closure rather than a type the container can construct on its own (for example, a lambda-form pipeline stage registered via `AddStage`'s delegate overloads).

| Member | Summary |
| --- | --- |
| `public InterlocutorFactoryRegistration(Type ServiceType, Func<IServiceProvider, object> Factory, RegistrationLifetime Lifetime, RegistrationKind Kind)` |  |
| `public Func<IServiceProvider, object> Factory` | Produces the service instance given the container's `IServiceProvider` - used both by factories that need no services (which simply ignore the parameter) and by lambda-form stages that resolve services from it. |
| `public RegistrationKind Kind` | The role this registration plays, used by translators and future override policy. |
| `public RegistrationLifetime Lifetime` | The service lifetime the registration should use. |
| `public Type ServiceType` | The closed service (contract) type the registration satisfies. |
| `public void Deconstruct(out Type ServiceType, out Func<IServiceProvider, object> Factory, out RegistrationLifetime Lifetime, out RegistrationKind Kind)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(InterlocutorFactoryRegistration other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(InterlocutorFactoryRegistration left, InterlocutorFactoryRegistration right)` |  |
| `public static bool operator !=(InterlocutorFactoryRegistration left, InterlocutorFactoryRegistration right)` |  |

### `InterlocutorGeneratedHandlerAttribute` sealed class

Assembly-level marker describing one compile-time discovered query, command, or void-command handler's request/response shape (ticket 12). Applied once per matching handler by Interlocutor.Generator - never by hand.

| Member | Summary |
| --- | --- |
| `public InterlocutorGeneratedHandlerAttribute(Type requestType, Type? responseType, GeneratedHandlerKind kind)` |  |
| `public GeneratedHandlerKind Kind` | Gets which handler contract `RequestType` is handled by. |
| `public Type RequestType` | Gets the handler's request type. |
| `public Type? ResponseType` | Gets the handler's response type, or `null` for a void-command handler. |

### `InterlocutorGeneratedModuleAttribute` sealed class

Assembly-level marker identifying the compile-time generated registration module a handler-defining assembly emits. Applied once per assembly by Interlocutor.Generator - never by hand.

| Member | Summary |
| --- | --- |
| `public InterlocutorGeneratedModuleAttribute(Type moduleType)` |  |
| `public Type ModuleType` | Gets the generated registration module type this attribute identifies. |

### `InterlocutorPipelineBuilder` sealed class

Container-neutral fluent builder for pipeline composition alone - behaviors, stages, the recommended-chain sugar, per-request-type pipeline overrides, and the try-add/override-able defaults the built-in behaviors depend on.

| Member | Summary |
| --- | --- |
| `public InterlocutorPipelineBuilder()` | Initializes a new instance of the `InterlocutorPipelineBuilder` class, standalone, with its own empty defaults catalog. |
| `public IReadOnlyList<InterlocutorRegistration> Behaviors` | Gets the flat, ordered list of pipeline behavior registrations, in builder-call order. Empty on a bare builder. May contain the same implementation type more than once. |
| `public IReadOnlyCollection<InterlocutorRegistration> Defaults` | Gets the try-add-able default registrations this builder has set, one per service type. When constructed standalone, this is exactly the set of pipeline-behavior dependency defaults added by this builder's own calls. When constructed as part of an `InterlocutorBuilder`, this is the same shared dictionary `Defaults` exposes. |
| `public IReadOnlyList<PipelineOverrideRule> OverrideRules` | Gets every Tier-1 pipeline override rule added via `OverridePipelineFor`, in call order. |
| `public PipelineExplainerOptions? PipelineExplainerConfiguration` | Gets the configuration `AddPipelineExplainer` was called with, or `null` if it was never called. |
| `public IReadOnlyList<SkippedRegistration> Skipped` | Gets every registration this builder deliberately did not make, and why - currently only the silent optional-package probes behind `AddValidationBehavior` and `AddUnitOfWorkBehavior` when the corresponding package is not referenced. |
| `public IReadOnlyList<InterlocutorFactoryRegistration> StageFactories` | Gets the delegate-backed pipeline stage registrations added via the AddStage delegate overloads - the lambda-form counterpart to `Stages`. |
| `public IReadOnlyList<InterlocutorRegistration> Stages` | Gets the pipeline stage registrations added via `AddStage`, each keyed to the closed `IPipelineStage` interface it implements. |
| `public InterlocutorPipelineBuilder AddAuditingBehavior()` | Adds `AuditingBehavior` at the current position in the chain, and try-adds its default dependency, `IAuditSink` (`LoggingAuditSink`), unless a default has already been set. See `AddLoggingBehavior` for why no `IResponseOutcomeDescriber` default is registered here either. |
| `public InterlocutorPipelineBuilder AddAuthorizationBehavior()` | Adds `AuthorizationBehavior` at the current position in the chain, and try-adds its default dependencies, `IPolicyEvaluator` (`DefaultPolicyEvaluator`) and `IOutcomeCaseRegistry` (`DefaultOutcomeCaseRegistry`), unless a default has already been set for either. |
| `public InterlocutorPipelineBuilder AddBehavior(Type behaviorImplementationType, RegistrationLifetime lifetime = Scoped)` | Adds a pipeline behavior implementation at the current position in the chain. This is the general escape hatch every named Add*Behavior convenience method is sugar over - use it directly for a custom or third-party `IPipelineBehavior` implementation this builder has no dedicated method for. |
| `public InterlocutorPipelineBuilder AddCancellationBehavior()` | Adds `CancellationBehavior` at the current position in the chain, and try-adds its default dependency, `IOutcomeCaseRegistry` (`DefaultOutcomeCaseRegistry`), unless a default has already been set. |
| `public InterlocutorPipelineBuilder AddLoggingBehavior()` | Adds `LoggingBehavior` at the current position in the chain. |
| `public InterlocutorPipelineBuilder AddPipelineExplainer(PipelineExplainerOptions? options = null)` | Registers the diagnostic `IPipelineExplainer` - the runtime counterpart to `InterlocutorBuilder.DescribeRegistrations` that answers which pipeline behaviors resolve for a request/response type pair known only at run time. Opt-in at two independent levels: registered at all only when this method is called, and disabled (`Explain` throws) until `PipelineExplainerOptions.Enabled` is `true`. |
| `public InterlocutorPipelineBuilder AddRateLimitedBehavior()` | Adds `RateLimitedBehavior` at the current position in the chain, and try-adds its default dependencies, `IRateLimiter` (`DefaultRateLimiter`) and `IOutcomeCaseRegistry` (`DefaultOutcomeCaseRegistry`), unless a default has already been set for either. |
| `public InterlocutorPipelineBuilder AddStage(Type stageImplementationType, RegistrationLifetime lifetime = Scoped)` | Registers a pipeline stage implementation directly, without assembly scanning. Reflects over the type's implemented interfaces and registers one `Stage` entry per closed `IPipelineStage` interface it implements - mirroring how `HandlerDiscovery` registers a handler per closed contract interface. |
| `public InterlocutorPipelineBuilder AddStage<TStage>(RegistrationLifetime lifetime = Scoped)` | Registers a pipeline stage implementation directly, without assembly scanning. See `AddStage`. |
| `public InterlocutorPipelineBuilder AddStage<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<StageResult<TResponse>>>? onBefore = null, Func<TRequest, TResponse, CancellationToken, Task>? onAfter = null, RegistrationLifetime lifetime = Singleton)` | Registers a pipeline stage as a pair of delegates instead of a dedicated implementation type - sugar over `DelegatePipelineStage` for stages simple enough not to need one. Either delegate may be omitted for a no-op hook. Legal alongside `AddStage`; both add entries the stage host aggregates. |
| `public InterlocutorPipelineBuilder AddStage<TRequest, TResponse>(Func<IServiceProvider, TRequest, CancellationToken, Task<StageResult<TResponse>>>? onBefore, Func<IServiceProvider, TRequest, TResponse, CancellationToken, Task>? onAfter, RegistrationLifetime lifetime = Scoped)` | Registers a pipeline stage as a pair of delegates that receive the container's `IServiceProvider` - the overload for lambda-form stages that need to resolve services (for example, a scoped dependency) rather than capturing them at builder-configuration time. See `AddStage` for the simpler, service-free overload. |
| `public InterlocutorPipelineBuilder AddStageHost(RegistrationLifetime lifetime = Scoped)` | Adds `PipelineStageBehavior` - the single host behavior that aggregates every registered `IPipelineStage` - at the current position in the chain. An ordinary behavior entry: position it anywhere, and register it more than once if different stage groupings should sit at different depths. |
| `public InterlocutorPipelineBuilder AddTimeoutBehavior()` | Adds `TimeoutBehavior` at the current position in the chain, and try-adds its default dependencies, `ITimeoutPolicy` (`DefaultTimeoutPolicy`) and `IOutcomeCaseRegistry` (`DefaultOutcomeCaseRegistry`), unless a default has already been set for either. |
| `public InterlocutorPipelineBuilder AddUnitOfWorkBehavior()` | Adds the EF Core or NHibernate UnitOfWorkBehavior - the unit-of-work-owned transaction commit/rollback gate - at the current position in the chain, preferring EF Core, if either store-specific persistence package is referenced by the application. A no-op when neither package is loaded. |
| `public InterlocutorPipelineBuilder AddValidationBehavior()` | Adds ValidationBehavior<,> at the current position in the chain, if the Interlocutor.FluentValidation package is referenced by the application, and try-adds its default dependency, `IOutcomeCaseRegistry` (`DefaultOutcomeCaseRegistry`), unless a default has already been set. A no-op when that package is not loaded. |
| `public InterlocutorPipelineBuilder OverrideAuditSink<TSink>(RegistrationLifetime lifetime = Scoped)` | Replaces the default `IAuditSink` implementation `AuditingBehavior` receives when no application-level registration is present. Wins regardless of whether it is called before or after `AddAuditingBehavior` in the same chain. |
| `public InterlocutorPipelineBuilder OverrideOutcomeCaseRegistry<TRegistry>(RegistrationLifetime lifetime = Scoped)` | Replaces the default `IOutcomeCaseRegistry` implementation `AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, and the FluentValidation ValidationBehavior receive when no application-level registration is present. Wins regardless of whether it is called before or after the corresponding Add*Behavior call in the same chain. |
| `public InterlocutorPipelineBuilder OverrideOutcomeDescriber<TDescriber>(RegistrationLifetime lifetime = Singleton)` | Replaces the default `IResponseOutcomeDescriber` implementation `LoggingBehavior` and `AuditingBehavior` optionally consume when no application-level registration is present. Wins regardless of whether it is called before or after `AddLoggingBehavior` / `AddAuditingBehavior` in the same chain. |
| `public InterlocutorPipelineBuilder OverridePipelineFor(Func<Type, bool> predicate, string name, IReadOnlyList<Type> behaviors)` | Declares a Tier-1 pipeline override: a request type `predicate` matches gets exactly `behaviors`, in the given order, in place of the general-purpose baseline (Tier 2) entirely - a full, positively-declared replacement, never a merge with or modification of it. An unmatched request's resolved pipeline is unaffected. |
| `public InterlocutorPipelineBuilder OverridePipelineFor<TRequest>(string name, params Type[] behaviors)` | Declares a Tier-1 pipeline override for exactly `TRequest` - the convenience overload of `OverridePipelineFor` for the common single-type case. See that overload for the full override semantics. |
| `public InterlocutorPipelineBuilder OverridePolicyEvaluator<TEvaluator>(RegistrationLifetime lifetime = Scoped)` | Replaces the default `IPolicyEvaluator` implementation `AuthorizationBehavior` receives when no application-level registration is present. Wins regardless of whether it is called before or after `AddAuthorizationBehavior` in the same chain. |
| `public InterlocutorPipelineBuilder OverrideRateLimiter<TLimiter>(RegistrationLifetime lifetime = Scoped)` | Replaces the default `IRateLimiter` implementation `RateLimitedBehavior` receives when no application-level registration is present. Wins regardless of whether it is called before or after `AddRateLimitedBehavior` in the same chain. |
| `public InterlocutorPipelineBuilder OverrideTimeoutPolicy<TPolicy>(RegistrationLifetime lifetime = Scoped)` | Replaces the default `ITimeoutPolicy` implementation `TimeoutBehavior` receives when no application-level registration is present. Wins regardless of whether it is called before or after `AddTimeoutBehavior` in the same chain. |
| `public InterlocutorPipelineBuilder UseRecommendedChain()` | Applies the library's documented recommended pipeline chain - logging, then validation, then transaction - in that order, as sugar over the individual Add*Behavior calls. Auditing/authorization are deliberately not part of this minimal default; add them back via AddAuditingBehavior()/AddAuthorizationBehavior(). Never applied automatically; a consumer opts in explicitly. |

### `InterlocutorRegistration` struct

A container-neutral description of a single service registration: what service type it satisfies, what implementation type provides it, its lifetime, and what role it plays.

| Member | Summary |
| --- | --- |
| `public InterlocutorRegistration(Type ServiceType, Type ImplementationType, RegistrationLifetime Lifetime, RegistrationKind Kind, RegistrationProvenance Provenance = Explicit, string? MatchedByRule = null)` |  |
| `public Type ImplementationType` | The concrete or open-generic type that implements `ServiceType`. |
| `public RegistrationKind Kind` | The role this registration plays, used by translators and future override policy. |
| `public RegistrationLifetime Lifetime` | The service lifetime the registration should use. |
| `public string? MatchedByRule` | When `Provenance` is `Matched`, identifies which `TypeMatchRule` produced this registration - either that rule's own `Name`, or its position among `MatchRules` when unnamed. `null` for every other `Provenance`. |
| `public RegistrationProvenance Provenance` | Which builder mechanism produced this registration. Defaults to `Explicit` since most construction sites (behaviors, stages, defaults, generator-emitted entries) are single-target calls; `Discover` is the one caller that overrides it to `Scanned`. |
| `public Type ServiceType` | The service (contract) type the registration satisfies. May be an open generic type definition (for example, IPipelineBehavior<,>) when `Kind` is `Behavior`. |
| `public void Deconstruct(out Type ServiceType, out Type ImplementationType, out RegistrationLifetime Lifetime, out RegistrationKind Kind, out RegistrationProvenance Provenance, out string? MatchedByRule)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(InterlocutorRegistration other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(InterlocutorRegistration left, InterlocutorRegistration right)` |  |
| `public static bool operator !=(InterlocutorRegistration left, InterlocutorRegistration right)` |  |

### `InterlocutorRegistrationValidationException` sealed class

Thrown by AddInterlocutor() when `ValidateOnBuild` is configured and `DescribeRegistrations` finds one or more problems in the accumulated registration list - every problem found is aggregated into this single throw, rather than failing on the first.

| Member | Summary |
| --- | --- |
| `public InterlocutorRegistrationValidationException(RegistrationReport report)` | Initializes a new instance of the `InterlocutorRegistrationValidationException` class. |
| `public RegistrationReport Report` | Gets the registration report whose `Problems` triggered this exception. |

### `PipelineOverrideRegistry` sealed class

The container-neutral `IPipelineOverrideRegistry` implementation both Microsoft and Autofac register as a singleton from `OverrideRules`, so PipelineChainResolver resolves an identical answer regardless of which container built it.

| Member | Summary |
| --- | --- |
| `public PipelineOverrideRegistry(IReadOnlyList<PipelineOverrideRule> rules)` |  |
| `public PipelineOverrideMatch? TryGetOverride(Type requestType)` |  |

### `PipelineOverrideRule` record

A Tier-1 pipeline override rule added via `OverridePipelineFor` - a request type matched by `Predicate` gets exactly `BehaviorTypes`, in declared order, in place of the general-purpose baseline (Tier 2) entirely.

| Member | Summary |
| --- | --- |
| `public PipelineOverrideRule(Func<Type, bool> Predicate, string Name, IReadOnlyList<Type> BehaviorTypes)` |  |
| `public IReadOnlyList<Type> BehaviorTypes` | The complete, explicitly-ordered behavior list a matched request resolves instead of the general-purpose baseline. Each entry is either an open generic type definition or a closed implementation type - the same shape `AddBehavior` accepts. |
| `public string Name` | A label identifying this rule, required (unlike `Name`) since it is the only way a `ConflictingPipelineOverride` message can distinguish which rules conflict. |
| `public Func<Type, bool> Predicate` | Tested against a request's runtime type at dispatch time. |
| `public void Deconstruct(out Func<Type, bool> Predicate, out string Name, out IReadOnlyList<Type> BehaviorTypes)` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(PipelineOverrideRule? other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(PipelineOverrideRule? left, PipelineOverrideRule? right)` |  |
| `public static bool operator !=(PipelineOverrideRule? left, PipelineOverrideRule? right)` |  |

### `RegistrationApplier` sealed class

Drives the seven-step order every container adapter's AddInterlocutor applies an `InterlocutorBuilder`'s accumulated registrations in - handlers, generated registrations, stages, stage factories, behaviors, matched registrations, then try-add defaults - against three small, container-agnostic callbacks a container adapter supplies.

| Member | Summary |
| --- | --- |
| `public RegistrationApplier(Action<InterlocutorRegistration> addRegistration, Action<InterlocutorFactoryRegistration> addFactory, Action<InterlocutorRegistration> tryAddDefault)` |  |
| `public void Apply(InterlocutorBuilder builder)` | Applies every registration `builder` has accumulated, in the same seven-step order every container adapter's AddInterlocutor uses: `Handlers`, `GeneratedRegistrations`, `Stages`, `StageFactories`, `Behaviors`, `MatchedRegistrations`, then `Defaults`. |

### `RegistrationKind` enum

Discriminates what role an `InterlocutorRegistration` plays, so a translator can apply container-specific policy (such as override precedence) per role without inspecting types.

| Member | Summary |
| --- | --- |
| `Behavior = 1` | A pipeline behavior registered against the request/response pipeline - one of the library's built-in behaviors (logging, auditing, authorization, validation, transaction) or a consumer's own, added via the builder's general AddBehavior escape hatch. Like `Handler` and `Stage`, a behavior carries no override precedence - it is translated with add, not try-add, semantics, and the same behavior type may be added more than once. |
| `Default = 2` | A default implementation of a service a pipeline behavior depends on (for example, IAuditSink or IPolicyEvaluator) that a consumer's own registration may override. |
| `Dispatcher = 4` | The generator-emitted IGeneratedRequestDispatcher implementation - a single, compilation-wide singleton registered alongside `Handler` entries whenever a compilation's generated registrations are added via InterlocutorBuilder.AddGeneratedRegistrations. Like `Handler` and `Stage`, it carries no override precedence. |
| `Handler = 0` | A handler discovered by scanning one or more assemblies for implementations of the mediator's handler contracts (query, command, bare command, or notification handler). |
| `Stage = 3` | An IPipelineStage<,> implementation registered directly (not discovered by assembly scanning) via the builder's stage-registration methods. Like `Handler`, a stage carries no override precedence - it is translated with add, not try-add, semantics. |

### `RegistrationLifetime` enum

Container-neutral service lifetime for an `InterlocutorRegistration`.

| Member | Summary |
| --- | --- |
| `Scoped = 1` | A single instance is created and shared within one DI scope. |
| `Singleton = 0` | A single instance is created and shared for the lifetime of the container. |
| `Transient = 2` | A new instance is created every time the service is resolved. |

### `RegistrationProblem` struct

A single problem `ValidateOnBuild` found while checking the accumulated registration list - answerable from that list alone, with no live container needed.

| Member | Summary |
| --- | --- |
| `public RegistrationProblem(RegistrationProblemKind Kind, string Description)` |  |
| `public string Description` | A human-readable description of the specific problem found. |
| `public RegistrationProblemKind Kind` | What kind of problem this is. |
| `public void Deconstruct(out RegistrationProblemKind Kind, out string Description)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(RegistrationProblem other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(RegistrationProblem left, RegistrationProblem right)` |  |
| `public static bool operator !=(RegistrationProblem left, RegistrationProblem right)` |  |

### `RegistrationProblemKind` enum

Discriminates what kind of issue a `RegistrationProblem` reports - every check `ValidateOnBuild` performs against the accumulated registration list.

| Member | Summary |
| --- | --- |
| `AmbiguousClosure = 3` | More than one distinct implementation type closes the same instantiation of a single-winner open generic contract - one of IQueryHandler<,>, ICommandHandler<,>, IBareCommandHandler<,>, or a contract a consumer opted in via `RequireUniqueClosure`. INotificationHandler<> is deliberately never checked - fan-out to many handlers per notification is its whole design. |
| `ConflictingLifetime = 2` | Two registrations of the same (service type, implementation type) pair were accumulated with different `RegistrationLifetime` values. |
| `ConflictingPipelineOverride = 4` | Two or more Tier-1 pipeline override rules added via `OverridePipelineFor` matched the same request type - which one should win is never decided silently or by call order. There is no precedence-declaration escape hatch; resolve the conflict by narrowing one rule's match criteria so the two no longer overlap. |
| `UnhandledRequestType = 0` | A request type discovered while scanning has no matching `Handler` registration. |
| `UnmatchedRequiredRule = 1` | A match rule marked as requiring at least one match (see the type-matching registration builder methods) matched no candidate type. |

### `RegistrationProvenance` enum

Discriminates how an `InterlocutorRegistration` came to be accumulated by `InterlocutorBuilder` - which builder mechanism produced it, as opposed to `RegistrationKind`, which describes what role it plays.

| Member | Summary |
| --- | --- |
| `Explicit = 1` | Registered directly by an explicit, single-target builder call - `AddHandler`, `AddBehavior`, a named Add*Behavior convenience method, a stage registration, or a default - rather than discovered by scanning or produced by a predicate match rule. |
| `Matched = 2` | Produced by a predicate-based type-matching rule, which tests a rule against every candidate type found while scanning and applies its action to every match - as opposed to `Scanned`, which tests every candidate type against the fixed set of known handler contract interfaces. |
| `Scanned = 0` | Discovered by scanning a configured assembly (`ScanAssembly` and its siblings) for an implementation of a mediator handler contract. |

### `RegistrationReport` sealed class

A structured, container-neutral snapshot of everything an `InterlocutorBuilder` configuration accumulated - every registration, every optional-package probe that found nothing, and every validation problem, in one shared model.

| Member | Summary |
| --- | --- |
| `public RegistrationReport(IReadOnlyList<InterlocutorRegistration> registrations, IReadOnlyList<InterlocutorFactoryRegistration> stageFactories, IReadOnlyList<SkippedRegistration> skipped, IReadOnlyList<RegistrationProblem> problems, IReadOnlyList<TypeLoadFailure>? typeLoadFailures = null)` | Initializes a new instance of the `RegistrationReport` class. |
| `public IReadOnlyList<RegistrationProblem> Problems` | Gets every validation problem found by `ValidateOnBuild`. Empty unless that method was configured and found something. |
| `public IReadOnlyList<InterlocutorRegistration> Registrations` | Gets every accumulated `InterlocutorRegistration` entry - handlers (scanned, explicit, or matched), behaviors, stages, defaults, and generated registrations alike, each carrying its own `Kind` and `Provenance`. |
| `public IReadOnlyList<SkippedRegistration> Skipped` | Gets every registration the builder deliberately did not make, and why. |
| `public IReadOnlyList<InterlocutorFactoryRegistration> StageFactories` | Gets every accumulated delegate-backed (lambda-form) stage registration. |
| `public IReadOnlyList<TypeLoadFailure> TypeLoadFailures` | Gets every type that failed to load while scanning an assembly, and why - non-fatal, since scanning continues past it. Empty when every scanned type loaded successfully, regardless of whether `ValidateOnBuild` was ever called. |
| `public string Format()` | Renders this report as human-readable text, suitable for logging or console output. |
| `public override string ToString()` | Renders this report as human-readable text. See `Format`. |

### `RootScopeGuardedMediator` sealed class

Decorates the library's real `Mediator` with the root-scope guard: every dispatch method (Send, Publish) checks `IsRootScope` first, throwing `MediatorResolvedFromRootScopeException` - naming the request/notification type being dispatched - instead of silently running the request against services resolved from the root provider/container.

| Member | Summary |
| --- | --- |
| `public RootScopeGuardedMediator(IMediator inner, IRootScopeDetector rootScopeDetector)` |  |
| `public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default(CancellationToken))` |  |
| `public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct = default(CancellationToken))` |  |
| `public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default(CancellationToken))` |  |
| `public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default(CancellationToken))` |  |
| `public Task<TResult> Send<TResult>(ICommand command, CancellationToken ct = default(CancellationToken))` |  |
| `public Task<IOutcome> Send(object request, CancellationToken ct = default(CancellationToken))` |  |

### `SkippedRegistration` struct

Records a registration the builder deliberately did not make, and why - most notably the silent optional-package probing behind `AddValidationBehavior` and `AddUnitOfWorkBehavior`, which stays silent as behavior (a missing optional package is not an error) but becomes visible as an entry in `DescribeRegistrations`'s report.

| Member | Summary |
| --- | --- |
| `public SkippedRegistration(string Description, string Reason)` |  |
| `public string Description` | What was not registered - typically the type name a probe looked for. |
| `public string Reason` | Why it was skipped. |
| `public void Deconstruct(out string Description, out string Reason)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(SkippedRegistration other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(SkippedRegistration left, SkippedRegistration right)` |  |
| `public static bool operator !=(SkippedRegistration left, SkippedRegistration right)` |  |

### `TypeLoadFailure` struct

Records one type that failed to load while scanning an assembly - surfaced as a non-fatal entry in `TypeLoadFailures` instead of aborting the scan that hit it.

| Member | Summary |
| --- | --- |
| `public TypeLoadFailure(string AssemblyName, string Detail)` |  |
| `public string AssemblyName` | The name of the assembly one or more of whose types failed to load. |
| `public string Detail` | The loader exception's own message for the failed type - typically names the type itself, since `Types` carries a `null` entry (not the failed `Type`) for each failure. |
| `public void Deconstruct(out string AssemblyName, out string Detail)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(TypeLoadFailure other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(TypeLoadFailure left, TypeLoadFailure right)` |  |
| `public static bool operator !=(TypeLoadFailure left, TypeLoadFailure right)` |  |

### `TypeMatchRule` record

A predicate-based registration rule added via `AddTypeMatch` - tested against every candidate type found while scanning, with every match registered as `Target` describes, minus anything `Except` carves out.

| Member | Summary |
| --- | --- |
| `public TypeMatchRule(Func<Type, bool> Predicate, TypeMatchTarget Target, RegistrationLifetime Lifetime, Func<Type, bool>? Except, bool Require, string? Name = null)` |  |
| `public Func<Type, bool>? Except` | An optional carve-out predicate - a candidate type matching `Predicate` is skipped if this also returns `true` for it. `null` excludes nothing. |
| `public RegistrationLifetime Lifetime` | The service lifetime to register each match with. |
| `public string? Name` | An optional label identifying this rule, carried onto every `InterlocutorRegistration` it produces (`MatchedByRule`) so `Format` can distinguish which rule matched a given type when more than one is configured. Defaults to this rule's position among `MatchRules` when not given. |
| `public Func<Type, bool> Predicate` | Tested against every candidate type found while scanning; every match is registered. |
| `public bool Require` | Whether this rule is required to match at least one candidate type. Recorded here for `ValidateOnBuild` to check; this record does not enforce it itself. |
| `public TypeMatchTarget Target` | What role each matching type is registered as. |
| `public void Deconstruct(out Func<Type, bool> Predicate, out TypeMatchTarget Target, out RegistrationLifetime Lifetime, out Func<Type, bool>? Except, out bool Require, out string? Name)` |  |
| `public override bool Equals(object? obj)` |  |
| `public bool Equals(TypeMatchRule? other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(TypeMatchRule? left, TypeMatchRule? right)` |  |
| `public static bool operator !=(TypeMatchRule? left, TypeMatchRule? right)` |  |

### `TypeMatchTarget` enum

What role a predicate-based type-matching rule (`AddTypeMatch`) registers each matching type as.

| Member | Summary |
| --- | --- |
| `Behavior = 1` | Register each matching type against the open `IPipelineBehavior` contract, the same way `AddBehavior` would. |
| `Handler = 0` | Register each matching type the same way `AddHandler` would - one `Handler` entry per mediator handler contract interface it implements. |
