namespace Interlocutor.DependencyInjection.Abstractions;

using System.Reflection;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Markers;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.Abstractions.Queries;

/// <summary>
/// Container-neutral fluent builder that is the single configuration surface for Interlocutor -
/// assembly scanning, pipeline behaviors, pipeline stages, and the try-add/override-able defaults
/// a subset of the built-in behaviors depend on.
/// </summary>
/// <remarks>
/// <para>
/// The <c>Interlocutor.DependencyInjection.Microsoft</c> and <c>Interlocutor.DependencyInjection.Autofac</c> packages
/// each expose an entry-point extension method that hands the consumer this exact type, so the chained
/// configuration calls a consumer writes are identical regardless of container. Each container package owns
/// only the translation from the accumulated <see cref="InterlocutorRegistration"/> lists to its own
/// registration primitives - no scanning, ordering, or override logic is duplicated per container.
/// </para>
/// <para>
/// <b>Pipeline composition lives on <see cref="Pipeline"/>.</b> Every pipeline-composition method - the
/// named <c>Add*Behavior</c> convenience methods, <c>AddBehavior</c>, <c>UseRecommendedChain</c>, the
/// <c>AddStage</c>/<c>AddStageHost</c> family, and the behavior-dependency <c>Override*</c> methods other
/// than <see cref="OverrideRequestContextAccessor{TAccessor}"/> - lives on <see cref="InterlocutorPipelineBuilder"/>,
/// reachable only via <see cref="Pipeline"/>. This keeps "what runs in what order" a self-contained
/// concern, separate from this type's own discovery (<c>Scan*</c>) and the one genuinely ambient default
/// it still owns directly. <see cref="Behaviors"/>, <see cref="Stages"/>, and <see cref="StageFactories"/>
/// remain pass-through projections of <see cref="Pipeline"/>'s state.
/// </para>
/// <para>
/// Per <c>ADR-0009</c> ("No Default Pipeline"), a builder with no behavior calls produces zero pipeline
/// behaviors - <see cref="Behaviors"/> starts empty and stays empty unless a builder call adds to it.
/// <see cref="Defaults"/> starts seeded with exactly one entry, <see cref="IRequestContextAccessor"/>,
/// because it is ambient infrastructure useful independent of which (if any) pipeline behaviors are
/// configured; the audit sink and policy evaluator defaults are added only alongside the specific
/// behavior that consumes them, so a bare builder never carries an orphan dependency. There is
/// deliberately no automatic <see cref="IResponseOutcomeDescriber"/> default - see
/// <c>Pipeline.AddLoggingBehavior</c> for why.
/// </para>
/// <para>
/// <b>Order and duplicates.</b> <see cref="Behaviors"/> is a flat list in builder-call order. Adding the
/// same behavior type more than once is legal and both container translators register it at both
/// positions - neither de-duplicates. A request's actual chain is this same list filtered, at dispatch
/// time by each container's open-generic resolution, to the entries whose generic constraints the
/// request satisfies, with relative order preserved.
/// </para>
/// <para>
/// <b>Override precedence.</b> Every entry in <see cref="Defaults"/> is translated with try-add
/// semantics, so a consumer's own service registration made before the container translator runs always
/// wins. The <c>Override*</c> methods - <see cref="OverrideRequestContextAccessor{TAccessor}"/> here, and
/// <c>OverrideAuditSink</c>/<c>OverridePolicyEvaluator</c>/<c>OverrideOutcomeDescriber</c> and the rest on
/// <see cref="Pipeline"/> - replace the builder's own internal default entry directly, so they win
/// regardless of whether they are called before or after the corresponding <c>Add*Behavior</c> call in
/// the same chain - there is no scenario where call order within the builder changes which implementation
/// is registered.
/// </para>
/// <para>
/// <b>The three previously-dead <c>InterlocutorOptions</c> properties.</b> Of the legacy options never
/// actually read (default publish strategy, outcome describer type, startup validation), only the outcome
/// describer became a real builder member: <c>Pipeline.OverrideOutcomeDescriber</c> genuinely
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
/// which is deliberately outside this builder's job - the builder only ever accumulates registrations
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
    private readonly List<(Assembly Assembly, RegistrationLifetime Lifetime)> _assembliesToScan = [];
    private readonly Dictionary<Type, InterlocutorRegistration> _defaults;
    private readonly Dictionary<(Type ServiceType, Type ImplementationType), InterlocutorRegistration> _explicitHandlers = [];
    private readonly List<InterlocutorRegistration> _generatedRegistrations = [];
    private readonly List<TypeMatchRule> _matchRules = [];
    private readonly InterlocutorPipelineBuilder _pipeline;
    private bool _validateOnBuild;

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

        // The pipeline builder shares this exact dictionary instance (rather than owning a copy) so a
        // pipeline default an Add*Behavior call try-adds (for example, IAuditSink) and an infrastructure
        // Override* call on this type that targets the same service type are always the same entry,
        // regardless of which one runs first.
        this._pipeline = new InterlocutorPipelineBuilder(this._defaults);
    }

    /// <summary>Gets the assemblies configured for handler discovery, in the order they were added.</summary>
    /// <value>The assemblies configured for handler discovery, in the order they were added.</value>
    public IReadOnlyList<Assembly> AssembliesToScan => [.. this._assembliesToScan.Select(entry => entry.Assembly)];

    /// <summary>
    /// Gets the sole entry point for pipeline composition - every behavior, stage, and behavior-dependency
    /// override lives here. See <see cref="InterlocutorPipelineBuilder"/> for the full method set.
    /// </summary>
    /// <value>The pipeline-composition builder backing this <see cref="InterlocutorBuilder"/>.</value>
    public InterlocutorPipelineBuilder Pipeline => this._pipeline;

    /// <summary>
    /// Gets the flat, ordered list of pipeline behavior registrations, in builder-call order. Empty on
    /// a bare builder. May contain the same implementation type more than once.
    /// </summary>
    /// <value>The flat, ordered list of pipeline behavior registrations, in builder-call order.</value>
    public IReadOnlyList<InterlocutorRegistration> Behaviors => this._pipeline.Behaviors;

    /// <summary>
    /// Gets the try-add-able default registrations, one per service type. Always contains exactly one
    /// entry per distinct service type - the most recently set entry for that type, whether set by an
    /// <c>Add*Behavior</c> convenience method or by the corresponding <c>Override*</c> method.
    /// </summary>
    /// <value>The try-add-able default registrations, one per service type.</value>
    public IReadOnlyCollection<InterlocutorRegistration> Defaults => this._defaults.Values;

    /// <summary>
    /// Gets the pipeline stage registrations added via <see cref="InterlocutorPipelineBuilder.AddStage(Type, RegistrationLifetime)"/>,
    /// each keyed to the closed <see cref="IPipelineStage{TRequest,TResponse}"/> interface it implements.
    /// </summary>
    /// <value>The pipeline stage registrations added via <see cref="InterlocutorPipelineBuilder.AddStage(Type, RegistrationLifetime)"/>.</value>
    public IReadOnlyList<InterlocutorRegistration> Stages => this._pipeline.Stages;

    /// <summary>
    /// Gets the delegate-backed pipeline stage registrations added via the <c>AddStage</c> delegate
    /// overloads - the lambda-form counterpart to <see cref="Stages"/>.
    /// </summary>
    /// <value>The delegate-backed pipeline stage registrations.</value>
    public IReadOnlyList<InterlocutorFactoryRegistration> StageFactories => this._pipeline.StageFactories;

    /// <summary>
    /// Gets the compile-time generated handler registrations added via
    /// <see cref="AddGeneratedRegistrations(IEnumerable{InterlocutorRegistration})"/>, in call order.
    /// </summary>
    /// <value>The compile-time generated handler registrations.</value>
    public IReadOnlyList<InterlocutorRegistration> GeneratedRegistrations => this._generatedRegistrations;

    /// <summary>
    /// Gets the merged set of handler registrations from both assembly scanning
    /// (<see cref="ScanAssembly(Assembly, RegistrationLifetime)"/> and its siblings) and explicit
    /// per-type registration (<see cref="AddHandler"/>) - the single list both container packages
    /// translate instead of calling <see cref="HandlerDiscovery.Discover(IEnumerable{Assembly})"/> directly.
    /// </summary>
    /// <remarks>
    /// An <see cref="AddHandler"/> call for a given (service type, implementation type) pair takes
    /// precedence over a duplicate of that same pair discovered by scanning in the same configuration -
    /// the explicit registration is the one this property yields, not an additional copy alongside it.
    /// Recomputed on every access, so it always reflects the builder's current configuration.
    /// </remarks>
    /// <value>The merged handler registrations, with explicit entries winning over scanned duplicates.</value>
    public IReadOnlyList<InterlocutorRegistration> Handlers
    {
        get
        {
            var merged = new Dictionary<(Type ServiceType, Type ImplementationType), InterlocutorRegistration>();

            foreach (var registration in HandlerDiscovery.Discover(this._assembliesToScan))
            {
                merged[(registration.ServiceType, registration.ImplementationType)] = registration;
            }

            // Explicit registrations are applied last so they overwrite - never duplicate - a scanned entry
            // for the same (service type, implementation type) pair.
            foreach (var (key, registration) in this._explicitHandlers)
            {
                merged[key] = registration;
            }

            return [.. merged.Values];
        }
    }

    /// <summary>
    /// Gets every predicate-based type-matching rule added via <see cref="AddTypeMatch"/>, in call order.
    /// </summary>
    /// <value>Every type-matching rule added so far.</value>
    public IReadOnlyList<TypeMatchRule> MatchRules => this._matchRules;

    /// <summary>
    /// Gets every registration produced by a <see cref="TypeMatchRule"/> matching a candidate type found
    /// while scanning - strictly additive to <see cref="Handlers"/> and <see cref="Behaviors"/>, both of
    /// which are unaffected by whether any match rules are configured.
    /// </summary>
    /// <remarks>
    /// Two rules matching the same type as the same service at the same lifetime coexist here as legal
    /// duplicates, consistent with the existing no-deduplication precedent for <see cref="RegistrationKind.Handler"/>
    /// and <see cref="RegistrationKind.Behavior"/> entries - this property performs no deduplication of its own.
    /// </remarks>
    /// <value>Every registration produced by a type-matching rule.</value>
    public IReadOnlyList<InterlocutorRegistration> MatchedRegistrations
    {
        get
        {
            var registrations = new List<InterlocutorRegistration>();
            if (this._matchRules.Count == 0)
            {
                return registrations;
            }

            var candidates = this.CandidateTypesForMatching().ToList();
            for (var ruleIndex = 0; ruleIndex < this._matchRules.Count; ruleIndex++)
            {
                var rule = this._matchRules[ruleIndex];
                // Falls back to the rule's own position when unnamed, so RegistrationReport.Format can
                // always distinguish which rule produced a given Matched registration, even when several
                // rules are configured without names.
                var ruleLabel = rule.Name ?? $"rule {ruleIndex}";

                foreach (var candidate in candidates)
                {
                    if (!rule.Predicate(candidate) || (rule.Except?.Invoke(candidate) ?? false))
                    {
                        continue;
                    }

                    var produced = rule.Target switch
                    {
                        TypeMatchTarget.Handler => HandlerDiscovery.DescribeHandler(
                            candidate,
                            rule.Lifetime,
                            RegistrationProvenance.Matched
                        ),
                        TypeMatchTarget.Behavior =>
                        [
                            new InterlocutorRegistration(
                                typeof(IPipelineBehavior<,>),
                                candidate,
                                rule.Lifetime,
                                RegistrationKind.Behavior,
                                RegistrationProvenance.Matched
                            ),
                        ],
                        _ => (IReadOnlyList<InterlocutorRegistration>)[],
                    };

                    registrations.AddRange(produced.Select(registration => registration with { MatchedByRule = ruleLabel }));
                }
            }

            return registrations;
        }
    }

    /// <summary>
    /// Adds a predicate-based type-matching rule: every candidate type found while scanning that
    /// <paramref name="predicate"/> matches (and <paramref name="except"/>, if given, does not) is
    /// registered as <paramref name="target"/> describes. Strictly additive to the existing
    /// marker-interface-based behavior scoping (<see cref="ITransactionalRequest"/>, <see cref="IAuditableRequest"/>,
    /// <see cref="IRequiresPolicy"/>, <see cref="IExternalStoreRequest"/>) - a behavior scoped by a marker
    /// interface constraint is unaffected by whether any match rules are configured.
    /// </summary>
    /// <remarks>
    /// Matching zero candidate types is legal and silent by default - pass <paramref name="require"/> as
    /// <see langword="true"/> to record that this rule must match at least one type; this call does not
    /// itself throw when a required rule matches nothing; see <see cref="ValidateOnBuild"/> for the opt-in
    /// check that does.
    /// </remarks>
    /// <param name="predicate">Tested against every candidate type found while scanning; every match is registered.</param>
    /// <param name="target">What role each matching type is registered as.</param>
    /// <param name="lifetime">The service lifetime to register each match with. Defaults to scoped.</param>
    /// <param name="except">
    /// An optional carve-out predicate excluding specific matches from the broader <paramref name="predicate"/>.
    /// </param>
    /// <param name="require">Whether this rule must match at least one candidate type.</param>
    /// <param name="name">
    /// An optional label for this rule, carried onto every registration it produces
    /// (<see cref="InterlocutorRegistration.MatchedByRule"/>) so the report from <see cref="DescribeRegistrations"/>
    /// can identify which rule matched a given type. Defaults to this rule's position among <see cref="MatchRules"/>.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddTypeMatch(
        Func<Type, bool> predicate,
        TypeMatchTarget target,
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped,
        Func<Type, bool>? except = null,
        bool require = false,
        string? name = null
    )
    {
        ArgumentNullException.ThrowIfNull(predicate);
        this._matchRules.Add(new TypeMatchRule(predicate, target, lifetime, except, require, name));
        return this;
    }

    /// <summary>
    /// Enumerates every candidate type found while scanning - the same concrete, non-abstract class filter
    /// <see cref="HandlerDiscovery.Discover(IEnumerable{Assembly})"/> applies, but unfiltered by handler
    /// contract, since a type-matching rule's predicate decides relevance instead.
    /// </summary>
    /// <returns>Every candidate type found while scanning.</returns>
    private IEnumerable<Type> CandidateTypesForMatching() =>
        this._assembliesToScan.SelectMany(entry => entry.Assembly.GetTypes()).Where(HandlerDiscovery.IsCandidateType);

    /// <summary>
    /// Adds handler registrations discovered at compile time by <c>Interlocutor.Generator</c> (ADR-0008),
    /// alongside - never instead of - any registrations produced by <see cref="ScanAssembly"/>/<see cref="ScanAssemblies(Assembly[])"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the generator's integration point into the container-neutral core: pass the generated module's
    /// own <c>Registrations</c> property straight through, for example
    /// <c>builder.AddGeneratedRegistrations(Interlocutor.Generated.InterlocutorGeneratedRegistrations.Registrations)</c>.
    /// Every entry is already a fully-formed <see cref="InterlocutorRegistration"/> built from compile-time
    /// <c>typeof(...)</c> tokens - this call performs no reflection of its own, and both container translators
    /// apply these entries with the same add semantics as a scanned <see cref="RegistrationKind.Handler"/>
    /// registration (see <see cref="RegistrationKind.Handler"/>'s remarks).
    /// </para>
    /// <para>
    /// Runtime assembly scanning (<see cref="ScanAssembly"/>/<see cref="ScanAssemblies(Assembly[])"/>/<see cref="ScanAssemblyContaining{TMarker}"/>/<see cref="ScanEntryAssembly"/>)
    /// remains fully independent and opt-in - calling this method does not disable scanning, and a bare builder
    /// that calls neither registers no handlers at all. A consumer who wants generated registrations to be the
    /// *only* source of handlers simply never calls a <c>Scan*</c> method, which was already true before this
    /// method existed: <see cref="AssembliesToScan"/> starts empty on a fresh builder regardless.
    /// </para>
    /// </remarks>
    /// <param name="registrations">
    /// The generated handler registrations to add - typically a generated module's <c>Registrations</c> property.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddGeneratedRegistrations(IEnumerable<InterlocutorRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        this._generatedRegistrations.AddRange(registrations);
        return this;
    }

    /// <summary>
    /// Builds a structured, container-neutral report of everything this builder has accumulated so far -
    /// every registration with its provenance, every optional-package probe that found nothing, and
    /// (only once <see cref="ValidateOnBuild"/> is configured) every validation problem found. Buildable and
    /// inspectable at any time; <see cref="RegistrationReport.Problems"/> stays empty when
    /// <see cref="ValidateOnBuild"/> was never called, since computing it means re-scanning every configured
    /// assembly and is therefore not free - consistent with <see cref="ValidateOnBuild"/> being opt-in.
    /// </summary>
    /// <returns>The structured registration report.</returns>
    public RegistrationReport DescribeRegistrations()
    {
        var registrations = new List<InterlocutorRegistration>();
        registrations.AddRange(this.Handlers);
        registrations.AddRange(this._generatedRegistrations);
        registrations.AddRange(this._pipeline.Stages);
        registrations.AddRange(this._pipeline.Behaviors);
        registrations.AddRange(this._defaults.Values);
        registrations.AddRange(this.MatchedRegistrations);

        var problems = this._validateOnBuild ? this.ComputeProblems(registrations) : [];

        return new RegistrationReport(registrations, this._pipeline.StageFactories, this._pipeline.Skipped, problems);
    }

    /// <summary>
    /// Opts into <see cref="DescribeRegistrations"/> computing <see cref="RegistrationReport.Problems"/> -
    /// checking the accumulated registration list, with no live container needed, for a query or command
    /// type discovered while scanning with no matching handler, a required type-matching rule
    /// (<see cref="AddTypeMatch"/>) that matched nothing, and two registrations of the same (service type,
    /// implementation type) pair at conflicting lifetimes. When the container translator finds any such
    /// problem, it throws a single <see cref="InterlocutorRegistrationValidationException"/> carrying every
    /// problem found, aggregated rather than failing on the first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The unhandled-request-type check covers <c>IQuery&lt;TResponse&gt;</c>/<c>ICommand&lt;TResponse&gt;</c>
    /// only - the same scope <c>InterlocutorStartupValidation.ValidateHandlerRegistrations</c> already checks
    /// at the container level. A void <c>ICommand</c> is not checked (its result type isn't declared on the
    /// command, so no single expected handler interface can be derived from the type alone), nor is
    /// <c>INotificationHandler&lt;TNotification&gt;</c> (zero notification handlers is a valid configuration,
    /// unlike zero query/command handlers).
    /// </para>
    /// <para>
    /// Deliberately does not attempt anything that needs a live container - an unresolvable constructor
    /// dependency, for instance, remains the job of the existing, separate, still-opt-in
    /// <c>InterlocutorStartupValidation.ValidateHandlerRegistrations</c>, which this method leaves untouched.
    /// Without this call, <see cref="DescribeRegistrations"/> runs zero additional checks - the report's
    /// <see cref="RegistrationReport.Problems"/> stays empty, same as today.
    /// </para>
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ValidateOnBuild()
    {
        this._validateOnBuild = true;
        return this;
    }

    /// <summary>
    /// Checks the accumulated registration list for every problem <see cref="ValidateOnBuild"/> documents,
    /// answerable from <paramref name="registrations"/> and this builder's own state alone.
    /// </summary>
    /// <param name="registrations">Every registration accumulated so far, as <see cref="DescribeRegistrations"/> assembled it.</param>
    /// <returns>Every problem found.</returns>
    private List<RegistrationProblem> ComputeProblems(IReadOnlyList<InterlocutorRegistration> registrations)
    {
        var problems = new List<RegistrationProblem>();
        var candidates = this.CandidateTypesForMatching().ToList();

        // A request type discovered while scanning with no matching handler - the same detection
        // InterlocutorStartupValidation.ValidateHandlerRegistrations uses, but checked against the
        // accumulated registration list instead of a live container.
        foreach (var requestType in candidates)
        {
            foreach (var iface in requestType.GetInterfaces().Where(i => i.IsGenericType))
            {
                var handlerType = iface.GetGenericTypeDefinition() switch
                {
                    var def when def == typeof(IQuery<>) => typeof(IQueryHandler<,>).MakeGenericType(
                        requestType,
                        iface.GetGenericArguments()[0]
                    ),
                    var def when def == typeof(ICommand<>) => typeof(ICommandHandler<,>).MakeGenericType(
                        requestType,
                        iface.GetGenericArguments()[0]
                    ),
                    _ => null,
                };

                if (
                    handlerType is not null
                    && !registrations.Any(r => r.Kind == RegistrationKind.Handler && r.ServiceType == handlerType)
                )
                {
                    problems.Add(
                        new RegistrationProblem(
                            RegistrationProblemKind.UnhandledRequestType,
                            $"{requestType.Name} implements {iface.Name} but no {handlerType.Name} is registered."
                        )
                    );
                }
            }
        }

        // A required type-matching rule that matched nothing.
        foreach (var rule in this._matchRules.Where(r => r.Require))
        {
            var matchedAnything = candidates.Any(candidate =>
                rule.Predicate(candidate) && !(rule.Except?.Invoke(candidate) ?? false)
            );
            if (!matchedAnything)
            {
                problems.Add(
                    new RegistrationProblem(
                        RegistrationProblemKind.UnmatchedRequiredRule,
                        $"A required type-matching rule targeting {rule.Target} matched no candidate type."
                    )
                );
            }
        }

        // Two registrations of the same (service type, implementation type) pair at conflicting lifetimes.
        foreach (var group in registrations.GroupBy(r => (r.ServiceType, r.ImplementationType)))
        {
            var lifetimes = group.Select(r => r.Lifetime).Distinct().ToList();
            if (lifetimes.Count > 1)
            {
                problems.Add(
                    new RegistrationProblem(
                        RegistrationProblemKind.ConflictingLifetime,
                        $"{group.Key.ServiceType.Name} -> {group.Key.ImplementationType.Name} is registered at conflicting lifetimes: {string.Join(", ", lifetimes)}."
                    )
                );
            }
        }

        return problems;
    }

    /// <summary>Adds an assembly to scan for handler implementations.</summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="lifetime">The service lifetime to register every handler this assembly yields with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ScanAssembly(Assembly assembly, RegistrationLifetime lifetime = RegistrationLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        this._assembliesToScan.Add((assembly, lifetime));
        return this;
    }

    /// <summary>Adds one or more assemblies, all at the default scoped lifetime, to scan for handler implementations, in the given order.</summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ScanAssemblies(params Assembly[] assemblies) =>
        this.ScanAssemblies(RegistrationLifetime.Scoped, assemblies);

    /// <summary>Adds one or more assemblies to scan for handler implementations, in the given order.</summary>
    /// <param name="lifetime">The service lifetime to register every handler these assemblies yield with.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ScanAssemblies(RegistrationLifetime lifetime, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        foreach (var assembly in assemblies)
        {
            this._assembliesToScan.Add((assembly, lifetime));
        }

        return this;
    }

    /// <summary>Adds the assembly containing <typeparamref name="TMarker"/> to scan for handler implementations.</summary>
    /// <typeparam name="TMarker">Any type whose containing assembly should be scanned.</typeparam>
    /// <param name="lifetime">The service lifetime to register every handler this assembly yields with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ScanAssemblyContaining<TMarker>(RegistrationLifetime lifetime = RegistrationLifetime.Scoped) =>
        this.ScanAssembly(typeof(TMarker).Assembly, lifetime);

    /// <summary>Adds the entry assembly to scan for handler implementations.</summary>
    /// <param name="lifetime">The service lifetime to register every handler this assembly yields with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder ScanEntryAssembly(RegistrationLifetime lifetime = RegistrationLifetime.Scoped) =>
        this.ScanAssembly(Assembly.GetEntryAssembly()!, lifetime);

    /// <summary>
    /// Registers a single handler implementation type directly, without assembly scanning - the manual
    /// escape hatch for handlers that mirrors <see cref="InterlocutorPipelineBuilder.AddBehavior"/>'s shape. Reflects over the type's
    /// implemented interfaces and registers one <see cref="RegistrationKind.Handler"/> entry per matching
    /// handler contract interface it implements (open or closed), each carrying
    /// <see cref="RegistrationProvenance.Explicit"/> provenance.
    /// </summary>
    /// <remarks>
    /// When a <c>ScanAssembly*</c> call in the same configuration would also discover
    /// <paramref name="handlerImplementationType"/>, this explicit registration is the one <see cref="Handlers"/>
    /// yields for each (service type, implementation type) pair the two would otherwise both produce - not
    /// an additional copy alongside the scanned one. A type implementing no handler contract registers nothing.
    /// </remarks>
    /// <param name="handlerImplementationType">
    /// The handler's implementation type - concrete, or an open generic type definition to register against
    /// every closed contract interface it implements.
    /// </param>
    /// <param name="lifetime">The service lifetime to register the handler with. Defaults to scoped.</param>
    /// <returns>This builder, for chaining.</returns>
    public InterlocutorBuilder AddHandler(
        Type handlerImplementationType,
        RegistrationLifetime lifetime = RegistrationLifetime.Scoped
    )
    {
        ArgumentNullException.ThrowIfNull(handlerImplementationType);

        foreach (
            var registration in HandlerDiscovery.DescribeHandler(
                handlerImplementationType,
                lifetime,
                RegistrationProvenance.Explicit
            )
        )
        {
            this._explicitHandlers[(registration.ServiceType, registration.ImplementationType)] = registration;
        }

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
        this._pipeline.SetDefault(typeof(IRequestContextAccessor), typeof(TAccessor), lifetime);
        return this;
    }
}
