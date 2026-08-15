namespace Interlocutor.DependencyInjection.Abstractions;

using System.Reflection;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Container-neutral fluent builder that is the single configuration surface for Interlocutor —
/// assembly scanning, pipeline behaviors, pipeline stages, and the try-add/override-able defaults
/// a subset of the built-in behaviors depend on.
/// </summary>
/// <remarks>
/// <para>
/// The <c>Interlocutor.DependencyInjection.Microsoft</c> and <c>Interlocutor.DependencyInjection.Autofac</c> packages
/// each expose an entry-point extension method that hands the consumer this exact type, so the chained
/// configuration calls a consumer writes are identical regardless of container. Each container package owns
/// only the translation from the accumulated <see cref="InterlocutorRegistration"/> lists to its own
/// registration primitives — no scanning, ordering, or override logic is duplicated per container.
/// </para>
/// <para>
/// Per <c>ADR-0009</c> ("No Default Pipeline"), a builder with no behavior calls produces zero pipeline
/// behaviors — <see cref="Behaviors"/> starts empty and stays empty unless a builder call adds to it.
/// <see cref="Defaults"/> starts seeded with exactly one entry, <see cref="IRequestContextAccessor"/>,
/// because it is ambient infrastructure useful independent of which (if any) pipeline behaviors are
/// configured; the audit sink and policy evaluator defaults are added only alongside the specific
/// behavior that consumes them, so a bare builder never carries an orphan dependency. There is
/// deliberately no automatic <see cref="IResponseOutcomeDescriber"/> default — see
/// <see cref="AddLoggingBehavior"/> for why.
/// </para>
/// <para>
/// <b>Order and duplicates.</b> <see cref="Behaviors"/> is a flat list in builder-call order. Adding the
/// same behavior type more than once is legal and both container translators register it at both
/// positions — neither de-duplicates. A request's actual chain is this same list filtered, at dispatch
/// time by each container's open-generic resolution, to the entries whose generic constraints the
/// request satisfies, with relative order preserved.
/// </para>
/// <para>
/// <b>Override precedence.</b> Every entry in <see cref="Defaults"/> is translated with try-add
/// semantics, so a consumer's own service registration made before the container translator runs always
/// wins. The <c>Override*</c> methods (<see cref="OverrideAuditSink{TSink}"/>,
/// <see cref="OverridePolicyEvaluator{TEvaluator}"/>, <see cref="OverrideRequestContextAccessor{TAccessor}"/>,
/// <see cref="OverrideOutcomeDescriber{TDescriber}"/>) replace the builder's own internal default entry
/// directly, so they win regardless of whether they are called before or after the corresponding
/// <c>Add*Behavior</c> call in the same chain — there is no scenario where call order within the builder
/// changes which implementation is registered.
/// </para>
/// <para>
/// <b>The three previously-dead <c>InterlocutorOptions</c> properties.</b> Of the legacy options never
/// actually read (default publish strategy, outcome describer type, startup validation), only the outcome
/// describer became a real builder member: <see cref="OverrideOutcomeDescriber{TDescriber}"/> genuinely
/// changes what <see cref="LoggingBehavior{TRequest,TResponse}"/> and <see cref="AuditingBehavior{TRequest,TResponse}"/>
/// resolve. The other two are deliberately absent from this builder rather than wired to look busy:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>Default publish strategy.</b> <c>Mediator.Publish</c>'s parameterless overload hard-codes
/// <c>PublishStrategy.Sequential</c>; making that configurable is a change to dispatch behavior in
/// <c>Interlocutor.Abstractions</c>, not a registration concern this container-neutral builder owns. The
/// existing <c>Publish(notification, strategy, ct)</c> overload already gives every call site an explicit
/// choice, which is more legible than a single ambient default a reader would have to go find.
/// </description></item>
/// <item><description>
/// <b>Startup validation.</b> Validating handler registrations needs a fully built provider/container,
/// which is deliberately outside this builder's job — the builder only ever accumulates registrations
/// for a container to apply, it never controls when the consumer finishes building it. Autofac exposes a
/// build-callback hook that could fire this automatically; Microsoft.Extensions.DependencyInjection has no
/// equivalent without taking a Hosting dependency this package otherwise doesn't need, so a builder flag
/// here could not behave identically on both containers. <c>InterlocutorStartupValidation.ValidateHandlerRegistrations</c>
/// remains available as an explicit, already container-neutral call a consumer makes once their provider exists.
/// </description></item>
/// </list>
/// </remarks>
public sealed class InterlocutorBuilder
{
    private readonly List<Assembly> _assembliesToScan = [];
    private readonly List<InterlocutorRegistration> _behaviors = [];
    private readonly Dictionary<Type, InterlocutorRegistration> _defaults;
    private readonly List<InterlocutorRegistration> _stages = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="InterlocutorBuilder"/> class, seeded with the
    /// single default every configuration needs regardless of which behaviors are added:
    /// <see cref="IRequestContextAccessor"/>.
    /// </summary>
    public InterlocutorBuilder()
    {
        this._defaults = new Dictionary<Type, InterlocutorRegistration>
        {
            [typeof(IRequestContextAccessor)] = new InterlocutorRegistration(
                typeof(IRequestContextAccessor),
                typeof(AmbientRequestContextAccessor),
                RegistrationLifetime.Scoped,
                RegistrationKind.Default
            ),
        };
    }

    /// <summary>Gets the assemblies configured for handler discovery, in the order they were added.</summary>
    /// <value>The assemblies configured for handler discovery, in the order they were added.</value>
    public IReadOnlyList<Assembly> AssembliesToScan => this._assembliesToScan;

    /// <summary>
    /// Gets the flat, ordered list of pipeline behavior registrations, in builder-call order. Empty on
    /// a bare builder. May contain the same implementation type more than once.
    /// </summary>
    /// <value>The flat, ordered list of pipeline behavior registrations, in builder-call order.</value>
    public IReadOnlyList<InterlocutorRegistration> Behaviors => this._behaviors;

    /// <summary>
    /// Gets the try-add-able default registrations, one per service type. Always contains exactly one
    /// entry per distinct service type — the most recently set entry for that type, whether set by an
    /// <c>Add*Behavior</c> convenience method or by the corresponding <c>Override*</c> method.
    /// </summary>
    /// <value>The try-add-able default registrations, one per service type.</value>
    public IReadOnlyCollection<InterlocutorRegistration> Defaults => this._defaults.Values;

    /// <summary>
    /// Gets the pipeline stage registrations added via <see cref="AddStage(Type, RegistrationLifetime)"/>,
    /// each keyed to the closed <see cref="IPipelineStage{TRequest,TResponse}"/> interface it implements.
    /// </summary>
    /// <value>The pipeline stage registrations added via <see cref="AddStage(Type, RegistrationLifetime)"/>.</value>
    public IReadOnlyList<InterlocutorRegistration> Stages => this._stages;

    /// <summary>Adds an assembly to scan for handler implementations.</summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ScanAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        this._assembliesToScan.Add(assembly);
        return this;
    }

    /// <summary>Adds one or more assemblies to scan for handler implementations, in the given order.</summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ScanAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        this._assembliesToScan.AddRange(assemblies);
        return this;
    }

    /// <summary>Adds the assembly containing <typeparamref name="TMarker"/> to scan for handler implementations.</summary>
    /// <typeparam name="TMarker">Any type whose containing assembly should be scanned.</typeparam>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ScanAssemblyContaining<TMarker>() => this.ScanAssembly(typeof(TMarker).Assembly);

    /// <summary>Adds the entry assembly to scan for handler implementations.</summary>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ScanEntryAssembly() => this.ScanAssembly(Assembly.GetEntryAssembly()!);

    /// <summary>
    /// Adds a pipeline behavior implementation at the current position in the chain. This is the
    /// general escape hatch every named <c>Add*Behavior</c> convenience method is sugar over — use it
    /// directly for a custom or third-party <see cref="IPipelineBehavior{TRequest,TResponse}"/>
    /// implementation the builder has no dedicated method for.
    /// </summary>
    /// <param name="behaviorImplementationType">
    /// The behavior's implementation type — typically an open generic type definition (for example
    /// <c>typeof(MyBehavior&lt;,&gt;)</c>) so it applies across request/response shapes, registered
    /// against the open <see cref="IPipelineBehavior{TRequest,TResponse}"/> contract.
    /// </param>
    /// <param name="lifetime">The service lifetime to register the behavior with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddBehavior(
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
    /// when nothing is registered — registering a default here would additionally require a default
    /// <c>IOutcomeClassifier</c> registration to keep it resolvable, which is exactly the kind of orphan
    /// dependency ADR-0009 asks the builder not to introduce. Call <see cref="OverrideOutcomeDescriber{TDescriber}"/>
    /// to opt into a specific implementation instead.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddLoggingBehavior() => this.AddBehavior(typeof(LoggingBehavior<,>));

    /// <summary>
    /// Adds <see cref="AuditingBehavior{TRequest,TResponse}"/> at the current position in the chain, and
    /// try-adds its default dependency, <see cref="IAuditSink"/> (<see cref="LoggingAuditSink"/>), unless
    /// a default has already been set. See <see cref="AddLoggingBehavior"/> for why no
    /// <see cref="IResponseOutcomeDescriber"/> default is registered here either.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddAuditingBehavior()
    {
        this.AddBehavior(typeof(AuditingBehavior<,>));
        this.TryAddDefault(typeof(IAuditSink), typeof(LoggingAuditSink));
        return this;
    }

    /// <summary>
    /// Adds <see cref="AuthorizationBehavior{TRequest,TSuccess}"/> at the current position in the chain,
    /// and try-adds its default dependency, <see cref="IPolicyEvaluator"/> (<see cref="DefaultPolicyEvaluator"/>),
    /// unless a default has already been set.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddAuthorizationBehavior()
    {
        this.AddBehavior(typeof(AuthorizationBehavior<,>));
        this.TryAddDefault(typeof(IPolicyEvaluator), typeof(DefaultPolicyEvaluator));
        return this;
    }

    /// <summary>
    /// Adds <c>Interlocutor.FluentValidation.ValidationBehavior&lt;,&gt;</c> at the current position in
    /// the chain, if the <c>Interlocutor.FluentValidation</c> package is referenced by the application.
    /// A no-op, matching the graceful-fallback behavior of <see cref="DefaultPipelineRegistrations"/>,
    /// when that package is not loaded.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddValidationBehavior()
    {
        var validationBehaviorType = TryGetType(
            "Interlocutor.FluentValidation.ValidationBehavior`2, Interlocutor.FluentValidation"
        );
        if (validationBehaviorType is not null)
        {
            this.AddBehavior(validationBehaviorType);
        }

        return this;
    }

    /// <summary>
    /// Adds the EF Core or NHibernate <c>TransactionBehavior</c> at the current position in the chain,
    /// preferring EF Core, if either store-specific package is referenced by the application. A no-op,
    /// matching the graceful-fallback behavior of <see cref="DefaultPipelineRegistrations"/>, when
    /// neither package is loaded.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddTransactionBehavior()
    {
        // EF Core's TransactionBehavior<TRequest,TResponse,TContext> is arity 3 (generic over the DbContext
        // type too); NHibernate's TransactionBehavior<TRequest,TResponse> is arity 2 (ISession isn't generic).
        var transactionBehaviorType =
            TryGetType("Interlocutor.EntityFrameworkCore.TransactionBehavior`3, Interlocutor.EntityFrameworkCore")
            ?? TryGetType("Interlocutor.NHibernate.TransactionBehavior`2, Interlocutor.NHibernate");
        if (transactionBehaviorType is not null)
        {
            this.AddBehavior(transactionBehaviorType);
        }

        return this;
    }

    /// <summary>
    /// Applies the library's documented recommended pipeline chain — logging, then auditing, then
    /// authorization, then validation, then transaction — in that order, as sugar over the individual
    /// <c>Add*Behavior</c> calls. Never applied automatically; a consumer opts in explicitly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order protects specific concerns, documented here because a developer composing their own
    /// chain instead of using this method needs the reasoning, not just the sequence:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Logging outermost so every request's timing and outcome is captured regardless of what happens inside.</description></item>
    /// <item><description>Auditing outside authorization so a request that gets denied is still recorded — a rejected attempt is itself an auditable event.</description></item>
    /// <item><description>Authorization before validation so an unauthorized caller never learns validation details about a request they cannot perform.</description></item>
    /// <item><description>Validation before transaction so malformed input never opens a transaction that would only be rolled back.</description></item>
    /// <item><description>Transaction innermost so only the handler's own work — not cross-cutting concerns — participates in the unit of work.</description></item>
    /// </list>
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder UseRecommendedChain() =>
        this.AddLoggingBehavior()
            .AddAuditingBehavior()
            .AddAuthorizationBehavior()
            .AddValidationBehavior()
            .AddTransactionBehavior();

    /// <summary>
    /// Replaces the default <see cref="IAuditSink"/> implementation <see cref="AuditingBehavior{TRequest,TResponse}"/>
    /// receives when no application-level registration is present. Wins regardless of whether it is
    /// called before or after <see cref="AddAuditingBehavior"/> in the same chain.
    /// </summary>
    /// <typeparam name="TSink">The replacement <see cref="IAuditSink"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder OverrideAuditSink<TSink>(RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
        where TSink : class, IAuditSink
    {
        this.SetDefault(typeof(IAuditSink), typeof(TSink), lifetime);
        return this;
    }

    /// <summary>
    /// Replaces the default <see cref="IPolicyEvaluator"/> implementation <see cref="AuthorizationBehavior{TRequest,TSuccess}"/>
    /// receives when no application-level registration is present. Wins regardless of whether it is
    /// called before or after <see cref="AddAuthorizationBehavior"/> in the same chain.
    /// </summary>
    /// <typeparam name="TEvaluator">The replacement <see cref="IPolicyEvaluator"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder OverridePolicyEvaluator<TEvaluator>(RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
        where TEvaluator : class, IPolicyEvaluator
    {
        this.SetDefault(typeof(IPolicyEvaluator), typeof(TEvaluator), lifetime);
        return this;
    }

    /// <summary>
    /// Replaces the default <see cref="IRequestContextAccessor"/> implementation
    /// (<see cref="AmbientRequestContextAccessor"/>) seeded by the constructor.
    /// </summary>
    /// <typeparam name="TAccessor">The replacement <see cref="IRequestContextAccessor"/> implementation.</typeparam>
    /// <param name="lifetime">The service lifetime to register the replacement with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder OverrideRequestContextAccessor<TAccessor>(
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
        where TAccessor : class, IRequestContextAccessor
    {
        this.SetDefault(typeof(IRequestContextAccessor), typeof(TAccessor), lifetime);
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
    public InterlocutorBuilder OverrideOutcomeDescriber<TDescriber>(
        RegistrationLifetime lifetime = RegistrationLifetime.Singleton
    )
        where TDescriber : class, IResponseOutcomeDescriber
    {
        this.SetDefault(typeof(IResponseOutcomeDescriber), typeof(TDescriber), lifetime);
        return this;
    }

    /// <summary>
    /// Registers a pipeline stage implementation directly, without assembly scanning. Reflects over the
    /// type's implemented interfaces and registers one <see cref="RegistrationKind.Stage"/> entry per
    /// closed <see cref="IPipelineStage{TRequest,TResponse}"/> interface it implements — mirroring how
    /// <see cref="HandlerDiscovery"/> registers a handler per closed contract interface.
    /// </summary>
    /// <param name="stageImplementationType">The concrete stage implementation type.</param>
    /// <param name="lifetime">The service lifetime to register the stage with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddStage(
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
    public InterlocutorBuilder AddStage<TStage>(RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
        where TStage : class => this.AddStage(typeof(TStage), lifetime);

    /// <summary>
    /// Adds <see cref="PipelineStageBehavior{TRequest,TResponse}"/> — the single host behavior that
    /// aggregates every registered <see cref="IPipelineStage{TRequest,TResponse}"/> — at the current
    /// position in the chain. An ordinary behavior entry: position it anywhere, and register it more
    /// than once if different stage groupings should sit at different depths.
    /// </summary>
    /// <param name="lifetime">The service lifetime to register the host with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddStageHost(RegistrationLifetime lifetime = RegistrationLifetime.Scoped) =>
        this.AddBehavior(typeof(PipelineStageBehavior<,>), lifetime);

    /// <summary>
    /// Sets or replaces the builder's own internal default entry for <paramref name="serviceType"/>,
    /// unconditionally — the mechanism behind every <c>Override*</c> method, guaranteeing it wins
    /// regardless of call order relative to the corresponding <c>Add*Behavior</c> call.
    /// </summary>
    private void SetDefault(Type serviceType, Type implementationType, RegistrationLifetime lifetime) =>
        this._defaults[serviceType] = new InterlocutorRegistration(
            serviceType,
            implementationType,
            lifetime,
            RegistrationKind.Default
        );

    /// <summary>
    /// Sets the builder's own internal default entry for <paramref name="serviceType"/> only if no entry
    /// exists yet — the mechanism behind every <c>Add*Behavior</c> convenience method's default
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
    /// <see langword="null"/> if the type (or the assembly it lives in) is not loaded — the same
    /// graceful-fallback lookup <see cref="DefaultPipelineRegistrations"/> uses for optional-package behaviors.
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
