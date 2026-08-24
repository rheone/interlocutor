namespace Interlocutor.DependencyInjection.Microsoft;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Persistence.Abstractions;

/// <summary>
/// Extension methods for opting a Microsoft DI <see cref="IServiceCollection"/> into Interlocutor's optional
/// persistence abstraction - <see cref="IUnitOfWork"/> and <see cref="IDomainRepository{TEntity, TKey}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This surface is separate from, and never called by, <see cref="InterlocutorServiceCollectionExtensions.AddInterlocutor"/> -
/// the persistence abstraction is optional per ADR-0017, so nothing here runs unless a consumer calls one of
/// these methods explicitly, consistent with ADR-0009's "no default pipeline" philosophy carried over to
/// this optional layer.
/// </para>
/// <para>
/// Both methods take a factory delegate rather than a bare implementation type parameter: an EF Core or
/// NHibernate repository's constructor needs a key-selector expression (<c>e =&gt; e.Id</c>) that the
/// container cannot synthesize on its own, so the consumer builds the concrete instance themselves. This
/// also means this package takes no dependency on <c>Interlocutor.Persistence.EntityFrameworkCore</c> or
/// <c>Interlocutor.Persistence.NHibernate</c> - only on the zero-ORM <c>Interlocutor.Persistence.Abstractions</c> -
/// so a consumer who never calls these methods never pulls either store package in transitively.
/// </para>
/// </remarks>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TImplementation"/> as the scoped <see cref="IUnitOfWork"/> for this
    /// container, built by <paramref name="implementationFactory"/>.
    /// </summary>
    /// <typeparam name="TImplementation">The concrete <see cref="IUnitOfWork"/> implementation to register - for example, an EF Core <c>EntityFrameworkUnitOfWork&lt;TContext&gt;</c> or an NHibernate <c>NHibernateUnitOfWork</c>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register into.</param>
    /// <param name="implementationFactory">Builds the <typeparamref name="TImplementation"/> instance from the resolving <see cref="IServiceProvider"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// Registered scoped, matching the underlying <c>DbContext</c>/<c>ISession</c>'s own registration
    /// lifetime, so this unit of work shares the same scoped session as
    /// <c>Interlocutor.EntityFrameworkCore.TransactionBehavior</c> or
    /// <c>Interlocutor.NHibernate.TransactionBehavior</c> for the same dispatch.
    /// </remarks>
    public static IServiceCollection AddInterlocutorUnitOfWork<TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, TImplementation> implementationFactory
    )
        where TImplementation : class, IUnitOfWork
    {
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddScoped<IUnitOfWork>(implementationFactory);
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TImplementation"/> as the scoped <see cref="IDomainRepository{TEntity, TKey}"/>
    /// for <typeparamref name="TEntity"/>/<typeparamref name="TKey"/> in this container, built by
    /// <paramref name="implementationFactory"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type the repository reads and writes.</typeparam>
    /// <typeparam name="TKey">The entity's key type.</typeparam>
    /// <typeparam name="TImplementation">The concrete <see cref="IDomainRepository{TEntity, TKey}"/> implementation to register - for example, an EF Core <c>EntityFrameworkRepository&lt;TEntity,TKey,TContext&gt;</c> or an NHibernate <c>NHibernateRepository&lt;TEntity,TKey&gt;</c>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register into.</param>
    /// <param name="implementationFactory">Builds the <typeparamref name="TImplementation"/> instance from the resolving <see cref="IServiceProvider"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// Called once per entity type a consumer wants a repository for - there is no open-generic registration
    /// that closes <see cref="IDomainRepository{TEntity, TKey}"/> automatically, because the concrete
    /// implementation types take an additional key-selector expression argument no container can synthesize.
    /// Registered scoped, matching the underlying <c>DbContext</c>/<c>ISession</c>'s own registration
    /// lifetime.
    /// </remarks>
    public static IServiceCollection AddInterlocutorRepository<TEntity, TKey, TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, TImplementation> implementationFactory
    )
        where TImplementation : class, IDomainRepository<TEntity, TKey>
    {
        ArgumentNullException.ThrowIfNull(implementationFactory);

        services.AddScoped<IDomainRepository<TEntity, TKey>>(implementationFactory);
        return services;
    }
}
