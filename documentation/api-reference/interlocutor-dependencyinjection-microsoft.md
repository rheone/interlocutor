# Interlocutor.DependencyInjection.Microsoft API Reference

Every public type in `Interlocutor.DependencyInjection.Microsoft`, grouped by namespace - [DI registration](../glossary.md#di-registration) for Microsoft.Extensions.DependencyInjection.

## Interlocutor.DependencyInjection.Microsoft

### `InterlocutorServiceCollectionExtensions` class

Provides the extension method for registering Interlocutor services with a Microsoft DI `IServiceCollection`.

| Member | Summary |
| --- | --- |
| `public static IServiceCollection AddInterlocutor(IServiceCollection services, Action<InterlocutorBuilder> configure)` | Configures Interlocutor via the container-neutral `InterlocutorBuilder` - the single fluent configuration surface identical across every supported container. Registers the mediator, discovered handlers, and stages unconditionally; registers zero pipeline behaviors unless `configure` adds them - no default pipeline. |

### `PersistenceServiceCollectionExtensions` class

Extension methods for opting a Microsoft DI `IServiceCollection` into Interlocutor's optional persistence abstraction - `IUnitOfWork` and `IDomainRepository`.

| Member | Summary |
| --- | --- |
| `public static IServiceCollection AddInterlocutorRepository<TEntity, TKey, TImplementation>(IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)` | Registers `TImplementation` as the scoped `IDomainRepository` for `TEntity`/`TKey` in this container, built by `implementationFactory`. |
| `public static IServiceCollection AddInterlocutorUnitOfWork<TImplementation>(IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory)` | Registers `TImplementation` as the scoped `IUnitOfWork` for this container, built by `implementationFactory`. |

