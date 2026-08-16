namespace Interlocutor.DependencyInjection.Abstractions;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Produces the container-neutral registrations for the library's recommended default pipeline
/// chain (logging, auditing, authorization, validation, transaction) and the default
/// implementations those behaviors depend on.
/// </summary>
/// <remarks>
/// This is the single copy of the default-behavior catalog and default-dependency catalog
/// previously duplicated, byte-for-byte, between <c>Interlocutor.DependencyInjection.Microsoft</c>
/// and <c>Interlocutor.DependencyInjection.Autofac</c>. Both packages translate the returned
/// registrations into their own container's primitives.
/// </remarks>
public static class DefaultPipelineRegistrations
{
    /// <summary>
    /// Returns the canonical pipeline behavior chain — logging, then auditing, then authorization,
    /// then validation, then transaction — as <see cref="RegistrationKind.Behavior"/> registrations
    /// in that order.
    /// </summary>
    /// <returns>
    /// One registration per behavior whose type could be resolved. Validation and transaction
    /// behaviors live in optional packages (<c>Interlocutor.FluentValidation</c>,
    /// <c>Interlocutor.EntityFrameworkCore</c>/<c>Interlocutor.NHibernate</c>); when the consuming
    /// application has not referenced them, their entries are omitted rather than failing.
    /// </returns>
    public static IReadOnlyList<InterlocutorRegistration> Behaviors()
    {
        // Try to load and register each behavior. If a type doesn't exist in loaded assemblies,
        // that's ok — the consumer just won't have that behavior (e.g., if FluentValidation isn't referenced).
        Type?[] behaviorTypes =
        [
            // Logging outermost
            typeof(LoggingBehavior<,>),
            // Auditing after logging
            typeof(AuditingBehavior<,>),
            // Authorization before validation
            typeof(AuthorizationBehavior<,>),
            // Validation before transaction
            TryGetType("Interlocutor.FluentValidation.ValidationBehavior`2, Interlocutor.FluentValidation"),
            // Transaction innermost. EF Core's TransactionBehavior<TRequest,TResponse,TContext> is arity 3
            // (it's generic over the DbContext type too); NHibernate's TransactionBehavior<TRequest,TResponse>
            // is arity 2 (ISession isn't generic) — the arities genuinely differ, this isn't a typo to unify.
            TryGetType("Interlocutor.EntityFrameworkCore.TransactionBehavior`3, Interlocutor.EntityFrameworkCore")
                ?? TryGetType("Interlocutor.NHibernate.TransactionBehavior`2, Interlocutor.NHibernate"),
        ];

        return behaviorTypes
            .Where(t => t != null)
            .Select(t => new InterlocutorRegistration(
                typeof(IPipelineBehavior<,>),
                t!,
                RegistrationLifetime.Scoped,
                RegistrationKind.Behavior
            ))
            .ToList();
    }

    /// <summary>
    /// Returns the default implementations that the built-in behaviors depend on — <see cref="IAuditSink"/>,
    /// <see cref="IPolicyEvaluator"/>, and <see cref="IRequestContextAccessor"/> — as
    /// <see cref="RegistrationKind.Default"/> registrations a consumer's own registration may override.
    /// </summary>
    /// <returns>The default dependency registrations.</returns>
    public static IReadOnlyList<InterlocutorRegistration> Dependencies() =>
        [
            new InterlocutorRegistration(
                typeof(IAuditSink),
                typeof(LoggingAuditSink),
                RegistrationLifetime.Scoped,
                RegistrationKind.Default
            ),
            new InterlocutorRegistration(
                typeof(IPolicyEvaluator),
                typeof(DefaultPolicyEvaluator),
                RegistrationLifetime.Scoped,
                RegistrationKind.Default
            ),
            new InterlocutorRegistration(
                typeof(IRequestContextAccessor),
                typeof(AmbientRequestContextAccessor),
                RegistrationLifetime.Scoped,
                RegistrationKind.Default
            ),
        ];

    /// <summary>
    /// Attempts to resolve a behavior type by its assembly-qualified name, returning <see langword="null"/>
    /// if the type (or the assembly it lives in) is not loaded.
    /// </summary>
    /// <param name="typeName">The assembly-qualified or partially-qualified type name.</param>
    /// <returns>The resolved <see cref="Type"/>, or <see langword="null"/> if it could not be found.</returns>
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
