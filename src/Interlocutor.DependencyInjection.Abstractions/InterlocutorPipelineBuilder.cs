namespace Interlocutor.DependencyInjection.Abstractions;

using System.Diagnostics.CodeAnalysis;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Pipeline.Diagnostics;

/// <summary>
/// Container-neutral fluent builder for pipeline composition alone - behaviors, stages, the
/// recommended-chain sugar, per-request-type pipeline overrides, and the try-add/override-able defaults
/// the built-in behaviors depend on.
/// </summary>
/// <remarks>
/// <para>
/// This is <see cref="InterlocutorBuilder"/>'s pipeline-composition responsibility, reachable only
/// through <see cref="InterlocutorBuilder.Pipeline"/>, so a consumer (and a reader of this code) can
/// reason about "what runs in what order" as a self-contained concern, separate from "which assemblies
/// get scanned" and "which ambient infrastructure defaults are overridden" - both of which remain
/// <see cref="InterlocutorBuilder"/>'s own job.
/// </para>
/// <para>
/// Per <c>ADR-0009</c> ("No Default Pipeline"), a builder with no behavior calls produces zero pipeline
/// behaviors - <see cref="Behaviors"/> starts empty and stays empty unless a builder call adds to it.
/// </para>
/// <para>
/// <b>Order and duplicates.</b> <see cref="Behaviors"/> is a flat list in builder-call order. Adding the
/// same behavior type more than once is legal - neither this type nor a container translator
/// de-duplicates. <see cref="BehaviorFactories"/> is the lambda-form counterpart holding entries added via
/// the <c>AddBehavior</c> delegate overloads - a separate list, applied as its own step immediately after
/// <see cref="Behaviors"/>, mirroring how <see cref="StageFactories"/> relates to <see cref="Stages"/>.
/// </para>
/// <para>
/// <b>Override precedence.</b> Every entry in <see cref="Defaults"/> is translated with try-add
/// semantics by the container translators, so a consumer's own service registration made before the
/// container translator runs always wins. The <c>Override*</c> methods on this type
/// (<see cref="OverrideTimeoutPolicy{TPolicy}"/>, <see cref="OverrideRateLimiter{TLimiter}"/>,
/// <see cref="OverrideOutcomeCaseRegistry{TRegistry}"/>) replace this builder's own internal default
/// entry directly, so they win regardless of whether they are called before or after the corresponding
/// <c>Add*Behavior</c> call in the same chain.
/// </para>
/// <para>
/// <b>Per-request-type overrides (Tier 1) vs. the general-purpose baseline (Tier 2).</b>
/// <see cref="Behaviors"/> is the general-purpose baseline every request type draws from unless
/// <see cref="OverridePipelineFor"/> declares an override rule matching it - a full, positively-declared
/// replacement of the baseline for matched requests, never a merge with or modification of it. A full
/// replacement sidesteps ever needing to identify and negate an individual behavior from an ambient list -
/// there is only ever something to register, never something to remove.
/// </para>
/// </remarks>
public sealed class InterlocutorPipelineBuilder
{
    private static readonly BehaviorDescriptor LoggingBehaviorDescriptor = new(typeof(LoggingBehavior<,>), []);

    private static readonly BehaviorDescriptor AuditingBehaviorDescriptor = new(
        typeof(AuditingBehavior<,>),
        [(typeof(IAuditSink), typeof(LoggingAuditSink))]
    );

    private static readonly BehaviorDescriptor AuthorizationBehaviorDescriptor = new(
        typeof(AuthorizationBehavior<,>),
        [
            (typeof(IPolicyEvaluator), typeof(DefaultPolicyEvaluator)),
            (typeof(IOutcomeCaseRegistry), typeof(DefaultOutcomeCaseRegistry)),
        ]
    );

    private static readonly BehaviorDescriptor CancellationBehaviorDescriptor = new(
        typeof(CancellationBehavior<,>),
        [(typeof(IOutcomeCaseRegistry), typeof(DefaultOutcomeCaseRegistry))]
    );

    private static readonly BehaviorDescriptor TimeoutBehaviorDescriptor = new(
        typeof(TimeoutBehavior<,>),
        [
            (typeof(ITimeoutPolicy), typeof(DefaultTimeoutPolicy)),
            (typeof(IOutcomeCaseRegistry), typeof(DefaultOutcomeCaseRegistry)),
        ]
    );

    private static readonly BehaviorDescriptor RateLimitedBehaviorDescriptor = new(
        typeof(RateLimitedBehavior<,>),
        [(typeof(IRateLimiter), typeof(DefaultRateLimiter)), (typeof(IOutcomeCaseRegistry), typeof(DefaultOutcomeCaseRegistry))]
    );

    private readonly List<InterlocutorFactoryRegistration> _behaviorFactories = [];
    private readonly List<InterlocutorRegistration> _behaviors = [];
    private readonly Dictionary<Type, InterlocutorRegistration> _defaults;
    private readonly HashSet<Type> _overrideBehaviorTypesRegistered = [];
    private readonly List<PipelineOverrideRule> _overrideRules = [];
    private readonly List<SkippedRegistration> _skipped = [];
    private readonly List<InterlocutorRegistration> _stages = [];
    private readonly List<InterlocutorFactoryRegistration> _stageFactories = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="InterlocutorPipelineBuilder"/> class, standalone,
    /// with its own empty defaults catalog.
    /// </summary>
    public InterlocutorPipelineBuilder()
        : this([]) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterlocutorPipelineBuilder"/> class sharing the
    /// given defaults dictionary - the constructor <see cref="InterlocutorBuilder"/> uses so that a
    /// pipeline default (for example, the <see cref="IAuditSink"/> entry <see cref="AddAuditingBehavior"/>
    /// try-adds) and an infrastructure override that targets the same service type
    /// (for example, <see cref="OverrideAuditSink{TSink}"/>) are always the same
    /// dictionary entry, regardless of which type's method runs first.
    /// </summary>
    /// <param name="sharedDefaults">The defaults dictionary to read from and write to.</param>
    internal InterlocutorPipelineBuilder(Dictionary<Type, InterlocutorRegistration> sharedDefaults)
    {
        this._defaults = sharedDefaults;
    }

    /// <summary>
    /// Gets the flat, ordered list of pipeline behavior registrations, in builder-call order. Empty on
    /// a bare builder. May contain the same implementation type more than once.
    /// </summary>
    /// <value>The flat, ordered list of pipeline behavior registrations, in builder-call order.</value>
    public IReadOnlyList<InterlocutorRegistration> Behaviors => this._behaviors;

    /// <summary>
    /// Gets the delegate-backed pipeline behavior registrations added via the <c>AddBehavior</c> delegate
    /// overloads - the lambda-form counterpart to <see cref="Behaviors"/>.
    /// </summary>
    /// <value>The delegate-backed pipeline behavior registrations.</value>
    public IReadOnlyList<InterlocutorFactoryRegistration> BehaviorFactories => this._behaviorFactories;

    /// <summary>
    /// Gets every Tier-1 pipeline override rule added via <see cref="OverridePipelineFor"/>, in call order.
    /// </summary>
    /// <value>Every pipeline override rule added so far.</value>
    public IReadOnlyList<PipelineOverrideRule> OverrideRules => this._overrideRules;

    /// <summary>
    /// Gets the configuration <see cref="AddPipelineExplainer"/> was called with, or <see langword="null"/>
    /// if it was never called - the signal both <c>Interlocutor.DependencyInjection.Microsoft</c> and
    /// <c>Interlocutor.DependencyInjection.Autofac</c> use to decide whether to register
    /// <c>IPipelineExplainer</c> at all. <c>IPipelineExplainer</c> is opt-in at the registration level (this
    /// property stays <see langword="null"/> unless <see cref="AddPipelineExplainer"/> is called) as well as
    /// at the <see cref="Interlocutor.Abstractions.Pipeline.Diagnostics.PipelineExplainerOptions.Enabled"/> level.
    /// </summary>
    /// <value>The configuration <see cref="AddPipelineExplainer"/> was called with, or <see langword="null"/>.</value>
    public PipelineExplainerOptions? PipelineExplainerConfiguration { get; private set; }

    /// <summary>
    /// Gets the try-add-able default registrations this builder has set, one per service type. When
    /// constructed standalone, this is exactly the set of pipeline-behavior dependency defaults added by
    /// this builder's own calls. When constructed as part of an <see cref="InterlocutorBuilder"/>, this
    /// is the same shared dictionary <see cref="InterlocutorBuilder.Defaults"/> exposes.
    /// </summary>
    /// <value>The try-add-able default registrations this builder has set.</value>
    public IReadOnlyCollection<InterlocutorRegistration> Defaults => this._defaults.Values;

    /// <summary>
    /// Gets the pipeline stage registrations added via <see cref="AddStage(Type, RegistrationLifetime)"/>,
    /// each keyed to the closed <see cref="IPipelineStage{TRequest,TResponse}"/> interface it implements.
    /// </summary>
    /// <value>The pipeline stage registrations added via <see cref="AddStage(Type, RegistrationLifetime)"/>.</value>
    public IReadOnlyList<InterlocutorRegistration> Stages => this._stages;

    /// <summary>
    /// Gets the delegate-backed pipeline stage registrations added via the <c>AddStage</c> delegate
    /// overloads - the lambda-form counterpart to <see cref="Stages"/>.
    /// </summary>
    /// <value>The delegate-backed pipeline stage registrations.</value>
    public IReadOnlyList<InterlocutorFactoryRegistration> StageFactories => this._stageFactories;

    /// <summary>
    /// Gets every registration this builder deliberately did not make, and why - currently only the
    /// silent optional-package probes behind <see cref="AddValidationBehavior"/> and
    /// <see cref="AddUnitOfWorkBehavior"/> when the corresponding package is not referenced.
    /// </summary>
    /// <value>Every registration this builder deliberately did not make, and why.</value>
    public IReadOnlyList<SkippedRegistration> Skipped => this._skipped;

    /// <summary>
    /// Adds a pipeline behavior implementation at the current position in the chain. This is the
    /// general escape hatch every named <c>Add*Behavior</c> convenience method is sugar over - use it
    /// directly for a custom or third-party <see cref="IPipelineBehavior{TRequest,TResponse}"/>
    /// implementation this builder has no dedicated method for.
    /// </summary>
    /// <param name="behaviorImplementationType">
    /// The behavior's implementation type - typically an open generic type definition (for example
    /// <c>typeof(MyBehavior&lt;,&gt;)</c>) so it applies across request/response shapes, registered
    /// against the open <see cref="IPipelineBehavior{TRequest,TResponse}"/> contract.
    /// </param>
    /// <param name="lifetime">The service lifetime to register the behavior with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddBehavior(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type behaviorImplementationType,
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
    {
        ArgumentNullException.ThrowIfNull(behaviorImplementationType);
        this._behaviors.Add(
            new InterlocutorRegistration(
                typeof(IPipelineBehavior<,>),
                behaviorImplementationType,
                lifetime,
                RegistrationKind.Behavior
            )
        );
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior as a pair of enter/exit delegates instead of a dedicated implementation
    /// type - sugar over <see cref="DelegatePipelineBehavior{TRequest,TResponse}"/> for a behavior simple
    /// enough not to need one. Either delegate may be omitted for a no-op hook. Legal alongside
    /// <see cref="AddBehavior(Type, RegistrationLifetime)"/>; entries from both are resolved together
    /// against the closed <see cref="IPipelineBehavior{TRequest,TResponse}"/> contract they both target.
    /// </summary>
    /// <typeparam name="TRequest">The request type the behavior participates in.</typeparam>
    /// <typeparam name="TResponse">The response type the behavior participates in.</typeparam>
    /// <param name="onEnter">
    /// The enter-hook delegate backing <see cref="PipelineBehaviorBase{TRequest,TResponse}.OnEnterAsync"/>,
    /// or <see langword="null"/> for a no-op (always-continue) enter-hook.
    /// </param>
    /// <param name="onExit">
    /// The exit-hook delegate backing <see cref="PipelineBehaviorBase{TRequest,TResponse}.OnExitAsync"/>, or
    /// <see langword="null"/> for a no-op (pass-through) exit-hook.
    /// </param>
    /// <param name="lifetime">
    /// The service lifetime to register the behavior with. Defaults to singleton - the delegates already
    /// capture whatever state they need.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddBehavior<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<StageResult<TResponse>>>? onEnter = null,
        Func<TRequest, TResponse, CancellationToken, Task<TResponse>>? onExit = null,
        RegistrationLifetime lifetime = RegistrationLifetime.Singleton
    )
    {
        this._behaviorFactories.Add(
            new InterlocutorFactoryRegistration(
                typeof(IPipelineBehavior<TRequest, TResponse>),
                _ => new DelegatePipelineBehavior<TRequest, TResponse>(onEnter, onExit),
                lifetime,
                RegistrationKind.Behavior
            )
        );
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior as a pair of enter/exit delegates that receive the container's
    /// <see cref="IServiceProvider"/> - the overload for lambda-form behaviors that need to resolve services
    /// (for example, a scoped dependency) rather than capturing them at builder-configuration time. See
    /// <see cref="AddBehavior{TRequest,TResponse}(Func{TRequest,CancellationToken,Task{StageResult{TResponse}}},Func{TRequest,TResponse,CancellationToken,Task{TResponse}},RegistrationLifetime)"/>
    /// for the simpler, service-free overload.
    /// </summary>
    /// <typeparam name="TRequest">The request type the behavior participates in.</typeparam>
    /// <typeparam name="TResponse">The response type the behavior participates in.</typeparam>
    /// <param name="onEnter">
    /// The enter-hook delegate, receiving the resolving <see cref="IServiceProvider"/> first, or
    /// <see langword="null"/> for a no-op (always-continue) enter-hook.
    /// </param>
    /// <param name="onExit">
    /// The exit-hook delegate, receiving the resolving <see cref="IServiceProvider"/> first, or
    /// <see langword="null"/> for a no-op (pass-through) exit-hook.
    /// </param>
    /// <param name="lifetime">
    /// The service lifetime to register the behavior with. Defaults to scoped, since a service-resolving
    /// behavior most often wants scoped dependencies resolved fresh per dispatch.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddBehavior<TRequest, TResponse>(
        Func<IServiceProvider, TRequest, CancellationToken, Task<StageResult<TResponse>>>? onEnter,
        Func<IServiceProvider, TRequest, TResponse, CancellationToken, Task<TResponse>>? onExit,
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
    {
        this._behaviorFactories.Add(
            new InterlocutorFactoryRegistration(
                typeof(IPipelineBehavior<TRequest, TResponse>),
                sp => new DelegatePipelineBehavior<TRequest, TResponse>(
                    onEnter is null ? null : (request, ct) => onEnter(sp, request, ct),
                    onExit is null ? null : (request, response, ct) => onExit(sp, request, response, ct)
                ),
                lifetime,
                RegistrationKind.Behavior
            )
        );
        return this;
    }

    /// <summary>
    /// Adds <typeparamref name="TBehavior"/> to the chain wrapped in
    /// <see cref="ConditionalBehavior{TRequest,TResponse}"/>, so it only runs when <paramref name="shouldRun"/>
    /// returns <see langword="true"/> for the dispatched request - otherwise the chain proceeds straight to
    /// <c>next()</c>, as if <typeparamref name="TBehavior"/> had never been registered. Lets a new or risky
    /// behavior (a stricter validation rule, a new authorization policy) be turned on for a fraction of
    /// traffic and ramped up without a redeploy, instead of the all-or-nothing choice every other
    /// <c>Add*Behavior</c> call makes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built from the same primitives as the service-resolving
    /// <see cref="AddBehavior{TRequest,TResponse}(Func{IServiceProvider,TRequest,CancellationToken,Task{StageResult{TResponse}}},Func{IServiceProvider,TRequest,TResponse,CancellationToken,Task{TResponse}},RegistrationLifetime)"/>
    /// overload: this method is a factory-form registration under the hood, so it lands in
    /// <see cref="BehaviorFactories"/> rather than <see cref="Behaviors"/> and, per that property's own
    /// remarks, resolves after every type-based <see cref="Behaviors"/> entry regardless of interleaved call
    /// order.
    /// </para>
    /// <para>
    /// <typeparamref name="TBehavior"/> is resolved from the container as the wrapper's inner behavior via a
    /// try-add default registration of <typeparamref name="TBehavior"/> against its own concrete type - the
    /// same "a consumer's own registration wins" precedent every other pipeline-behavior dependency default
    /// in <see cref="Defaults"/> already carries. <typeparamref name="TBehavior"/> is registered scoped by
    /// default, the same as <see cref="AddBehavior(Type, RegistrationLifetime)"/>; <paramref name="lifetime"/>
    /// governs the <see cref="ConditionalBehavior{TRequest,TResponse}"/> wrapper itself, not
    /// <typeparamref name="TBehavior"/>'s own registration. Because <see cref="ConditionalBehavior{TRequest,TResponse}"/>
    /// wraps an already-constructed inner instance rather than a per-request factory, <typeparamref name="TBehavior"/>
    /// is constructed whenever the wrapper itself is - once per resolution of the chain, the same as every
    /// other registered behavior - not lazily deferred until <paramref name="shouldRun"/> is evaluated against
    /// a specific request. <paramref name="shouldRun"/> only gates whether the already-constructed
    /// <typeparamref name="TBehavior"/> instance's <c>HandleAsync</c> runs for a given dispatch, not whether it
    /// gets constructed at all.
    /// </para>
    /// <para>
    /// Ships one built-in gate, a percentage rollout - see <see cref="RolloutOptions"/> and the
    /// Microsoft-DI/Autofac-specific <c>AddRolloutBehavior</c> sugar each container package builds on top of
    /// this method - but <paramref name="shouldRun"/> accepts any predicate, so a consumer can gate on a
    /// feature flag, a tenant allowlist, or anything else a runtime decision can be made from.
    /// </para>
    /// </remarks>
    /// <typeparam name="TBehavior">The concrete <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementation to gate.</typeparam>
    /// <typeparam name="TRequest">The request type the behavior participates in.</typeparam>
    /// <typeparam name="TResponse">The response type the behavior participates in.</typeparam>
    /// <param name="shouldRun">
    /// Evaluated once per dispatch, given the resolving <see cref="IServiceProvider"/> and the request
    /// instance - <see langword="true"/> runs <typeparamref name="TBehavior"/>, <see langword="false"/> skips
    /// straight to <c>next()</c>.
    /// </param>
    /// <param name="lifetime">
    /// The service lifetime to register the <see cref="ConditionalBehavior{TRequest,TResponse}"/> wrapper
    /// with. Defaults to scoped.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddConditionalBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TBehavior,
        TRequest,
        TResponse
    >(Func<IServiceProvider, TRequest, bool> shouldRun, RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        ArgumentNullException.ThrowIfNull(shouldRun);

        this.TryAddDefault(typeof(TBehavior), typeof(TBehavior));
        this._behaviorFactories.Add(
            new InterlocutorFactoryRegistration(
                typeof(IPipelineBehavior<TRequest, TResponse>),
                sp => new ConditionalBehavior<TRequest, TResponse>(
                    (IPipelineBehavior<TRequest, TResponse>)(
                        sp.GetService(typeof(TBehavior))
                        ?? throw new InvalidOperationException(
                            $"'{typeof(TBehavior)}' could not be resolved from the container. "
                                + $"AddConditionalBehavior<{typeof(TBehavior).Name},...> registers it as a "
                                + "try-add default against its own concrete type - check that its constructor "
                                + "dependencies are themselves resolvable."
                        )
                    ),
                    request => shouldRun(sp, request)
                ),
                lifetime,
                RegistrationKind.Behavior
            )
        );

        return this;
    }

    /// <summary>
    /// Declares a Tier-1 pipeline override: a request type <paramref name="predicate"/> matches gets exactly
    /// <paramref name="behaviors"/>, in the given order, in place of the general-purpose baseline (Tier 2)
    /// entirely - a full, positively-declared replacement, never a merge with or modification of it. An
    /// unmatched request's resolved pipeline is unaffected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every entry in <paramref name="behaviors"/> is also registered exactly as an ordinary
    /// <see cref="AddBehavior"/> call would register it (deduplicated against a behavior type already added
    /// by an earlier <see cref="OverridePipelineFor"/> call, so declaring the same behavior type in more than
    /// one override rule does not register it twice), so it is resolvable at dispatch time. There is
    /// deliberately no complementary "skip"/"exclude" primitive - declaring an override is the only action a
    /// consumer takes.
    /// </para>
    /// <para>
    /// A request matched by this rule that also structurally satisfies an existing marker interface
    /// (<c>IAuditableRequest</c>, <c>IRequiresPolicyRequest</c>, etc.) does not run that marker-constrained
    /// behavior unless <paramref name="behaviors"/> names it explicitly - the matched request's resolved
    /// pipeline is exactly and only <paramref name="behaviors"/>, enforced once, centrally, at the point
    /// each dispatch path builds its behavior chain (see <c>Interlocutor.Abstractions.Pipeline.PipelineChainResolver</c>),
    /// not by asking every behavior to check whether it should yield to an override.
    /// </para>
    /// <para>
    /// Two rules whose predicates both match the same request type are a build-validation error - see
    /// <see cref="RegistrationProblemKind.ConflictingPipelineOverride"/> - not a silent resolution. There is
    /// no precedence-declaration escape hatch; narrow one rule's <paramref name="predicate"/> instead.
    /// </para>
    /// </remarks>
    /// <param name="predicate">Tested against a request's runtime type at dispatch time.</param>
    /// <param name="name">
    /// A label identifying this rule, used in <see cref="RegistrationProblemKind.ConflictingPipelineOverride"/>
    /// messages to name which rules conflict.
    /// </param>
    /// <param name="behaviors">
    /// The complete, explicitly-ordered behavior list a matched request resolves instead of the
    /// general-purpose baseline. Must not be empty.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:DynamicallyAccessedMembers",
        Justification = "Each entry in behaviors is expected to be a compile-time typeof(...) token from the "
            + "caller, the same shape AddBehavior's own DynamicallyAccessedMembers-annotated parameter expects "
            + "directly - IReadOnlyList<Type> cannot itself carry that per-element annotation, so it is "
            + "re-asserted here rather than on the collection type. See documentation/trim-and-aot-support.md."
    )]
    public InterlocutorPipelineBuilder OverridePipelineFor(
        Func<Type, bool> predicate,
        string name,
        IReadOnlyList<Type> behaviors
    )
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(behaviors);
        if (behaviors.Count == 0)
        {
            throw new ArgumentException("An override pipeline must declare at least one behavior.", nameof(behaviors));
        }

        this._overrideRules.Add(new PipelineOverrideRule(predicate, name, behaviors));
        foreach (var behaviorType in behaviors.Where(behaviorType => this._overrideBehaviorTypesRegistered.Add(behaviorType)))
        {
            this.AddBehavior(behaviorType);
        }

        return this;
    }

    /// <summary>
    /// Declares a Tier-1 pipeline override for exactly <typeparamref name="TRequest"/> - the convenience
    /// overload of <see cref="OverridePipelineFor(Func{Type,bool},string,IReadOnlyList{Type})"/> for the
    /// common single-type case. See that overload for the full override semantics.
    /// </summary>
    /// <typeparam name="TRequest">The request type this override applies to.</typeparam>
    /// <param name="name">
    /// A label identifying this rule, used in <see cref="RegistrationProblemKind.ConflictingPipelineOverride"/>
    /// messages to name which rules conflict.
    /// </param>
    /// <param name="behaviors">
    /// The complete, explicitly-ordered behavior list <typeparamref name="TRequest"/> resolves instead of
    /// the general-purpose baseline. Must not be empty.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder OverridePipelineFor<TRequest>(string name, params Type[] behaviors) =>
        this.OverridePipelineFor(t => t == typeof(TRequest), name, behaviors);

    /// <summary>
    /// Adds <see cref="LoggingBehavior{TRequest,TResponse}"/> at the current position in the chain.
    /// </summary>
    /// <remarks>
    /// No <see cref="IResponseOutcomeDescriber"/> default is registered here: <see cref="LoggingBehavior{TRequest,TResponse}"/>
    /// takes it as an <em>optional</em> constructor dependency and already falls back to a describer
    /// built from <see cref="Interlocutor.Abstractions.Pipeline.Classifiers.OutcomeClassifierBuilder.DefaultClassifier"/>
    /// when nothing is registered - registering a default here would additionally require a default
    /// <c>IOutcomeClassifier</c> registration to keep it resolvable, which is exactly the kind of orphan
    /// dependency ADR-0009 asks the builder not to introduce. Call <see cref="OverrideOutcomeDescriber{TDescriber}"/>
    /// to opt into a specific implementation instead.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddLoggingBehavior() => this.AddFromDescriptor(LoggingBehaviorDescriptor);

    /// <summary>
    /// Adds <see cref="AuditingBehavior{TRequest,TResponse}"/> at the current position in the chain, and
    /// try-adds its default dependency, <see cref="IAuditSink"/> (<see cref="LoggingAuditSink"/>), unless
    /// a default has already been set. See <see cref="AddLoggingBehavior"/> for why no
    /// <see cref="IResponseOutcomeDescriber"/> default is registered here either.
    /// </summary>
    /// <remarks>
    /// Not part of <see cref="UseRecommendedChain"/>'s default composition - where an audit trail should
    /// go is an application-specific decision, so add this explicitly when your application wants one.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddAuditingBehavior() => this.AddFromDescriptor(AuditingBehaviorDescriptor);

    /// <summary>
    /// Adds <see cref="AuthorizationBehavior{TRequest,TResponse}"/> at the current position in the chain,
    /// and try-adds its default dependencies, <see cref="IPolicyEvaluator"/> (<see cref="DefaultPolicyEvaluator"/>)
    /// and <see cref="IOutcomeCaseRegistry"/> (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default
    /// has already been set for either.
    /// </summary>
    /// <remarks>
    /// Not part of <see cref="UseRecommendedChain"/>'s default composition - most consumers already
    /// enforce authorization earlier in the request path (ASP.NET Core's own authentication/authorization
    /// middleware, or an upstream policy engine, running before the mediator is ever invoked), which makes
    /// a second in-pipeline pass redundant. Add this explicitly when the mediator itself is the first
    /// place authorization can be enforced - a message-bus consumer, a background worker, a scheduled
    /// job, or anything else dispatching a request with nothing HTTP-shaped in front of it - or when you
    /// want defense-in-depth via a fine-grained, resource-level check that upstream middleware can't see,
    /// even alongside upstream authorization.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddAuthorizationBehavior() => this.AddFromDescriptor(AuthorizationBehaviorDescriptor);

    /// <summary>
    /// Adds <c>Interlocutor.FluentValidation.ValidationBehavior&lt;,&gt;</c> at the current position in
    /// the chain, if the <c>Interlocutor.FluentValidation</c> package is referenced by the application,
    /// and try-adds its default dependency, <see cref="IOutcomeCaseRegistry"/>
    /// (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default has already been set. A no-op when that
    /// package is not loaded.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:DynamicallyAccessedMembers",
        Justification = "The probed type is Interlocutor.FluentValidation's own ValidationBehavior<,> - found "
            + "by name only to detect whether the peer package is referenced at all, never to discover a type "
            + "unknown to the application. A consuming app that references Interlocutor.FluentValidation "
            + "already uses FluentValidation's own IValidator<T> types ordinarily elsewhere, which is what "
            + "keeps this behavior's own members reachable independent of this reflective lookup."
    )]
    public InterlocutorPipelineBuilder AddValidationBehavior()
    {
        var validationBehaviorType = TryGetType(
            "Interlocutor.FluentValidation.ValidationBehavior`2, Interlocutor.FluentValidation"
        );
        if (validationBehaviorType is not null)
        {
            this.AddBehavior(validationBehaviorType);
            this.TryAddDefault(typeof(IOutcomeCaseRegistry), typeof(DefaultOutcomeCaseRegistry));
        }
        else
        {
            this._skipped.Add(
                new SkippedRegistration(
                    "Interlocutor.FluentValidation.ValidationBehavior<,>",
                    "Interlocutor.FluentValidation package is not referenced by the application"
                )
            );
        }

        return this;
    }

    /// <summary>
    /// Adds the EF Core or NHibernate <c>UnitOfWorkBehavior</c> - the unit-of-work-owned transaction
    /// commit/rollback gate - at the current position in the chain, preferring EF Core, if either
    /// store-specific persistence package is referenced by the application. A no-op when neither package is
    /// loaded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This behavior applies to every request once registered - no marker is required to participate, unlike
    /// the retired <c>TransactionBehavior</c> it supersedes. It opens a transaction lazily the first time
    /// anything writes (or eagerly, at pipeline start, for a request implementing <c>ITransactionalRequest</c>)
    /// and commits or rolls it back automatically based on the outcome classifier's verdict.
    /// </para>
    /// <para>
    /// EF Core's <c>UnitOfWorkBehavior&lt;TRequest,TResponse,TContext&gt;</c> is generic over the
    /// application's own <c>DbContext</c> type (<c>TContext</c>), unlike NHibernate's two-parameter
    /// equivalent. Under Microsoft DI, this method's plain, un-closed registration of that type only works
    /// when the application references NHibernate instead - Microsoft.Extensions.DependencyInjection
    /// requires an open generic implementation to declare exactly as many open type parameters as the
    /// service interface it's paired with, and cannot leave <c>TContext</c> open behind the two-parameter
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/>. Calling this method under Microsoft DI once EF
    /// Core is referenced throws immediately, with an actionable message pointing here (see
    /// <c>ServiceCollectionExtensions.ResolveServiceType</c>'s exception documentation).
    /// </para>
    /// <para>
    /// Autofac has no such restriction and works with this method unchanged. Under Microsoft DI with EF
    /// Core, instead define a small subclass that closes <c>TContext</c> over the application's own
    /// <c>DbContext</c> type while leaving <c>TRequest</c>/<c>TResponse</c> open - for example
    /// <c>class MyUnitOfWorkBehavior&lt;TRequest,TResponse&gt; : UnitOfWorkBehavior&lt;TRequest,TResponse,MyDbContext&gt;</c>
    /// - and register that subclass via <see cref="AddBehavior"/> instead of calling this method.
    /// </para>
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:DynamicallyAccessedMembers",
        Justification = "The probed type is Interlocutor.Persistence.EntityFrameworkCore's or "
            + "Interlocutor.Persistence.NHibernate's own UnitOfWorkBehavior - found by name only to detect "
            + "whether the peer package is referenced at all, never to discover a type unknown to the "
            + "application. A consuming app that references either package already uses its DbContext/ISession "
            + "types ordinarily elsewhere, which is what keeps this behavior's own members reachable "
            + "independent of this reflective lookup."
    )]
    public InterlocutorPipelineBuilder AddUnitOfWorkBehavior()
    {
        // EF Core's UnitOfWorkBehavior<TRequest,TResponse,TContext> is arity 3 (generic over the DbContext
        // type too); NHibernate's UnitOfWorkBehavior<TRequest,TResponse> is arity 2 (ISession isn't generic).
        // NHibernate is only probed - and only reported as skipped - when EF Core isn't found, preserving
        // the original short-circuit: a project referencing neither is silently skipped for both, one
        // referencing only NHibernate is skipped for EF Core alone.
        var efCoreType = TryGetType(
            "Interlocutor.Persistence.EntityFrameworkCore.UnitOfWorkBehavior`3, Interlocutor.Persistence.EntityFrameworkCore"
        );
        Type? nHibernateType = null;
        if (efCoreType is null)
        {
            this._skipped.Add(
                new SkippedRegistration(
                    "Interlocutor.Persistence.EntityFrameworkCore.UnitOfWorkBehavior<,,>",
                    "Interlocutor.Persistence.EntityFrameworkCore package is not referenced by the application"
                )
            );

            nHibernateType = TryGetType(
                "Interlocutor.Persistence.NHibernate.UnitOfWorkBehavior`2, Interlocutor.Persistence.NHibernate"
            );
            if (nHibernateType is null)
            {
                this._skipped.Add(
                    new SkippedRegistration(
                        "Interlocutor.Persistence.NHibernate.UnitOfWorkBehavior<,>",
                        "Interlocutor.Persistence.NHibernate package is not referenced by the application"
                    )
                );
            }
        }

        var unitOfWorkBehaviorType = efCoreType ?? nHibernateType;
        if (unitOfWorkBehaviorType is not null)
        {
            this.AddBehavior(unitOfWorkBehaviorType);
        }

        return this;
    }

    /// <summary>
    /// Adds <see cref="CancellationBehavior{TRequest,TResponse}"/> at the current position in the chain,
    /// and try-adds its default dependency, <see cref="IOutcomeCaseRegistry"/>
    /// (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default has already been set.
    /// </summary>
    /// <remarks>
    /// This behavior has no prescribed position in the pipeline - unlike the behaviors composed by
    /// <see cref="UseRecommendedChain"/>, it is not part of that documented ordering. The caller chooses
    /// where it lands in their own chain by where they call this method relative to their other
    /// <c>Add*Behavior</c> calls.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddCancellationBehavior() => this.AddFromDescriptor(CancellationBehaviorDescriptor);

    /// <summary>
    /// Adds <see cref="TimeoutBehavior{TRequest,TResponse}"/> at the current position in the chain, and
    /// try-adds its default dependencies, <see cref="ITimeoutPolicy"/> (<see cref="DefaultTimeoutPolicy"/>)
    /// and <see cref="IOutcomeCaseRegistry"/> (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default
    /// has already been set for either.
    /// </summary>
    /// <remarks>
    /// This behavior has no prescribed position in the pipeline - unlike the behaviors composed by
    /// <see cref="UseRecommendedChain"/>, it is not part of that documented ordering. The caller chooses
    /// where it lands in their own chain by where they call this method relative to their other
    /// <c>Add*Behavior</c> calls.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddTimeoutBehavior() => this.AddFromDescriptor(TimeoutBehaviorDescriptor);

    /// <summary>
    /// Adds <see cref="RateLimitedBehavior{TRequest,TResponse}"/> at the current position in the chain,
    /// and try-adds its default dependencies, <see cref="IRateLimiter"/> (<see cref="DefaultRateLimiter"/>)
    /// and <see cref="IOutcomeCaseRegistry"/> (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default
    /// has already been set for either.
    /// </summary>
    /// <remarks>
    /// This behavior has no prescribed position in the pipeline - unlike the behaviors composed by
    /// <see cref="UseRecommendedChain"/>, it is not part of that documented ordering. The caller chooses
    /// where it lands in their own chain by where they call this method relative to their other
    /// <c>Add*Behavior</c> calls.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddRateLimitedBehavior() => this.AddFromDescriptor(RateLimitedBehaviorDescriptor);

    /// <summary>
    /// Registers the diagnostic <see cref="IPipelineExplainer"/> - the runtime counterpart to
    /// <see cref="InterlocutorBuilder.DescribeRegistrations"/> that answers which pipeline behaviors resolve
    /// for a request/response type pair known only at run time (typically from a bug report or a
    /// configuration file, not a compile-time generic argument).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IPipelineExplainer"/> is opt-in at two independent levels: it is registered at all only
    /// when this method is called (<see cref="PipelineExplainerConfiguration"/> stays <see langword="null"/>
    /// otherwise, and neither container translator registers it), and even then it stays disabled - calling
    /// <see cref="IPipelineExplainer.Explain"/> throws - until <see cref="PipelineExplainerOptions.Enabled"/>
    /// is <see langword="true"/>. This double gate reflects that the explainer resolves live instances from
    /// the container and reveals internal wiring, which is not something to run unexamined against a
    /// production container.
    /// </para>
    /// <para>
    /// Under Microsoft DI, an application registration of <c>IOptions&lt;PipelineExplainerOptions&gt;</c> or
    /// <c>IOptionsMonitor&lt;PipelineExplainerOptions&gt;</c> - typically via
    /// <c>services.Configure&lt;PipelineExplainerOptions&gt;(configuration.GetSection(...))</c> - wins over
    /// <paramref name="options"/>, consistent with how <see cref="Defaults"/> entries already let a
    /// consumer's own registration win. Autofac has no such precedence layer: it always registers
    /// <paramref name="options"/> (or a fresh, disabled instance when <paramref name="options"/> is
    /// <see langword="null"/>) directly, with no <c>IOptions</c> dependency at all.
    /// </para>
    /// </remarks>
    /// <param name="options">
    /// The explainer's configuration, or <see langword="null"/> to register it with a fresh, disabled
    /// <see cref="PipelineExplainerOptions"/> (<see cref="PipelineExplainerOptions.Enabled"/> defaults to
    /// <see langword="false"/>) until a consumer enables it - by passing an instance with
    /// <see langword="true"/> here, or, under Microsoft DI, via an <c>IOptions</c>/<c>IOptionsMonitor</c>
    /// registration that wins over this value.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddPipelineExplainer(PipelineExplainerOptions? options = null)
    {
        this.PipelineExplainerConfiguration = options ?? new PipelineExplainerOptions();
        return this;
    }

    /// <summary>
    /// Applies the library's documented recommended pipeline chain - logging, then validation, then
    /// transaction - in that order, as sugar over the individual <c>Add*Behavior</c> calls. Never applied
    /// automatically; a consumer opts in explicitly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a deliberately minimal default: <see cref="AddAuditingBehavior"/> and
    /// <see cref="AddAuthorizationBehavior"/> are not part of it. Most consumers don't want an audit trail
    /// at all, and most who need authorization already enforce it earlier in the request path - ASP.NET
    /// Core's own authentication/authorization middleware, or an upstream policy engine, running before
    /// the mediator is ever invoked - which makes a second in-pipeline pass redundant, or even
    /// conflicting. See <see cref="AddAuditingBehavior"/>'s and <see cref="AddAuthorizationBehavior"/>'s
    /// own remarks for when it's still worth adding either back explicitly.
    /// </para>
    /// <para>
    /// The order below still applies to whichever of them you add back, documented here because a
    /// developer composing their own chain needs the reasoning, not just the sequence:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Logging outermost so every request's timing and outcome is captured regardless of what happens inside.</description></item>
    /// <item><description>Auditing, if added, outside authorization so a request that gets denied is still recorded - a rejected attempt is itself an auditable event.</description></item>
    /// <item><description>Authorization, if added, before validation so an unauthorized caller never learns validation details about a request they cannot perform.</description></item>
    /// <item><description>Validation before transaction so malformed input never opens a transaction that would only be rolled back.</description></item>
    /// <item><description>Transaction innermost so only the handler's own work - not cross-cutting concerns - participates in the unit of work.</description></item>
    /// </list>
    /// <para>
    /// To restore the full chain this method composed before auditing/authorization were dropped from the
    /// default, compose every step individually rather than calling this method and appending
    /// <see cref="AddAuditingBehavior"/>/<see cref="AddAuthorizationBehavior"/> afterward - appending after
    /// the fact puts both innermost, past validation and the transaction, not in their documented position:
    /// <c>.AddLoggingBehavior().AddAuditingBehavior().AddAuthorizationBehavior().AddValidationBehavior().AddUnitOfWorkBehavior()</c>.
    /// </para>
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder UseRecommendedChain() =>
        this.AddLoggingBehavior().AddValidationBehavior().AddUnitOfWorkBehavior();

    /// <summary>
    /// Replaces the default <see cref="IAuditSink"/> implementation <see cref="AuditingBehavior{TRequest,TResponse}"/>
    /// receives when no application-level registration is present. Wins regardless of whether it is
    /// called before or after <see cref="AddAuditingBehavior"/> in the same chain.
    /// </summary>
    /// <typeparam name="TSink">The replacement <see cref="IAuditSink"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder OverrideAuditSink<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TSink
    >(RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
        where TSink : class, IAuditSink
    {
        this.SetDefault(typeof(IAuditSink), typeof(TSink), lifetime);
        return this;
    }

    /// <summary>
    /// Replaces the default <see cref="IPolicyEvaluator"/> implementation <see cref="AuthorizationBehavior{TRequest,TResponse}"/>
    /// receives when no application-level registration is present. Wins regardless of whether it is
    /// called before or after <see cref="AddAuthorizationBehavior"/> in the same chain.
    /// </summary>
    /// <typeparam name="TEvaluator">The replacement <see cref="IPolicyEvaluator"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder OverridePolicyEvaluator<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvaluator
    >(RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
        where TEvaluator : class, IPolicyEvaluator
    {
        this.SetDefault(typeof(IPolicyEvaluator), typeof(TEvaluator), lifetime);
        return this;
    }

    /// <summary>
    /// Replaces the default <see cref="IResponseOutcomeDescriber"/> implementation
    /// <see cref="LoggingBehavior{TRequest,TResponse}"/> and <see cref="AuditingBehavior{TRequest,TResponse}"/>
    /// optionally consume when no application-level registration is present. Wins regardless of whether
    /// it is called before or after <see cref="AddLoggingBehavior"/> / <see cref="AddAuditingBehavior"/>
    /// in the same chain.
    /// </summary>
    /// <typeparam name="TDescriber">The replacement <see cref="IResponseOutcomeDescriber"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to singleton.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder OverrideOutcomeDescriber<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDescriber
    >(RegistrationLifetime lifetime = RegistrationLifetime.Singleton)
        where TDescriber : class, IResponseOutcomeDescriber
    {
        this.SetDefault(typeof(IResponseOutcomeDescriber), typeof(TDescriber), lifetime);
        return this;
    }

    /// <summary>
    /// Replaces the default <see cref="ITimeoutPolicy"/> implementation <see cref="TimeoutBehavior{TRequest,TResponse}"/>
    /// receives when no application-level registration is present. Wins regardless of whether it is
    /// called before or after <see cref="AddTimeoutBehavior"/> in the same chain.
    /// </summary>
    /// <typeparam name="TPolicy">The replacement <see cref="ITimeoutPolicy"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder OverrideTimeoutPolicy<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TPolicy
    >(RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
        where TPolicy : class, ITimeoutPolicy
    {
        this.SetDefault(typeof(ITimeoutPolicy), typeof(TPolicy), lifetime);
        return this;
    }

    /// <summary>
    /// Replaces the default <see cref="IRateLimiter"/> implementation <see cref="RateLimitedBehavior{TRequest,TResponse}"/>
    /// receives when no application-level registration is present. Wins regardless of whether it is
    /// called before or after <see cref="AddRateLimitedBehavior"/> in the same chain.
    /// </summary>
    /// <typeparam name="TLimiter">The replacement <see cref="IRateLimiter"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder OverrideRateLimiter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TLimiter
    >(RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
        where TLimiter : class, IRateLimiter
    {
        this.SetDefault(typeof(IRateLimiter), typeof(TLimiter), lifetime);
        return this;
    }

    /// <summary>
    /// Replaces the default <see cref="IOutcomeCaseRegistry"/> implementation
    /// <see cref="AuthorizationBehavior{TRequest,TResponse}"/>, <see cref="CancellationBehavior{TRequest,TResponse}"/>,
    /// <see cref="TimeoutBehavior{TRequest,TResponse}"/>, <see cref="RateLimitedBehavior{TRequest,TResponse}"/>,
    /// and the FluentValidation <c>ValidationBehavior</c> receive when no application-level registration is
    /// present. Wins regardless of whether it is called before or after the corresponding <c>Add*Behavior</c>
    /// call in the same chain.
    /// </summary>
    /// <typeparam name="TRegistry">The replacement <see cref="IOutcomeCaseRegistry"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder OverrideOutcomeCaseRegistry<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRegistry
    >(RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
        where TRegistry : class, IOutcomeCaseRegistry
    {
        this.SetDefault(typeof(IOutcomeCaseRegistry), typeof(TRegistry), lifetime);
        return this;
    }

    /// <summary>
    /// Registers a pipeline stage implementation directly, without assembly scanning. Reflects over the
    /// type's implemented interfaces and registers one <see cref="RegistrationKind.Stage"/> entry per
    /// closed <see cref="IPipelineStage{TRequest,TResponse}"/> interface it implements - mirroring how
    /// <see cref="HandlerDiscovery"/> registers a handler per closed contract interface.
    /// </summary>
    /// <param name="stageImplementationType">The concrete stage implementation type.</param>
    /// <param name="lifetime">The service lifetime to register the stage with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddStage(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type stageImplementationType,
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
    {
        ArgumentNullException.ThrowIfNull(stageImplementationType);

        foreach (var stageInterface in stageImplementationType.GetInterfaces())
        {
            if (stageInterface.IsGenericType && stageInterface.GetGenericTypeDefinition() == typeof(IPipelineStage<,>))
            {
                this._stages.Add(
                    new InterlocutorRegistration(stageInterface, stageImplementationType, lifetime, RegistrationKind.Stage)
                );
            }
        }

        return this;
    }

    /// <summary>
    /// Registers a pipeline stage implementation directly, without assembly scanning.
    /// See <see cref="AddStage(Type, RegistrationLifetime)"/>.
    /// </summary>
    /// <typeparam name="TStage">The concrete stage implementation type.</typeparam>
    /// <param name="lifetime">The service lifetime to register the stage with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddStage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TStage>(
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
        where TStage : class => this.AddStage(typeof(TStage), lifetime);

    /// <summary>
    /// Registers a pipeline stage as a pair of delegates instead of a dedicated implementation type -
    /// sugar over <see cref="DelegatePipelineStage{TRequest,TResponse}"/> for stages simple enough not to
    /// need one. Either delegate may be omitted for a no-op hook. Legal alongside
    /// <see cref="AddStage(Type, RegistrationLifetime)"/>; both add entries the stage host aggregates.
    /// </summary>
    /// <typeparam name="TRequest">The request type the stage participates in.</typeparam>
    /// <typeparam name="TResponse">The response type the stage participates in.</typeparam>
    /// <param name="onBefore">
    /// The before-hook delegate, or <see langword="null"/> for a no-op (always-continue) before-hook.
    /// </param>
    /// <param name="onAfter">The after-hook delegate, or <see langword="null"/> for a no-op after-hook.</param>
    /// <param name="lifetime">The service lifetime to register the stage with. Defaults to singleton - the delegates already capture whatever state they need.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddStage<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<StageResult<TResponse>>>? onBefore = null,
        Func<TRequest, TResponse, CancellationToken, Task>? onAfter = null,
        RegistrationLifetime lifetime = RegistrationLifetime.Singleton
    )
    {
        this._stageFactories.Add(
            new InterlocutorFactoryRegistration(
                typeof(IPipelineStage<TRequest, TResponse>),
                _ => new DelegatePipelineStage<TRequest, TResponse>(onBefore, onAfter),
                lifetime,
                RegistrationKind.Stage
            )
        );
        return this;
    }

    /// <summary>
    /// Registers a pipeline stage as a pair of delegates that receive the container's
    /// <see cref="IServiceProvider"/> - the overload for lambda-form stages that need to resolve services
    /// (for example, a scoped dependency) rather than capturing them at builder-configuration time. See
    /// <see cref="AddStage{TRequest,TResponse}(Func{TRequest,CancellationToken,Task{StageResult{TResponse}}},Func{TRequest,TResponse,CancellationToken,Task},RegistrationLifetime)"/>
    /// for the simpler, service-free overload.
    /// </summary>
    /// <typeparam name="TRequest">The request type the stage participates in.</typeparam>
    /// <typeparam name="TResponse">The response type the stage participates in.</typeparam>
    /// <param name="onBefore">
    /// The before-hook delegate, receiving the resolving <see cref="IServiceProvider"/> first, or
    /// <see langword="null"/> for a no-op (always-continue) before-hook.
    /// </param>
    /// <param name="onAfter">
    /// The after-hook delegate, receiving the resolving <see cref="IServiceProvider"/> first, or
    /// <see langword="null"/> for a no-op after-hook.
    /// </param>
    /// <param name="lifetime">
    /// The service lifetime to register the stage with. Defaults to scoped, since a service-resolving
    /// stage most often wants scoped dependencies resolved fresh per request.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddStage<TRequest, TResponse>(
        Func<IServiceProvider, TRequest, CancellationToken, Task<StageResult<TResponse>>>? onBefore,
        Func<IServiceProvider, TRequest, TResponse, CancellationToken, Task>? onAfter,
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
    {
        this._stageFactories.Add(
            new InterlocutorFactoryRegistration(
                typeof(IPipelineStage<TRequest, TResponse>),
                sp => new DelegatePipelineStage<TRequest, TResponse>(
                    onBefore is null ? null : (request, ct) => onBefore(sp, request, ct),
                    onAfter is null ? null : (request, response, ct) => onAfter(sp, request, response, ct)
                ),
                lifetime,
                RegistrationKind.Stage
            )
        );
        return this;
    }

    /// <summary>
    /// Adds <see cref="PipelineStageBehavior{TRequest,TResponse}"/> - the single host behavior that
    /// aggregates every registered <see cref="IPipelineStage{TRequest,TResponse}"/> - at the current
    /// position in the chain. An ordinary behavior entry: position it anywhere, and register it more
    /// than once if different stage groupings should sit at different depths.
    /// </summary>
    /// <param name="lifetime">The service lifetime to register the host with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddStageHost(RegistrationLifetime lifetime = RegistrationLifetime.Scoped) =>
        this.AddBehavior(typeof(PipelineStageBehavior<,>), lifetime);

    /// <summary>
    /// Applies a <see cref="BehaviorDescriptor"/>: adds its behavior type via <see cref="AddBehavior"/>,
    /// then try-adds each of its default dependency pairs via <see cref="TryAddDefault"/>, in order - the
    /// shared body every built-in <c>Add*Behavior</c> convenience method whose registration fits this
    /// (behavior type, defaults) shape delegates to.
    /// </summary>
    /// <param name="descriptor">The behavior registration to apply.</param>
    /// <returns>This builder, for chaining.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:DynamicallyAccessedMembers",
        Justification = "Every BehaviorDescriptor this method is called with is one of this type's own "
            + "private static readonly fields, built from compile-time typeof(...) tokens - the same shape "
            + "AddBehavior's and TryAddDefault's own DynamicallyAccessedMembers-annotated parameters expect "
            + "directly. The tuple-typed Defaults array cannot itself carry that per-element annotation, so "
            + "it is re-asserted here rather than on the descriptor's field types."
    )]
    private InterlocutorPipelineBuilder AddFromDescriptor(BehaviorDescriptor descriptor)
    {
        this.AddBehavior(descriptor.BehaviorType);
        foreach (var (serviceType, implementationType) in descriptor.Defaults)
        {
            this.TryAddDefault(serviceType, implementationType);
        }

        return this;
    }

    /// <summary>
    /// Sets or replaces this builder's own internal default entry for <paramref name="serviceType"/>,
    /// unconditionally - the mechanism behind every <c>Override*</c> method, guaranteeing it wins
    /// regardless of call order relative to the corresponding <c>Add*Behavior</c> call. Internal rather
    /// than private so <see cref="InterlocutorBuilder.OverrideRequestContextAccessor{TAccessor}"/> - the
    /// one override that stays on <see cref="InterlocutorBuilder"/> itself - writes through this same
    /// canonical path instead of keeping its own copy.
    /// </summary>
    /// <param name="serviceType">The service type to set or replace the default entry for.</param>
    /// <param name="implementationType">The implementation type to register as that default.</param>
    /// <param name="lifetime">The service lifetime to register the default with.</param>
    internal void SetDefault(
        Type serviceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type implementationType,
        RegistrationLifetime lifetime
    ) =>
        this._defaults[serviceType] = new InterlocutorRegistration(
            serviceType,
            implementationType,
            lifetime,
            RegistrationKind.Default
        );

    /// <summary>
    /// Sets this builder's own internal default entry for <paramref name="serviceType"/> only if no entry
    /// exists yet - the mechanism behind every <c>Add*Behavior</c> convenience method's default
    /// registration, so it never clobbers a prior <c>Override*</c> call.
    /// </summary>
    /// <param name="serviceType">The service type to set a default entry for, if none exists yet.</param>
    /// <param name="implementationType">The implementation type to register as that default.</param>
    private void TryAddDefault(
        Type serviceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type implementationType
    )
    {
        if (!this._defaults.ContainsKey(serviceType))
        {
            this._defaults[serviceType] = new InterlocutorRegistration(
                serviceType,
                implementationType,
                RegistrationLifetime.Scoped,
                RegistrationKind.Default
            );
        }
    }

    /// <summary>
    /// Attempts to resolve a type by its assembly-qualified or partially-qualified name, returning
    /// <see langword="null"/> if the type (or the assembly it lives in) is not loaded - the graceful-fallback
    /// lookup used for optional-package behaviors.
    /// </summary>
    /// <param name="typeName">The assembly-qualified or partially-qualified type name to resolve.</param>
    /// <returns>The resolved type, or <see langword="null"/> if it (or its assembly) could not be loaded.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2057:UnrecognizedReflectionPattern",
        Justification = "This is a best-effort probe for an optional package's type by name (EF Core/NHibernate/"
            + "FluentValidation transaction and validation behaviors) - returning null when trimming has removed "
            + "an unreferenced optional package is the same, correct 'that behavior is absent' outcome as the "
            + "package never having been referenced at all. See documentation/trim-and-aot-support.md."
    )]
    private static Type? TryGetType(string typeName)
    {
        try
        {
            return Type.GetType(typeName, throwOnError: false);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The registration shape shared by every built-in <c>Add*Behavior</c> convenience method whose body
    /// is exactly "add this behavior type, then try-add these default dependency pairs" - the behavior
    /// type plus its zero or more <c>(ServiceType, ImplementationType)</c> default dependency pairs, in
    /// the order <see cref="AddFromDescriptor"/> applies them.
    /// </summary>
    /// <param name="BehaviorType">The behavior's open-generic implementation type.</param>
    /// <param name="Defaults">The default dependency pairs to try-add alongside the behavior, in order.</param>
    private readonly record struct BehaviorDescriptor(
        Type BehaviorType,
        (Type ServiceType, Type ImplementationType)[] Defaults
    );
}
