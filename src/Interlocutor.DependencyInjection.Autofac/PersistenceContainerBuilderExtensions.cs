namespace Interlocutor.DependencyInjection.Autofac;

using global::Autofac;
using Interlocutor.Persistence.Abstractions;

/// <summary>
/// Extension methods for opting an Autofac <see cref="ContainerBuilder"/> into Interlocutor's optional
/// persistence abstraction - <see cref="IUnitOfWork"/> and <see cref="IDomainRepository{TEntity, TKey}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This surface is separate from, and never called by, <see cref="InterlocutorAutofacExtensions.AddInterlocutor"/> -
/// the persistence abstraction is optional per ADR-0017, so nothing here runs unless a consumer calls one of
/// these methods explicitly, consistent with ADR-0009's "no default pipeline" philosophy carried over to
/// this optional layer.
/// </para>
/// <para>
/// Both methods take a factory delegate over Autofac's own <see cref="IComponentContext"/> rather than a
/// bare implementation type parameter: an EF Core or NHibernate repository's constructor needs a
/// key-selector expression (<c>e =&gt; e.Id</c>) that the container cannot synthesize on its own, so the
/// consumer builds the concrete instance themselves. This also means this package takes no dependency on
/// <c>Interlocutor.Persistence.EntityFrameworkCore</c> or <c>Interlocutor.Persistence.NHibernate</c> - only
/// on the zero-ORM <c>Interlocutor.Persistence.Abstractions</c> - so a consumer who never calls these
/// methods never pulls either store package in transitively.
/// </para>
/// </remarks>
public static class PersistenceContainerBuilderExtensions
{
    /// <summary>
    /// Registers a scoped (<c>InstancePerLifetimeScope</c>) <see cref="IUnitOfWork"/>, built by
    /// <paramref name="implementationFactory"/>.
    /// </summary>
    /// <typeparam name="TImplementation">The concrete <see cref="IUnitOfWork"/> implementation to register - for example, an EF Core <c>EntityFrameworkUnitOfWork&lt;TContext&gt;</c> or an NHibernate <c>NHibernateUnitOfWork</c>.</typeparam>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register into.</param>
    /// <param name="implementationFactory">Builds the <typeparamref name="TImplementation"/> instance from the resolving <see cref="IComponentContext"/>.</param>
    /// <returns>The same <see cref="ContainerBuilder"/> instance for chaining.</returns>
    /// <remarks>
    /// <c>InstancePerLifetimeScope</c> matches the underlying <c>DbContext</c>/<c>ISession</c>'s own
    /// registration lifetime, so this unit of work shares the same scoped session as
    /// the EF Core or NHibernate <c>UnitOfWorkBehavior</c>
    /// (<c>Interlocutor.Persistence.EntityFrameworkCore</c>/<c>.NHibernate</c>) for the same dispatch.
    /// </remarks>
    public static ContainerBuilder AddInterlocutorUnitOfWork<TImplementation>(
        this ContainerBuilder builder,
        Func<IComponentContext, TImplementation> implementationFactory
    )
        where TImplementation : notnull, IUnitOfWork
    {
        ArgumentNullException.ThrowIfNull(implementationFactory);

        builder.Register(implementationFactory).As<IUnitOfWork>().InstancePerLifetimeScope();
        return builder;
    }

    /// <summary>
    /// Registers a scoped (<c>InstancePerLifetimeScope</c>) <see cref="IDomainRepository{TEntity, TKey}"/>
    /// for <typeparamref name="TEntity"/>/<typeparamref name="TKey"/>, built by <paramref name="implementationFactory"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type the repository reads and writes.</typeparam>
    /// <typeparam name="TKey">The entity's key type.</typeparam>
    /// <typeparam name="TImplementation">The concrete <see cref="IDomainRepository{TEntity, TKey}"/> implementation to register - for example, an EF Core <c>EntityFrameworkRepository&lt;TEntity,TKey,TContext&gt;</c> or an NHibernate <c>NHibernateRepository&lt;TEntity,TKey&gt;</c>.</typeparam>
    /// <param name="builder">The Autofac <see cref="ContainerBuilder"/> to register into.</param>
    /// <param name="implementationFactory">Builds the <typeparamref name="TImplementation"/> instance from the resolving <see cref="IComponentContext"/>.</param>
    /// <returns>The same <see cref="ContainerBuilder"/> instance for chaining.</returns>
    /// <remarks>
    /// Called once per entity type a consumer wants a repository for - there is no open-generic registration
    /// that closes <see cref="IDomainRepository{TEntity, TKey}"/> automatically, because the concrete
    /// implementation types take an additional key-selector expression argument no container can synthesize.
    /// <c>InstancePerLifetimeScope</c> matches the underlying <c>DbContext</c>/<c>ISession</c>'s own
    /// registration lifetime.
    /// </remarks>
    public static ContainerBuilder AddInterlocutorRepository<TEntity, TKey, TImplementation>(
        this ContainerBuilder builder,
        Func<IComponentContext, TImplementation> implementationFactory
    )
        where TImplementation : notnull, IDomainRepository<TEntity, TKey>
    {
        ArgumentNullException.ThrowIfNull(implementationFactory);

        builder.Register(implementationFactory).As<IDomainRepository<TEntity, TKey>>().InstancePerLifetimeScope();
        return builder;
    }
}
