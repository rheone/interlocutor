# Interlocutor.DependencyInjection.Autofac API Reference

Every public type in `Interlocutor.DependencyInjection.Autofac`, grouped by namespace - [DI registration](../glossary.md#di-registration) for Autofac.

## Interlocutor.DependencyInjection.Autofac

### `InterlocutorAutofacExtensions` class

Provides the extension method for registering Interlocutor services with an Autofac `ContainerBuilder`.

| Member | Summary |
| --- | --- |
| `public static ContainerBuilder AddInterlocutor(ContainerBuilder builder, Action<InterlocutorBuilder> configure)` | Configures Interlocutor via the container-neutral `InterlocutorBuilder` - the single fluent configuration surface identical across every supported container. Registers the mediator, discovered handlers, and stages unconditionally; registers zero pipeline behaviors unless `configure` adds them - no default pipeline. |

### `PersistenceContainerBuilderExtensions` class

Extension methods for opting an Autofac `ContainerBuilder` into Interlocutor's optional persistence abstraction - `IUnitOfWork` and `IDomainRepository`.

| Member | Summary |
| --- | --- |
| `public static ContainerBuilder AddInterlocutorRepository<TEntity, TKey, TImplementation>(ContainerBuilder builder, Func<IComponentContext, TImplementation> implementationFactory)` | Registers a scoped (InstancePerLifetimeScope) `IDomainRepository` for `TEntity`/`TKey`, built by `implementationFactory`. |
| `public static ContainerBuilder AddInterlocutorUnitOfWork<TImplementation>(ContainerBuilder builder, Func<IComponentContext, TImplementation> implementationFactory)` | Registers a scoped (InstancePerLifetimeScope) `IUnitOfWork`, built by `implementationFactory`. |

