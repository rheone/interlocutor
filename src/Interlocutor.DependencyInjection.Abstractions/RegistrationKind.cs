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
    /// A pipeline behavior from the library's recommended default chain (logging, auditing,
    /// authorization, validation, transaction).
    /// </summary>
    Behavior,

    /// <summary>
    /// A default implementation of a service a pipeline behavior depends on (for example,
    /// <c>IAuditSink</c> or <c>IPolicyEvaluator</c>) that a consumer's own registration may override.
    /// </summary>
    Default,
}
