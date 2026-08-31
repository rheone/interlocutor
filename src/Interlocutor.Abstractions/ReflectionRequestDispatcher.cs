namespace Interlocutor.Abstractions;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// The runtime-typed dispatch fallback <see cref="Mediator"/> delegates to when no
/// <see cref="IGeneratedRequestDispatcher"/> is registered - the explicitly opted-in runtime-scanning path used
/// when the generator never ran over the compilation, or its output was never passed to
/// <c>InterlocutorBuilder.AddGeneratedRegistrations</c>. Resolves and invokes the correct strongly-typed
/// <c>Send</c> overload for a request's runtime type, via reflection, rather than duplicating the
/// pipeline-composition logic each overload already carries.
/// </summary>
/// <remarks>
/// Kept as its own internal type, separate from <see cref="Mediator"/>'s typed dispatch, activity tracing, and
/// generated-dispatcher routing, since a future generator-backed implementation replaces this fallback entirely
/// (see <see cref="ResolveVoidCommandResultType"/>'s remarks) without touching any of those other concerns.
/// </remarks>
internal static class ReflectionRequestDispatcher
{
    // Reuses the strongly-typed Send<TResponse> overloads on ISender via reflection, rather than duplicating
    // pipeline-composition logic. These MethodInfo handles are resolved once (each generic method definition is
    // found by matching its first parameter's open generic shape) and reused for every call, since
    // MakeGenericMethod on an already-resolved MethodInfo is cheap relative to re-scanning the type's methods.
    private static readonly MethodInfo QuerySendMethod = ResolveSendMethod(typeof(IQuery<>));
    private static readonly MethodInfo CommandSendMethod = ResolveSendMethod(typeof(ICommand<>));
    private static readonly MethodInfo VoidCommandSendMethod = ResolveSendMethod(typeof(ICommand));

    // A unit command (ICommand) does not declare its own response type - the caller picks it via Send<TResult>'s
    // type argument. Runtime-typed dispatch has no caller-supplied type argument, so it discovers the response
    // type by scanning loaded assemblies for a registered IVoidCommandHandler<TCommand, TResult> closing over the
    // concrete command type. This is the expensive part of the reflection fallback; results are cached per command
    // type since the answer cannot change without reloading assemblies.
    private static readonly ConcurrentDictionary<Type, Type?> VoidCommandResultTypes = new();

    /// <summary>
    /// Resolves and invokes the correct strongly-typed <c>Send</c> overload for <paramref name="request"/>'s
    /// runtime type, via reflection.
    /// </summary>
    /// <param name="mediator">The mediator instance to invoke the resolved <c>Send</c> overload on.</param>
    /// <param name="services">The service provider used to check handler registration before dispatching.</param>
    /// <param name="request">The request to dispatch, by its runtime type.</param>
    /// <param name="ct">A token that may be used to cancel the operation.</param>
    /// <returns>The dispatched request's outcome.</returns>
    [RequiresUnreferencedCode(
        "The runtime-typed dispatch fallback reflects over runtime types and is only reached when no IGeneratedRequestDispatcher "
            + "is registered (Interlocutor.Generator did not run, or its output was never passed to "
            + "AddGeneratedRegistrations) - see documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "The runtime-typed dispatch fallback closes generic types/methods reflectively - see documentation/trim-and-aot-support.md."
    )]
    public static async Task<IOutcome> DispatchAsync(
        Mediator mediator,
        IServiceProvider services,
        object request,
        CancellationToken ct
    )
    {
        var requestType = request.GetType();

        var queryInterface = FindClosedInterface(requestType, typeof(IQuery<>));
        if (queryInterface is not null)
        {
            var responseType = queryInterface.GetGenericArguments()[0];
            EnsureHandlerRegistered(services, requestType, typeof(IQueryHandler<,>).MakeGenericType(requestType, responseType));
            return await InvokeSendAsync(QuerySendMethod, mediator, responseType, request, ct).ConfigureAwait(false);
        }

        var commandInterface = FindClosedInterface(requestType, typeof(ICommand<>));
        if (commandInterface is not null)
        {
            var responseType = commandInterface.GetGenericArguments()[0];
            EnsureHandlerRegistered(
                services,
                requestType,
                typeof(ICommandHandler<,>).MakeGenericType(requestType, responseType)
            );
            return await InvokeSendAsync(CommandSendMethod, mediator, responseType, request, ct).ConfigureAwait(false);
        }

        if (request is ICommand)
        {
            // A unit command doesn't declare its own response type - Send<TResult>'s caller-supplied type
            // argument picks it. Runtime-typed dispatch has no caller-supplied type argument, so the response
            // type is discovered by scanning loaded assemblies for a registered IVoidCommandHandler<TCommand,
            // TResult> closing over this concrete command type (cached per command type - see the field doc).
            var resultType = VoidCommandResultTypes.GetOrAdd(requestType, ResolveVoidCommandResultType);
            if (resultType is null)
            {
                throw new HandlerNotRegisteredException(requestType);
            }

            EnsureHandlerRegistered(
                services,
                requestType,
                typeof(IVoidCommandHandler<,>).MakeGenericType(requestType, resultType)
            );
            return await InvokeSendAsync(VoidCommandSendMethod, mediator, resultType, request, ct).ConfigureAwait(false);
        }

        throw new UnsupportedRequestTypeException(requestType);
    }

    /// <summary>
    /// Resolves the <see cref="MethodInfo"/> for one of <see cref="ISender"/>'s generic <c>Send</c> overloads by
    /// matching its first parameter's shape, so runtime-typed dispatch can invoke it reflectively without
    /// duplicating the pipeline-composition logic each overload already carries.
    /// </summary>
    /// <param name="requestParameterType">
    /// The open generic interface (e.g. <c>typeof(IQuery&lt;&gt;)</c>) or, for the unit-command overload, the
    /// non-generic <see cref="ICommand"/> type, identifying which overload's first parameter to match.
    /// </param>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "Enumerating ISender's own methods to find one by signature shape is not itself a call "
            + "to the RequiresUnreferencedCode-annotated Mediator.Send(object,...) overload; the resolved MethodInfo "
            + "is only ever the strongly-typed Send<TResponse> overloads DispatchAsync invokes, which carries its "
            + "own matching annotation. See documentation/trim-and-aot-support.md."
    )]
    private static MethodInfo ResolveSendMethod(Type requestParameterType) =>
        typeof(ISender)
            .GetMethods()
            .Single(method =>
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    return false;
                }

                var parameterType = parameters[0].ParameterType;
                return requestParameterType.IsGenericTypeDefinition
                    ? parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == requestParameterType
                    : parameterType == requestParameterType;
            });

    /// <summary>
    /// Invokes a resolved generic <c>Send&lt;TResponse&gt;</c> overload reflectively, awaits it, and returns its
    /// result as the base outcome interface - the reflection stand-in for the compile-time generic type
    /// argument a static caller would normally supply.
    /// </summary>
    [RequiresUnreferencedCode(
        "The runtime-typed dispatch fallback reflects over runtime types and is only reached when no IGeneratedRequestDispatcher "
            + "is registered (Interlocutor.Generator did not run, or its output was never passed to "
            + "AddGeneratedRegistrations) - see documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "The runtime-typed dispatch fallback closes generic types/methods reflectively - see documentation/trim-and-aot-support.md."
    )]
    private static async Task<IOutcome> InvokeSendAsync(
        MethodInfo openGenericSendMethod,
        Mediator mediator,
        Type responseType,
        object request,
        CancellationToken ct
    )
    {
        var closedMethod = openGenericSendMethod.MakeGenericMethod(responseType);
        var task = (Task)closedMethod.Invoke(mediator, [request, ct])!;
        await task.ConfigureAwait(false);

        // Reflectively read Task<TResponse>.Result rather than casting to a statically-typed Task<TResponse> -
        // TResponse is only known at this point as a runtime Type, not a compile-time generic argument. Result
        // is safe to read unconditionally here: the preceding await already completed (or faulted/threw out of)
        // the task, so a completed Task<T> always has a non-null Result property to reflect.
        var resultProperty = task.GetType().GetProperty("Result")!;
        return (IOutcome)resultProperty.GetValue(task)!;
    }

    /// <summary>
    /// Finds the closed generic interface matching <paramref name="openGenericInterface"/> that
    /// <paramref name="requestType"/> implements, or <see langword="null"/> if it implements none.
    /// </summary>
    [RequiresUnreferencedCode(
        "The runtime-typed dispatch fallback reflects over runtime types and is only reached when no IGeneratedRequestDispatcher "
            + "is registered (Interlocutor.Generator did not run, or its output was never passed to "
            + "AddGeneratedRegistrations) - see documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "The runtime-typed dispatch fallback closes generic types/methods reflectively - see documentation/trim-and-aot-support.md."
    )]
    private static Type? FindClosedInterface(Type requestType, Type openGenericInterface) =>
        requestType
            .GetInterfaces()
            .FirstOrDefault(candidate =>
                candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openGenericInterface
            );

    /// <summary>
    /// Throws <see cref="HandlerNotRegisteredException"/> if <paramref name="handlerType"/> has no registration
    /// in <paramref name="services"/>. Runtime-typed dispatch checks this explicitly, rather than letting a
    /// missing handler surface as the DI container's own generic exception from deep inside a handler wrapper.
    /// </summary>
    private static void EnsureHandlerRegistered(IServiceProvider services, Type requestType, Type handlerType)
    {
        if (services.GetService(handlerType) is null)
        {
            throw new HandlerNotRegisteredException(requestType, handlerType);
        }
    }

    /// <summary>
    /// Scans loaded assemblies for a concrete type implementing <c>IVoidCommandHandler&lt;TCommand, TResult&gt;</c>
    /// closed over <paramref name="commandType"/>, returning the <c>TResult</c> it was implemented against, or
    /// <see langword="null"/> if no such implementation is loaded.
    /// </summary>
    /// <remarks>
    /// This is the expensive part of the reflection-based fallback (see the <see cref="VoidCommandResultTypes"/>
    /// field doc) - a future generator-backed implementation replaces it with a compiled lookup, without
    /// changing <see cref="Mediator.Send(object, CancellationToken)"/>'s contract.
    /// </remarks>
    [RequiresUnreferencedCode(
        "The runtime-typed dispatch fallback reflects over runtime types and is only reached when no IGeneratedRequestDispatcher "
            + "is registered (Interlocutor.Generator did not run, or its output was never passed to "
            + "AddGeneratedRegistrations) - see documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "The runtime-typed dispatch fallback closes generic types/methods reflectively - see documentation/trim-and-aot-support.md."
    )]
    private static Type? ResolveVoidCommandResultType(Type commandType)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type is not null).Cast<Type>().ToArray();
            }

            foreach (var candidate in types)
            {
                if (candidate.IsAbstract || candidate.IsInterface)
                {
                    continue;
                }

                foreach (var candidateInterface in candidate.GetInterfaces())
                {
                    if (
                        !candidateInterface.IsGenericType
                        || candidateInterface.GetGenericTypeDefinition() != typeof(IVoidCommandHandler<,>)
                    )
                    {
                        continue;
                    }

                    var genericArguments = candidateInterface.GetGenericArguments();
                    if (genericArguments[0] == commandType)
                    {
                        return genericArguments[1];
                    }
                }
            }
        }

        return null;
    }
}
