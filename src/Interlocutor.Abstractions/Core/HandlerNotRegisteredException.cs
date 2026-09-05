namespace Interlocutor.Abstractions.Core;

/// <summary>
/// Thrown by runtime-typed dispatch (<see cref="ISender.SendAsync(object, CancellationToken)"/>) when the
/// request's own type is a recognized request shape (it closes <c>IQuery&lt;TResponse&gt;</c>,
/// <c>ICommand&lt;TResponse&gt;</c>, or implements <c>ICommand</c>) but no handler for it is registered in
/// the service provider.
/// </summary>
/// <remarks>
/// <para>
/// This is distinct from <see cref="UnsupportedRequestTypeException"/>, which is thrown when the object
/// passed to runtime-typed dispatch is not a request at all. A missing handler is a configuration problem
/// (register the handler); an unsupported type is a caller bug (don't dispatch it as a request).
/// </para>
/// <para>
/// Typed dispatch (<see cref="ISender.SendAsync{TResponse}(Queries.IQuery{TResponse}, CancellationToken)"/> and
/// its command overloads) surfaces a missing handler as the DI container's own generic
/// <see cref="InvalidOperationException"/> from <c>GetRequiredService</c>. Runtime-typed dispatch checks
/// registration explicitly beforehand so infrastructure callers (outbox/inbox/queue consumers) get a
/// specific, named exception identifying the request type instead.
/// </para>
/// </remarks>
// RCS1194 wants the standard parameterless/message/message+innerException/serialization constructor set. This
// exception's RequestType is load-bearing - the whole point of catching it is to read which type had no handler
// - so a parameterless or message-only constructor would produce a useless exception missing the one thing
// callers need. Suppressed here rather than adding constructors that would only confuse callers into using them.
#pragma warning disable RCS1194
public sealed class HandlerNotRegisteredException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="HandlerNotRegisteredException"/> class.</summary>
    /// <param name="requestType">The runtime type of the request that has no registered handler.</param>
    /// <param name="handlerType">
    /// The specific closed handler interface (e.g. <c>IQueryHandler&lt;MyQuery, MyResult&gt;</c>) that was
    /// expected to be registered, or <see langword="null"/> when no single closed handler interface could be
    /// determined (e.g. a unit command, whose result type is chosen by the caller rather than declared on
    /// the command).
    /// </param>
    public HandlerNotRegisteredException(Type requestType, Type? handlerType = null)
        : base(BuildMessage(requestType, handlerType))
    {
        this.RequestType = requestType;
        this.HandlerType = handlerType;
    }

    /// <summary>Gets the runtime type of the request that has no registered handler.</summary>
    /// <value>The runtime type of the request that has no registered handler.</value>
    public Type RequestType { get; }

    /// <summary>
    /// Gets the specific closed handler interface that was expected to be registered, or
    /// <see langword="null"/> when no single closed handler interface could be determined.
    /// </summary>
    /// <value>
    /// The specific closed handler interface that was expected to be registered, or
    /// <see langword="null"/> when no single closed handler interface could be determined.
    /// </value>
    public Type? HandlerType { get; }

    private static string BuildMessage(Type requestType, Type? handlerType) =>
        handlerType is not null
            ? $"No handler is registered for request type '{requestType.FullName}'. Expected an implementation of '{handlerType.FullName}' to be registered in the service provider."
            : $"No handler is registered for request type '{requestType.FullName}'.";
}
#pragma warning restore RCS1194
