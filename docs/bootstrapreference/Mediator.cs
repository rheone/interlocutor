using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Interlocutor.Mediation;

/// <summary>
/// Non-generic so it can be cached in a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// keyed by the request's runtime <see cref="Type"/>. The reflection needed
/// to bridge "runtime type" to "closed generic type" happens exactly once
/// per distinct request type — see <see cref="Mediator"/> — not once per
/// call. Every method inside a concrete wrapper below is ordinary,
/// compiler-checked C#; no <c>dynamic</c> is used anywhere in dispatch.
///
/// <para>
/// The <c>Handle</c> implementations below are also where the "nesting
/// doll" (Russian-doll / onion) pipeline is assembled: start with the
/// innermost function — a call straight to the handler — then fold each
/// registered <c>IPipelineBehavior</c> around it. Folding in <em>reverse</em>
/// registration order means the FIRST-registered behavior ends up as the
/// OUTERMOST call. For behaviors registered as
/// <c>[Logging, Auditing, Validation, Authorization, Transaction]</c>, the
/// resulting call graph is exactly what hand-written nesting would produce:
/// <code>
/// return await logging.Handle(request, ct, () =&gt;
///     auditing.Handle(request, ct, () =&gt;
///         validation.Handle(request, ct, () =&gt;
///             authorization.Handle(request, ct, () =&gt;
///                 transaction.Handle(request, ct, () =&gt;
///                     handler.Handle(request, ct))))));
/// </code>
/// </para>
/// </summary>
internal abstract class HandlerWrapper<TResponse>
{
    public abstract Task<Result<TResponse>> Handle(object request, IServiceProvider services, CancellationToken ct);
}

/// <summary>
/// Statically typed dispatch for one concrete query type. Constructed once
/// (via <see cref="Activator.CreateInstance(Type)"/>) and cached by
/// <see cref="Mediator"/>; every call after construction is fully typed —
/// <c>services.GetRequiredService&lt;IQueryHandler&lt;TQuery,TResponse&gt;&gt;()</c>
/// and <c>services.GetServices&lt;IPipelineBehavior&lt;TQuery,TResponse&gt;&gt;()</c>
/// are compiler-verified against the real handler/behavior contracts.
/// </summary>
internal sealed class QueryHandlerWrapper<TQuery, TResponse> : HandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override async Task<Result<TResponse>> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TQuery)request;
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

        Func<Task<Result<TResponse>>> pipeline = () => handler.Handle(typedRequest, ct);

        foreach (var behavior in services.GetServices<IPipelineBehavior<TQuery, TResponse>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, ct, next);
        }

        return await pipeline();
    }
}

/// <summary>The command-returning-a-value equivalent of <see cref="QueryHandlerWrapper{TQuery,TResponse}"/>.</summary>
internal sealed class CommandHandlerWrapper<TCommand, TResponse> : HandlerWrapper<TResponse>
    where TCommand : ICommand<TResponse>
{
    public override async Task<Result<TResponse>> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TResponse>>();

        Func<Task<Result<TResponse>>> pipeline = () => handler.Handle(typedRequest, ct);

        foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, TResponse>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, ct, next);
        }

        return await pipeline();
    }
}

/// <summary>
/// The default <see cref="IMediator"/> implementation. This is the only
/// class in the whole family of packages that holds an
/// <see cref="IServiceProvider"/> directly — every handler and behavior
/// downstream of it uses ordinary constructor injection. The provider is
/// used for exactly two lookups per request: which handler and which
/// ordered behaviors correspond to the request's runtime type. Nothing
/// else in this class, or anywhere else in the library, calls
/// <see cref="IServiceProvider"/> to fetch arbitrary application resources
/// — see the library README §"Service locator usage" for why that
/// distinction matters.
/// </summary>
public sealed class Mediator(IServiceProvider services) : IMediator
{
    // One wrapper instance per distinct request TYPE, not per call. Reflection
    // (Activator.CreateInstance) runs once the first time a given request type
    // is dispatched; every subsequent Send for that type reuses the cached,
    // statically-typed wrapper.
    private static readonly ConcurrentDictionary<Type, object> QueryWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> CommandWrappers = new();

    /// <inheritdoc />
    public Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    {
        var wrapper =
            (HandlerWrapper<TResponse>)
                QueryWrappers.GetOrAdd(
                    query.GetType(),
                    requestType =>
                        Activator.CreateInstance(
                            typeof(QueryHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse))
                        )!
                );

        return wrapper.Handle(query, services, ct);
    }

    /// <inheritdoc />
    public Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        var wrapper =
            (HandlerWrapper<TResponse>)
                CommandWrappers.GetOrAdd(
                    command.GetType(),
                    requestType =>
                        Activator.CreateInstance(
                            typeof(CommandHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse))
                        )!
                );

        return wrapper.Handle(command, services, ct);
    }

    /// <inheritdoc />
    public async Task<Result> Send(ICommand command, CancellationToken ct = default)
    {
        // Simplified for the no-payload case — a full implementation mirrors
        // the wrapper pattern above with a Result-returning (not Result<T>)
        // HandlerWrapper hierarchy. Omitted here since the technique is
        // identical; the point of this file is the dynamic -> typed change.
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = services.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand>.Handle))!;
        return await (Task<Result>)handleMethod.Invoke(handler, [command, ct])!;
    }
}
