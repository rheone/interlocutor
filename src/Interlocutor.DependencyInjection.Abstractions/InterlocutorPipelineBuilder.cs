namespace Interlocutor.DependencyInjection.Abstractions;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;

/// <summary>
/// Container-neutral fluent builder for pipeline composition alone - behaviors, stages, the
/// recommended-chain sugar, and the try-add/override-able defaults the built-in behaviors depend on.
/// </summary>
/// <remarks>
/// <para>
/// This is <see cref="InterlocutorBuilder"/>'s pipeline-composition responsibility, extracted so a
/// consumer (and a reader of this code) can reason about "what runs in what order" as a self-contained
/// concern, separate from "which assemblies get scanned" and "which infrastructure defaults are
/// overridden" - both of which remain <see cref="InterlocutorBuilder"/>'s job.
/// <see cref="InterlocutorBuilder"/>'s own pipeline methods (<see cref="InterlocutorBuilder.AddBehavior"/>,
/// <see cref="InterlocutorBuilder.AddLoggingBehavior"/>, and the rest) delegate to an instance of this
/// type rather than mutating their own state directly - every existing call on
/// <see cref="InterlocutorBuilder"/> keeps compiling and behaving exactly as before.
/// </para>
/// <para>
/// Per <c>ADR-0009</c> ("No Default Pipeline"), a builder with no behavior calls produces zero pipeline
/// behaviors - <see cref="Behaviors"/> starts empty and stays empty unless a builder call adds to it.
/// </para>
/// <para>
/// <b>Order and duplicates.</b> <see cref="Behaviors"/> is a flat list in builder-call order. Adding the
/// same behavior type more than once is legal - neither this type nor a container translator
/// de-duplicates.
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
/// </remarks>
public sealed class InterlocutorPipelineBuilder
{
    private readonly List<InterlocutorRegistration> _behaviors = [];
    private readonly Dictionary<Type, InterlocutorRegistration> _defaults;
    private readonly List<SkippedRegistration> _skipped = [];
    private readonly List<InterlocutorRegistration> _stages = [];
    private readonly List<InterlocutorFactoryRegistration> _stageFactories = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="InterlocutorPipelineBuilder"/> class, standalone,
    /// with its own empty defaults catalog.
    /// </summary>
    public InterlocutorPipelineBuilder()
        : this(new Dictionary<Type, InterlocutorRegistration>()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterlocutorPipelineBuilder"/> class sharing the
    /// given defaults dictionary - the constructor <see cref="InterlocutorBuilder"/> uses so that a
    /// pipeline default (for example, the <see cref="IAuditSink"/> entry <see cref="AddAuditingBehavior"/>
    /// try-adds) and an infrastructure override that targets the same service type
    /// (for example, <see cref="InterlocutorBuilder.OverrideAuditSink{TSink}"/>) are always the same
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
    /// <see cref="AddTransactionBehavior"/> when the corresponding package is not referenced.
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
        Type behaviorImplementationType,
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
    /// Adds <see cref="LoggingBehavior{TRequest,TResponse}"/> at the current position in the chain.
    /// </summary>
    /// <remarks>
    /// No <see cref="IResponseOutcomeDescriber"/> default is registered here: <see cref="LoggingBehavior{TRequest,TResponse}"/>
    /// takes it as an <em>optional</em> constructor dependency and already falls back to a describer
    /// built from <see cref="Interlocutor.Abstractions.Pipeline.Classifiers.OutcomeClassifierBuilder.DefaultClassifier"/>
    /// when nothing is registered - registering a default here would additionally require a default
    /// <c>IOutcomeClassifier</c> registration to keep it resolvable, which is exactly the kind of orphan
    /// dependency ADR-0009 asks the builder not to introduce. Call <see cref="InterlocutorBuilder.OverrideOutcomeDescriber{TDescriber}"/>
    /// to opt into a specific implementation instead.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddLoggingBehavior() => this.AddBehavior(typeof(LoggingBehavior<,>));

    /// <summary>
    /// Adds <see cref="AuditingBehavior{TRequest,TResponse}"/> at the current position in the chain, and
    /// try-adds its default dependency, <see cref="IAuditSink"/> (<see cref="LoggingAuditSink"/>), unless
    /// a default has already been set. See <see cref="AddLoggingBehavior"/> for why no
    /// <see cref="IResponseOutcomeDescriber"/> default is registered here either.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddAuditingBehavior()
    {
        this.AddBehavior(typeof(AuditingBehavior<,>));
        this.TryAddDefault(typeof(IAuditSink), typeof(LoggingAuditSink));
        return this;
    }

    /// <summary>
    /// Adds <see cref="AuthorizationBehavior{TRequest,TResponse}"/> at the current position in the chain,
    /// and try-adds its default dependencies, <see cref="IPolicyEvaluator"/> (<see cref="DefaultPolicyEvaluator"/>)
    /// and <see cref="IOutcomeCaseRegistry"/> (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default
    /// has already been set for either.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddAuthorizationBehavior()
    {
        this.AddBehavior(typeof(AuthorizationBehavior<,>));
        this.TryAddDefault(typeof(IPolicyEvaluator), typeof(DefaultPolicyEvaluator));
        this.TryAddDefault(typeof(IOutcomeCaseRegistry), typeof(DefaultOutcomeCaseRegistry));
        return this;
    }

    /// <summary>
    /// Adds <c>Interlocutor.FluentValidation.ValidationBehavior&lt;,&gt;</c> at the current position in
    /// the chain, if the <c>Interlocutor.FluentValidation</c> package is referenced by the application,
    /// and try-adds its default dependency, <see cref="IOutcomeCaseRegistry"/>
    /// (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default has already been set. A no-op when that
    /// package is not loaded.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
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
    /// Adds the EF Core or NHibernate <c>TransactionBehavior</c> at the current position in the chain,
    /// preferring EF Core, if either store-specific package is referenced by the application. A no-op
    /// when neither package is loaded.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddTransactionBehavior()
    {
        // EF Core's TransactionBehavior<TRequest,TResponse,TContext> is arity 3 (generic over the DbContext
        // type too); NHibernate's TransactionBehavior<TRequest,TResponse> is arity 2 (ISession isn't generic).
        // NHibernate is only probed - and only reported as skipped - when EF Core isn't found, preserving
        // the original short-circuit: a project referencing neither is silently skipped for both, one
        // referencing only NHibernate is skipped for EF Core alone.
        var efCoreType = TryGetType("Interlocutor.EntityFrameworkCore.TransactionBehavior`3, Interlocutor.EntityFrameworkCore");
        Type? nHibernateType = null;
        if (efCoreType is null)
        {
            this._skipped.Add(
                new SkippedRegistration(
                    "Interlocutor.EntityFrameworkCore.TransactionBehavior<,,>",
                    "Interlocutor.EntityFrameworkCore package is not referenced by the application"
                )
            );

            nHibernateType = TryGetType("Interlocutor.NHibernate.TransactionBehavior`2, Interlocutor.NHibernate");
            if (nHibernateType is null)
            {
                this._skipped.Add(
                    new SkippedRegistration(
                        "Interlocutor.NHibernate.TransactionBehavior<,>",
                        "Interlocutor.NHibernate package is not referenced by the application"
                    )
                );
            }
        }

        var transactionBehaviorType = efCoreType ?? nHibernateType;
        if (transactionBehaviorType is not null)
        {
            this.AddBehavior(transactionBehaviorType);
        }

        return this;
    }

    /// <summary>
    /// Adds <see cref="CancellationBehavior{TRequest,TResponse}"/> at the current position in the chain,
    /// and try-adds its default dependency, <see cref="IOutcomeCaseRegistry"/>
    /// (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default has already been set.
    /// </summary>
    /// <remarks>
    /// This behavior has no prescribed position in the pipeline - unlike the five behaviors composed by
    /// <see cref="UseRecommendedChain"/>, it is not part of that documented ordering. The caller chooses
    /// where it lands in their own chain by where they call this method relative to their other
    /// <c>Add*Behavior</c> calls.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddCancellationBehavior()
    {
        this.AddBehavior(typeof(CancellationBehavior<,>));
        this.TryAddDefault(typeof(IOutcomeCaseRegistry), typeof(DefaultOutcomeCaseRegistry));
        return this;
    }

    /// <summary>
    /// Adds <see cref="TimeoutBehavior{TRequest,TResponse}"/> at the current position in the chain, and
    /// try-adds its default dependencies, <see cref="ITimeoutPolicy"/> (<see cref="DefaultTimeoutPolicy"/>)
    /// and <see cref="IOutcomeCaseRegistry"/> (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default
    /// has already been set for either.
    /// </summary>
    /// <remarks>
    /// This behavior has no prescribed position in the pipeline - unlike the five behaviors composed by
    /// <see cref="UseRecommendedChain"/>, it is not part of that documented ordering. The caller chooses
    /// where it lands in their own chain by where they call this method relative to their other
    /// <c>Add*Behavior</c> calls.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddTimeoutBehavior()
    {
        this.AddBehavior(typeof(TimeoutBehavior<,>));
        this.TryAddDefault(typeof(ITimeoutPolicy), typeof(DefaultTimeoutPolicy));
        this.TryAddDefault(typeof(IOutcomeCaseRegistry), typeof(DefaultOutcomeCaseRegistry));
        return this;
    }

    /// <summary>
    /// Adds <see cref="RateLimitedBehavior{TRequest,TResponse}"/> at the current position in the chain,
    /// and try-adds its default dependencies, <see cref="IRateLimiter"/> (<see cref="DefaultRateLimiter"/>)
    /// and <see cref="IOutcomeCaseRegistry"/> (<see cref="DefaultOutcomeCaseRegistry"/>), unless a default
    /// has already been set for either.
    /// </summary>
    /// <remarks>
    /// This behavior has no prescribed position in the pipeline - unlike the five behaviors composed by
    /// <see cref="UseRecommendedChain"/>, it is not part of that documented ordering. The caller chooses
    /// where it lands in their own chain by where they call this method relative to their other
    /// <c>Add*Behavior</c> calls.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder AddRateLimitedBehavior()
    {
        this.AddBehavior(typeof(RateLimitedBehavior<,>));
        this.TryAddDefault(typeof(IRateLimiter), typeof(DefaultRateLimiter));
        this.TryAddDefault(typeof(IOutcomeCaseRegistry), typeof(DefaultOutcomeCaseRegistry));
        return this;
    }

    /// <summary>
    /// Applies the library's documented recommended pipeline chain - logging, then auditing, then
    /// authorization, then validation, then transaction - in that order, as sugar over the individual
    /// <c>Add*Behavior</c> calls. Never applied automatically; a consumer opts in explicitly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order protects specific concerns, documented here because a developer composing their own
    /// chain instead of using this method needs the reasoning, not just the sequence:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Logging outermost so every request's timing and outcome is captured regardless of what happens inside.</description></item>
    /// <item><description>Auditing outside authorization so a request that gets denied is still recorded - a rejected attempt is itself an auditable event.</description></item>
    /// <item><description>Authorization before validation so an unauthorized caller never learns validation details about a request they cannot perform.</description></item>
    /// <item><description>Validation before transaction so malformed input never opens a transaction that would only be rolled back.</description></item>
    /// <item><description>Transaction innermost so only the handler's own work - not cross-cutting concerns - participates in the unit of work.</description></item>
    /// </list>
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder UseRecommendedChain() =>
        this.AddLoggingBehavior()
            .AddAuditingBehavior()
            .AddAuthorizationBehavior()
            .AddValidationBehavior()
            .AddTransactionBehavior();

    /// <summary>
    /// Replaces the default <see cref="ITimeoutPolicy"/> implementation <see cref="TimeoutBehavior{TRequest,TResponse}"/>
    /// receives when no application-level registration is present. Wins regardless of whether it is
    /// called before or after <see cref="AddTimeoutBehavior"/> in the same chain.
    /// </summary>
    /// <typeparam name="TPolicy">The replacement <see cref="ITimeoutPolicy"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorPipelineBuilder OverrideTimeoutPolicy<TPolicy>(
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
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
    public InterlocutorPipelineBuilder OverrideRateLimiter<TLimiter>(
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
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
    public InterlocutorPipelineBuilder OverrideOutcomeCaseRegistry<TRegistry>(
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
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
        Type stageImplementationType,
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
    public InterlocutorPipelineBuilder AddStage<TStage>(RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
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
    /// Sets or replaces this builder's own internal default entry for <paramref name="serviceType"/>,
    /// unconditionally - the mechanism behind every pipeline-specific <c>Override*</c> method, guaranteeing
    /// it wins regardless of call order relative to the corresponding <c>Add*Behavior</c> call.
    /// </summary>
    private void SetDefault(Type serviceType, Type implementationType, RegistrationLifetime lifetime) =>
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
    private void TryAddDefault(Type serviceType, Type implementationType)
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
}
