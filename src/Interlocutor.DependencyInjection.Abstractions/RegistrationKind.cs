namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Discriminates what role an <see cref="InterlocutorRegistration"/> plays, so a translator can
/// apply container-specific policy (such as override precedence) per role without inspecting types.
/// </summary>
public enum RegistrationKind
{
    /// <summary>
    /// A handler discovered by scanning one or more assemblies for implementations of the
    /// mediator's handler contracts (query, command, void command, or notification handler).
    /// </summary>
    Handler,

    /// <summary>
    /// A pipeline behavior registered against the request/response pipeline - one of the library's
    /// built-in behaviors (logging, auditing, authorization, validation, transaction) or a consumer's
    /// own, added via the builder's general <c>AddBehavior</c> escape hatch. Like <see cref="Handler"/>
    /// and <see cref="Stage"/>, a behavior carries no override precedence - it is translated with add,
    /// not try-add, semantics, and the same behavior type may be added more than once.
    /// </summary>
    Behavior,

    /// <summary>
    /// A default implementation of a service a pipeline behavior depends on (for example,
    /// <c>IAuditSink</c> or <c>IPolicyEvaluator</c>) that a consumer's own registration may override.
    /// </summary>
    /// <remarks>
    /// Translated with try-add semantics by both container translators: a consumer's own registration
    /// of the same service type - made either before or after the builder configures the container -
    /// always wins. See <see cref="Handler"/> and <see cref="Stage"/> for the contrasting always-add kinds.
    /// </remarks>
    Default,

    /// <summary>
    /// An <c>IPipelineStage&lt;,&gt;</c> implementation registered directly (not discovered by
    /// assembly scanning) via the builder's stage-registration methods. Like <see cref="Handler"/>,
    /// a stage carries no override precedence - it is translated with add, not try-add, semantics.
    /// </summary>
    Stage,

    /// <summary>
    /// The generator-emitted <c>IGeneratedRequestDispatcher</c> implementation - a single, compilation-wide
    /// singleton registered alongside <see cref="Handler"/> entries whenever a compilation's generated
    /// registrations are added via <c>InterlocutorBuilder.AddGeneratedRegistrations</c>. Like
    /// <see cref="Handler"/> and <see cref="Stage"/>, it carries no override precedence.
    /// </summary>
    Dispatcher,
}
