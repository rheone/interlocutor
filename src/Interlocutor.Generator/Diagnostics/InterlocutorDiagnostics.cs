namespace Interlocutor.Generator.Diagnostics;

using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Emission;
using Interlocutor.Generator.Models;
using Microsoft.CodeAnalysis;

/// <summary>
/// The diagnostic descriptors <see cref="InterlocutorSourceGenerator"/> reports.
/// </summary>
internal static class InterlocutorDiagnostics
{
    /// <summary>
    /// Reported when a type implements <c>IQuery&lt;TResponse&gt;</c>, <c>ICommand&lt;TResponse&gt;</c>, or
    /// <c>ICommand</c> but the compiling project defines no matching handler for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default severity is <see cref="DiagnosticSeverity.Warning"/> - per ADR-0008 and this ticket's acceptance
    /// criteria, a consumer escalates it to an error via the standard <c>.editorconfig</c> mechanism every other
    /// diagnostic in this repository uses:
    /// <code>
    /// dotnet_diagnostic.INTERLOC001.severity = error
    /// </code>
    /// A handler for this request living in a different, referenced assembly is considered too (ticket 12) -
    /// see <see cref="ReferencedModuleDiscovery"/> - as long as that assembly's own generated module is reachable
    /// through the current compilation's reference graph, transitively. Only a handler declared purely in a
    /// sibling assembly the current compilation neither directly nor transitively references, or one registered
    /// only via runtime assembly scanning, is invisible to this check.
    /// </para>
    /// <para>
    /// This has an inherent, asymmetric blind spot under acyclic project references: a request-declaring
    /// assembly can never see a handler implemented *downstream* of it (for example, in the application that
    /// eventually references it), since the request-declaring library's own build necessarily happens before
    /// any consumer's. That library's own compilation reports this diagnostic even though the request is
    /// ultimately served once the application is built - a false positive at the library level, not fixable
    /// without either a circular reference (impossible) or deferring the check to the final consuming
    /// compilation only (a design this ticket did not take, since a library ought to be independently
    /// buildable and warn-clean on its own terms). Suppress per-request via <c>.editorconfig</c> or a targeted
    /// <c>#pragma warning disable INTERLOC001</c> if a library intentionally declares a request it expects a
    /// downstream consumer to serve.
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor MissingHandler = new(
        id: "INTERLOC001",
        title: "No handler registered for request type",
        messageFormat: "Request type '{0}' has no matching handler in this compilation",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A type implementing IQuery<TResponse>, ICommand<TResponse>, or ICommand was found with no "
            + "corresponding IQueryHandler<,>, ICommandHandler<,>, or IBareCommandHandler<,> implementation "
            + "reachable from this compilation, including transitively-referenced assemblies. Add a handler, or "
            + "reference the assembly that defines one."
    );

    /// <summary>
    /// Reported when more than one concrete <c>IQueryHandler&lt;TQuery, TResponse&gt;</c> or
    /// <c>ICommandHandler&lt;TCommand, TResponse&gt;</c> implementation is found in the compiling project for the
    /// same closed request/response pair.
    /// </summary>
    /// <remarks>
    /// Unlike a notification (see <see cref="RequestKind"/>'s remarks), a query or command must resolve to
    /// exactly one handler - <c>Mediator.SendAsync</c> and the generated dispatch wrapper both resolve the handler's
    /// service interface as a single service, so a second registration silently shadows the first rather than
    /// being an error at resolution time. This diagnostic catches that ambiguity at build time instead. Default
    /// severity is <see cref="DiagnosticSeverity.Warning"/>, escalated the same way as
    /// <see cref="MissingHandler"/>:
    /// <code>
    /// dotnet_diagnostic.INTERLOC002.severity = error
    /// </code>
    /// </remarks>
    public static readonly DiagnosticDescriptor DuplicateHandler = new(
        id: "INTERLOC002",
        title: "Multiple handlers registered for request type",
        messageFormat: "Request type '{0}' has multiple matching handlers in this compilation: {1}",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "More than one IQueryHandler<,> or ICommandHandler<,> implementation was found for the same "
            + "closed request/response pair in the same compilation. A query or command must resolve to exactly "
            + "one handler, unlike a notification, which permits zero or many. Remove or rename the extra "
            + "handler so only one remains."
    );

    /// <summary>
    /// Reported when a notification handler implements <c>INotificationHandler&lt;INotification&gt;</c> -
    /// subscribing to the base notification interface itself, and therefore to every notification published
    /// anywhere in the process.
    /// </summary>
    /// <remarks>
    /// Legal by design (ADR-0007) - this is the entire point of contravariant fan-out - but a process-wide
    /// subscriber is easy to write by accident (e.g. a typo'd generic constraint). Default severity is
    /// <see cref="DiagnosticSeverity.Info"/>: this is a deliberate-choice confirmation, not a problem.
    /// </remarks>
    public static readonly DiagnosticDescriptor CatchAllNotificationHandler = new(
        id: "INTERLOC003",
        title: "Catch-all notification handler",
        messageFormat: "'{0}' handles every published notification (INotificationHandler<INotification>)",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A handler implementing INotificationHandler<INotification> receives every notification "
            + "published anywhere in the process, not just one family of events. This is a legal and useful "
            + "pattern (a process-wide audit sink, for example), but is reported so it reads as a deliberate "
            + "choice rather than an accidental catch-all."
    );

    /// <summary>
    /// Reported when the relative order declared across notification handlers via
    /// <c>NotificationHandlerRunAfterAttribute</c>/<c>NotificationHandlerRunBeforeAttribute</c> contains a
    /// cycle - no topological order exists.
    /// </summary>
    /// <remarks>
    /// Default severity is <see cref="DiagnosticSeverity.Error"/>, not warning: an ordering cycle has no
    /// meaningful fallback behavior to run with, unlike <see cref="MissingHandler"/> or
    /// <see cref="DuplicateHandler"/>, which still produce a program that runs (just not the one the developer
    /// intended). It is not escalated via <c>.editorconfig</c> like those two - it is always an error.
    /// </remarks>
    public static readonly DiagnosticDescriptor NotificationHandlerOrderingCycle = new(
        id: "INTERLOC004",
        title: "Notification handler ordering cycle",
        messageFormat: "Notification handler run-after/run-before ordering forms a cycle: {0}",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "NotificationHandlerRunAfterAttribute/NotificationHandlerRunBeforeAttribute declared a "
            + "relative ordering across notification handlers that contains a cycle, so no consistent "
            + "execution order exists. Remove or redirect one of the run-after/run-before declarations named "
            + "in the cycle."
    );

    /// <summary>
    /// Reported when a <c>NotificationHandlerRunAfterAttribute</c>/<c>NotificationHandlerRunBeforeAttribute</c>
    /// argument names a type that is not itself a discovered <c>INotificationHandler&lt;TNotification&gt;</c>
    /// implementation in this compilation.
    /// </summary>
    /// <remarks>
    /// Default severity is <see cref="DiagnosticSeverity.Error"/>: the named type can never actually run, so
    /// the declared relationship can never be honored - there is no meaningful partial behavior to fall back
    /// to, the same reasoning behind <see cref="NotificationHandlerOrderingCycle"/> being an always-on error
    /// rather than a <c>.editorconfig</c>-escalated warning.
    /// </remarks>
    public static readonly DiagnosticDescriptor NotificationHandlerOrderingReferenceIsNotAHandler = new(
        id: "INTERLOC005",
        title: "Notification handler ordering reference is not a handler",
        messageFormat: "'{0}' names '{1}' in a run-after/run-before attribute, but '{1}' is not a discovered INotificationHandler<> implementation in this compilation",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "NotificationHandlerRunAfterAttribute/NotificationHandlerRunBeforeAttribute's type argument "
            + "must name another INotificationHandler<TNotification> implementation discovered in the same "
            + "compilation. A type that is not a notification handler (or a handler declared in a different, "
            + "referenced assembly - cross-assembly discovery is out of scope, see ticket 12) can never "
            + "participate in the ordering it names."
    );

    /// <summary>
    /// Reported when more than one open generic query or command handler closes over the same concrete request
    /// type once every discovered closed-generic usage is matched against every open generic handler's own
    /// constraints, and neither is resolved as the winner by a <c>HandlerOverrideAttribute</c>.
    /// </summary>
    /// <remarks>
    /// There is exactly one selection rule between two open generic handlers: an explicit
    /// <c>HandlerOverrideAttribute</c> naming the other. No ranking by inheritance distance, namespace, or
    /// assembly location is ever considered, so moving either handler's declaring file never changes which
    /// handler runs - an unresolved ambiguity is always a build error naming both handlers, never an arbitrary
    /// runtime choice. Default severity is <see cref="DiagnosticSeverity.Error"/>: like
    /// <see cref="NotificationHandlerOrderingCycle"/>, there is no meaningful partial behavior to fall back to.
    /// </remarks>
    public static readonly DiagnosticDescriptor OpenGenericHandlerAmbiguity = new(
        id: "INTERLOC006",
        title: "Ambiguous open generic handlers for request type",
        messageFormat: "Request type '{0}' is matched by multiple open generic handlers with no resolving "
            + "HandlerOverrideAttribute: {1}",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "More than one open generic IQueryHandler<,>/ICommandHandler<,> implementation closes over "
            + "the same concrete request type once constraints are applied. Add a HandlerOverrideAttribute to "
            + "one of the handlers naming the other, or narrow one handler's type parameter constraints so only "
            + "one still matches."
    );

    /// <summary>
    /// Reported when a <c>HandlerOverrideAttribute</c> argument names a type that is not itself a discovered
    /// handler (open generic or closed) in this compilation.
    /// </summary>
    /// <remarks>
    /// Default severity is <see cref="DiagnosticSeverity.Error"/>, for the same reason as
    /// <see cref="NotificationHandlerOrderingReferenceIsNotAHandler"/>: the named type can never actually run,
    /// so the declared override can never be honored.
    /// </remarks>
    public static readonly DiagnosticDescriptor HandlerOverrideInvalidTarget = new(
        id: "INTERLOC007",
        title: "Handler override reference is not a handler",
        messageFormat: "'{0}' names '{1}' in a HandlerOverrideAttribute, but '{1}' is not a discovered handler "
            + "implementation in this compilation",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "HandlerOverrideAttribute's type argument must name another IQueryHandler<,>/"
            + "ICommandHandler<,> implementation (open generic or closed) discovered in the same compilation. A "
            + "type that is not a handler - or a handler declared in a different, referenced assembly - can "
            + "never participate in the override it names."
    );

    /// <summary>
    /// Reported when a closed usage of a generic <c>IQuery&lt;TResponse&gt;</c>/<c>ICommand&lt;TResponse&gt;</c>
    /// request type resolves to no handler at all - neither a closed handler nor an open generic handler
    /// closed over it locally (ticket 11), nor a closed handler from an upstream referenced assembly's
    /// generated module (ticket 12).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Open generic handler closing is same-compilation-only: <see cref="OpenGenericHandlerResolver"/> only
    /// ever sees open generic handlers and closed usages discovered by <em>this</em> compilation's own syntax
    /// trees, never a referenced library's. An open generic handler declared in library A, used only via a
    /// closed instantiation in application B (which references A), is invisible to both sides' own generator
    /// runs - A's run finished before B exists, and B's run never sees A's syntax, only its metadata. Left
    /// unresolved, that produced a request no registered handler ever backs, previously discovered only when
    /// <c>Mediator.SendAsync</c> throws <c>Interlocutor.Abstractions.Core.HandlerNotRegisteredException</c>
    /// at first dispatch. This diagnostic surfaces the same gap at build time instead.
    /// </para>
    /// <para>
    /// Default severity is <see cref="DiagnosticSeverity.Warning"/>, escalated the same way as
    /// <see cref="MissingHandler"/>:
    /// <code>
    /// dotnet_diagnostic.INTERLOC008.severity = error
    /// </code>
    /// Move the closed usage into the handler's own assembly (so the same compilation sees both), or add an
    /// explicit closed handler for it in the consuming project, to resolve.
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor UnresolvedGenericRequestUsage = new(
        id: "INTERLOC008",
        title: "Unresolved generic request usage",
        messageFormat: "Request usage '{0}' has no matching handler - open generic handler closing only spans "
            + "the current compilation, so a handler declared in a different referenced assembly cannot close "
            + "over a usage found only here",
        category: "Interlocutor.Discovery",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A closed usage of a generic IQuery<TResponse>/ICommand<TResponse> request type resolved "
            + "to no handler: not a closed handler, not an open generic handler closed over it in this "
            + "compilation, and not a closed handler from a referenced assembly's generated module. Open "
            + "generic handler closing only spans the current compilation's own syntax trees - move the usage "
            + "into the handler's own assembly, or add an explicit closed handler for it here."
    );

    /// <summary>
    /// Reported when a closed generic type implementing <c>IOutcome</c> repeats the same type in more than one
    /// type-argument position - e.g. <c>Outcome&lt;Success, Failure, Failure&gt;</c>.
    /// </summary>
    /// <remarks>
    /// C# has no generic constraint expressing "these type arguments must all be distinct," so nothing at the
    /// language level stops a caller from closing an outcome union this way, silently collapsing two
    /// supposedly-independent case positions into one. Reported by <see cref="DuplicateOutcomeCaseTypeAnalyzer"/>
    /// for any closed generic <c>IOutcome</c> implementer found anywhere in the compilation - the library's own
    /// shipped <c>Outcome&lt;...&gt;</c> family and an arbitrary consumer-declared union alike, since the check
    /// is driven purely by <c>IOutcome</c> membership, never by the closed type's name. Default severity is
    /// <see cref="DiagnosticSeverity.Warning"/>, escalated the same way as <see cref="MissingHandler"/>:
    /// <code>
    /// dotnet_diagnostic.INTERLOC009.severity = error
    /// </code>
    /// </remarks>
    public static readonly DiagnosticDescriptor DuplicateOutcomeCaseType = new(
        id: "INTERLOC009",
        title: "Duplicate outcome case type",
        messageFormat: "'{0}' repeats case type '{1}' in type-argument positions {2} - each case position must "
            + "be a distinct type",
        category: "Interlocutor.Outcomes",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A closed generic type implementing IOutcome repeated the same type in more than one "
            + "type-argument position. C# has no generic constraint expressing 'these type arguments must all "
            + "be distinct,' so this silently collapses two supposedly-independent case positions into one, "
            + "defeating the point of picking a specific arity for what an operation can actually produce. "
            + "Replace the repeated position with a distinct case type."
    );

    /// <summary>
    /// Reported when a <c>FluentValidation.AbstractValidator&lt;TCommand&gt;</c>/<c>AbstractValidator&lt;TQuery&gt;</c>
    /// exists for a request whose declared response arity has no validation-shaped case.
    /// </summary>
    /// <remarks>
    /// <c>ResourceCommandHandler</c>'s response shape is fixed to <c>Outcome&lt;TSuccess, NotFound, Failure&gt;</c>,
    /// which has no case an <c>Interlocutor.FluentValidation.ValidationBehavior</c> failure can occupy -
    /// registering a validator for a command shaped like that doesn't fail at compile time today, it throws the
    /// first time a real validation failure happens at runtime, because the behavior has nowhere to put the
    /// result. Reported by <see cref="ValidationShapeMismatchAnalyzer"/> for any request/response pair shaped
    /// this way, whether or not <c>ResourceCommandHandler</c> is involved. Default severity is
    /// <see cref="DiagnosticSeverity.Warning"/>, escalated the same way as <see cref="MissingHandler"/>:
    /// <code>
    /// dotnet_diagnostic.INTERLOC010.severity = error
    /// </code>
    /// </remarks>
    public static readonly DiagnosticDescriptor ValidationShapeMismatch = new(
        id: "INTERLOC010",
        title: "Validator exists for a request with no validation-shaped response case",
        messageFormat: "'{0}' has a FluentValidation validator ('{1}') but its response type '{2}' has no "
            + "validation-shaped case among its case types ({3}) - add a case implementing "
            + "IValidationFailureCase (or ValidationError/ValidationErrors), or remove the validator",
        category: "Interlocutor.Validation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An AbstractValidator<TCommand>/AbstractValidator<TQuery> exists for a request whose "
            + "declared response type has no case implementing IValidationFailureCase (or the concrete "
            + "ValidationError/ValidationErrors types). ValidationBehavior has nowhere to put a real validation "
            + "failure for this request, so it throws at the first validation failure instead of returning an "
            + "outcome. Add a validation-shaped case to the response arity, or remove the validator."
    );

    /// <summary>
    /// Reported when a <c>partial union</c> decorated with <c>GenerateOutcomeMembersAttribute</c> declares
    /// fewer than two case types.
    /// </summary>
    /// <remarks>
    /// A single-case union has no alternative for a <c>switch</c> to be exhaustive over, so it isn't a
    /// meaningful outcome - <see cref="OutcomeMemberGenerator"/> refuses to emit <c>Match</c>/<c>Switch</c>
    /// members for it rather than silently emitting a no-op or a member set that can never do anything useful.
    /// Always <see cref="DiagnosticSeverity.Error"/> - unlike <see cref="MissingHandler"/> or
    /// <see cref="DuplicateHandler"/>, there is no meaningful partial member set to fall back to.
    /// </remarks>
    public static readonly DiagnosticDescriptor TooFewOutcomeCaseTypes = new(
        id: "INTERLOC011",
        title: "Attributed union declares fewer than two case types",
        messageFormat: "'{0}' is decorated with GenerateOutcomeMembersAttribute but declares {1} case type(s) - "
            + "at least two are required for a meaningful outcome union",
        category: "Interlocutor.Outcomes",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "GenerateOutcomeMembersAttribute marks a partial union declaration for source-generated "
            + "Match/Switch/TryGetValue/ToString members. A union with fewer than two case types has "
            + "no alternative for a switch to be exhaustive over, so it isn't a meaningful outcome - add at "
            + "least one more case type, or remove the attribute."
    );

    /// <summary>
    /// Reported when a <c>partial union</c> decorated with <c>GenerateOutcomeMembersAttribute</c> would
    /// generate two case types' named <c>TryGet{TypeName}</c> accessor to the same method name, or a case
    /// type's own name collides with an already-reserved generated member name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three shapes reach this diagnostic, all reported by <see cref="OutcomeAccessorCollisionAnalyzer"/> once
    /// per colliding case: two peer case types sharing the same simple name from different namespaces (e.g.
    /// <c>MyApp.Http.NotFound</c> and <c>MyApp.Domain.NotFound</c>); two closed instantiations of the same open
    /// generic case type as peer cases (e.g. <c>ValidationError&lt;string&gt;</c> and
    /// <c>ValidationError&lt;int&gt;</c> would both want <c>TryGetValidationError</c>); and a case type whose
    /// own simple name exactly matches a reserved generated member name (<c>Value</c>, <c>Match</c>,
    /// <c>Switch</c>, <c>ToString</c>, <c>Equals</c>, <c>GetHashCode</c>, <c>TryGetValue</c>,
    /// <c>ActiveCaseType</c>).
    /// </para>
    /// <para>
    /// A case type that is one of the union's own type parameters never participates - it gets no named
    /// accessor in the first place (see <c>OutcomeUnionModel.CaseAccessorNames</c>'s remarks), so it can never
    /// collide with anything by name. The reserved-name shape guards all nine names uniformly, even though only
    /// <c>Value</c> (whose accessor, <c>TryGetValue</c>, literally duplicates the anonymous overload - a real
    /// <c>CS0111</c>) can produce an outright compile error under today's <c>TryGet</c>-prefixed scheme; the
    /// other eight are reserved defensively, so a case type never claims a name this type's own vocabulary
    /// already owns. Always <see cref="DiagnosticSeverity.Error"/>, same posture as
    /// <see cref="TooFewOutcomeCaseTypes"/>: silently emitting an ambiguous overload set (a peer-name collision
    /// is resolvable only by explicit type, never by <c>out var</c>) is worse than refusing to compile,
    /// escalated the same way as every other diagnostic in this file:
    /// <code>
    /// dotnet_diagnostic.INTERLOC012.severity = warning
    /// </code>
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor AccessorNameCollision = new(
        id: "INTERLOC012",
        title: "Colliding TryGet accessor name among a union's case types",
        messageFormat: "'{0}' would generate a colliding 'TryGet{1}' accessor: {2}",
        category: "Interlocutor.Outcomes",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "GenerateOutcomeMembersAttribute generates a named TryGet{TypeName} accessor for every "
            + "concrete case type. Two peer case types that resolve to the same simple name - whether from "
            + "different namespaces, or two closed instantiations of the same open generic type - would "
            + "generate the same accessor name with different, non-var-inferable signatures; a case type whose "
            + "own name matches an already-reserved generated member name would generate a duplicate member "
            + "outright. Rename the colliding case type(s), or use distinct case types."
    );

    /// <summary>
    /// Reported when a <c>partial union</c> decorated with <c>GenerateOutcomeMembersAttribute</c> declares
    /// more than sixteen case types.
    /// </summary>
    /// <remarks>
    /// <see cref="OutcomeMemberEmitter"/>'s generated <c>Match&lt;TResult&gt;</c>/<c>Switch</c> signatures bind
    /// one delegate parameter per case type to the BCL's <c>System.Func&lt;T1..T16, TResult&gt;</c>/
    /// <c>System.Action&lt;T1..T16&gt;</c> families, which stop at sixteen input type parameters - there is no
    /// wider overload to grow into past that point. Left unchecked, a seventeen-or-more-case union would fail
    /// deep inside the generated member body with a confusing "no overload of Func/Action takes this many type
    /// arguments" error rather than a clear message at the declaration site; this diagnostic surfaces the same
    /// ceiling there instead. Always <see cref="DiagnosticSeverity.Error"/>, the same posture as
    /// <see cref="TooFewOutcomeCaseTypes"/>: past sixteen cases, there is no meaningful partial member set to
    /// fall back to - <c>Match</c>/<c>Switch</c> themselves cannot be emitted at all.
    /// </remarks>
    public static readonly DiagnosticDescriptor TooManyOutcomeCaseTypes = new(
        id: "INTERLOC013",
        title: "Attributed union declares more than sixteen case types",
        messageFormat: "'{0}' is decorated with GenerateOutcomeMembersAttribute but declares {1} case type(s) - "
            + "at most sixteen are supported, because Match/Switch bind one delegate parameter per case to the "
            + "BCL's Func<T1..T16,TResult>/Action<T1..T16> delegate families, which top out at sixteen type "
            + "parameters",
        category: "Interlocutor.Outcomes",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "GenerateOutcomeMembersAttribute marks a partial union declaration for source-generated "
            + "Match/Switch/TryGetValue/ToString members. Match<TResult> and Switch bind one delegate parameter "
            + "per case type to System.Func<T1..T16, TResult>/System.Action<T1..T16>, and the BCL defines no "
            + "wider overload of either family, so a union past sixteen case types has no signature Match/Switch "
            + "could ever be emitted with. Reduce the union to sixteen or fewer case types."
    );

    /// <summary>
    /// Reported when a <c>partial union</c> decorated with <c>GenerateOutcomeMembersAttribute</c> declares more
    /// case types than the configured recommended maximum (default six, matching the shipped
    /// <c>Outcome</c>/<c>Outcome&lt;...&gt;</c> family's own ceiling).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="TooManyOutcomeCaseTypes"/> - a hard, unconfigurable ceiling forced by
    /// <c>Func&lt;T1..T16, TResult&gt;</c>/<c>Action&lt;T1..T16&gt;</c>'s own arity limit - this is a style
    /// nudge, not a structural limit: an operation whose response needs this many independently-branchable
    /// cases is usually a sign the underlying type or handler is doing too much, and splitting the operation is
    /// more often the right fix than growing the union further. Default severity is
    /// <see cref="DiagnosticSeverity.Warning"/>, escalated the same way as <see cref="MissingHandler"/>:
    /// <code>
    /// dotnet_diagnostic.INTERLOC014.severity = error
    /// </code>
    /// </para>
    /// <para>
    /// The threshold itself - distinct from severity - is also configurable, via an <c>.editorconfig</c>-style
    /// analyzer option read per-declaring-file:
    /// <code>
    /// interlocutor_max_recommended_outcome_arity = 8
    /// </code>
    /// A deliberate outlier (a payment gateway integration with several genuinely independent decline reasons,
    /// say) dismisses this per-declaration with a targeted <c>#pragma warning disable INTERLOC014</c> /
    /// <c>#pragma warning restore INTERLOC014</c> pair around the union declaration, the same as any other
    /// warning - no bespoke suppression mechanism exists for this diagnostic.
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor RecommendedOutcomeArityExceeded = new(
        id: "INTERLOC014",
        title: "Attributed union declares more cases than the recommended maximum",
        messageFormat: "'{0}' is decorated with GenerateOutcomeMembersAttribute but declares {1} case type(s), "
            + "more than the recommended maximum of {2} - a type or handler needing this many cases is usually "
            + "a sign it's doing too much; consider splitting the operation instead of growing the union further",
        category: "Interlocutor.Outcomes",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "GenerateOutcomeMembersAttribute marks a partial union declaration for source-generated "
            + "Match/Switch/TryGetValue/ToString members. Declaring more case types than the recommended "
            + "maximum (default six, configurable via the interlocutor_max_recommended_outcome_arity "
            + ".editorconfig option) usually signals the underlying type or handler is doing too much. Split "
            + "the operation into more specific requests, or dismiss this warning per-declaration with "
            + "#pragma warning disable/restore INTERLOC014 for a genuine outlier."
    );

    /// <summary>
    /// Reported when a <c>TRequest</c> implements one of the four marker-interface-gated pipeline behaviors'
    /// marker interfaces (<c>IRequiresPolicyRequest</c>, <c>ICancellableRequest</c>, <c>IRateLimitedRequest</c>,
    /// <c>ITimeoutRequest</c>) but its paired <c>TResponse</c> declares no case of the exact type that
    /// behavior needs to construct (<c>Forbidden</c>, <c>Cancelled</c>, <c>RateLimited</c>, <c>Timeout</c>
    /// respectively).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per ADR-0023 ("Pipeline Behavior Case Construction Fails Fast, Never Degrades or Reshapes"), a pipeline
    /// behavior that can't construct the case it needs from a handler's <c>TResponse</c> already fails fast
    /// with a named <see cref="InvalidOperationException"/> at run time, via <c>OutcomeCaseRegistryExtensions.ConstructOrThrow</c> -
    /// this diagnostic is an earlier-feedback layer on top of that same runtime guarantee, not a replacement
    /// for it: it reads what the generator already knows about <c>TRequest</c>'s implemented interfaces and
    /// <c>TResponse</c>'s declared case types, without touching <c>TResponse</c>'s type,
    /// <c>IOutcomeCaseRegistry</c>'s contract, or how any container resolves these behaviors. The match is
    /// exact-type, not "implements the same marker interface the case type happens to implement" - the
    /// underlying runtime construction (<c>UnionCaseFactory.FindCaseConstructor</c>) looks for a constructor
    /// accepting the literal case type, so a response declaring some other type that merely implements
    /// <c>IForbiddenFailureCase</c>/<c>IRateLimitedFailureCase</c>/<c>ITimeoutFailureCase</c> still doesn't
    /// satisfy the behavior that needs the concrete <c>Forbidden</c>/<c>RateLimited</c>/<c>Timeout</c> case.
    /// </para>
    /// <para>
    /// <b>Scope:</b> only these four marker-interface-gated behaviors (<c>AuthorizationBehavior</c>,
    /// <c>CancellationBehavior</c>, <c>RateLimitedBehavior</c>, <c>TimeoutBehavior</c>) are covered.
    /// <c>ValidationBehavior</c>/<c>FluentValidationErrorsBehavior</c> are gated by whether a
    /// <c>FluentValidation.IValidator&lt;TRequest&gt;</c> is actually registered in the built container - a
    /// DI-registration-time fact, not a static marker interface on <c>TRequest</c>, and therefore invisible to
    /// a compile-time analyzer working only from a type's declared shape. Those two behaviors are deliberately
    /// out of scope for this diagnostic; see <see cref="ValidationShapeMismatch"/> for the closest
    /// already-shipped build-time coverage they do get, and a later opt-in startup-validation extension for
    /// the runtime-registered coverage the rest of their surface gets instead.
    /// </para>
    /// <para>
    /// Default severity is <see cref="DiagnosticSeverity.Warning"/>, escalated the same way as
    /// <see cref="MissingHandler"/>:
    /// <code>
    /// dotnet_diagnostic.INTERLOC015.severity = error
    /// </code>
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor PipelineBehaviorCaseFidelityMismatch = new(
        id: "INTERLOC015",
        title: "Pipeline behavior marker interface without a matching response case",
        messageFormat: "'{0}' implements '{1}' but its response type '{2}' has no '{3}' case for {4} to "
            + "construct - add a '{3}' case to the response type, or remove '{1}'",
        category: "Interlocutor.Pipeline",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AuthorizationBehavior, CancellationBehavior, RateLimitedBehavior, and TimeoutBehavior "
            + "each construct a specific outcome case (Forbidden, Cancelled, RateLimited, Timeout "
            + "respectively) via IOutcomeCaseRegistry when their gating marker interface "
            + "(IRequiresPolicyRequest, ICancellableRequest, IRateLimitedRequest, ITimeoutRequest) is "
            + "implemented by TRequest - the response type must declare that exact case, or the behavior "
            + "throws InvalidOperationException at runtime the first time it actually short-circuits. "
            + "ValidationBehavior/FluentValidationErrorsBehavior are gated by DI-registered-validator "
            + "presence, not a static marker interface, and are out of scope for this diagnostic. Add the "
            + "missing case to the response type's declared arity, or remove the marker interface if the "
            + "cross-cutting behavior does not apply to this request."
    );
}
