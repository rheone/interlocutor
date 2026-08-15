namespace Interlocutor.Abstractions.Core;

/// <summary>
/// Thrown by runtime-typed dispatch (<see cref="ISender.Send(object, CancellationToken)"/>) when the given
/// object does not implement any recognized request contract — it closes neither
/// <c>IQuery&lt;TResponse&gt;</c> nor <c>ICommand&lt;TResponse&gt;</c>, and does not implement <c>ICommand</c>.
/// </summary>
/// <remarks>
/// This is distinct from <see cref="HandlerNotRegisteredException"/>, which is thrown when the object is a
/// recognized request shape but simply has no handler registered. This exception means the object was never
/// a request to begin with — a deserialization or caller bug, not a missing DI registration.
/// </remarks>
// RCS1194 wants the standard parameterless/message/message+innerException/serialization constructor set. This
// exception's RequestType is load-bearing — see HandlerNotRegisteredException's identical suppression note.
#pragma warning disable RCS1194
public sealed class UnsupportedRequestTypeException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="UnsupportedRequestTypeException"/> class.</summary>
    /// <param name="requestType">The runtime type of the object that is not a recognized request shape.</param>
    public UnsupportedRequestTypeException(Type requestType)
        : base(
            $"'{requestType.FullName}' cannot be dispatched: it does not implement IQuery<TResponse>, "
                + "ICommand<TResponse>, or ICommand."
        )
    {
        this.RequestType = requestType;
    }

    /// <summary>Gets the runtime type of the object that is not a recognized request shape.</summary>
    public Type RequestType { get; }
}
#pragma warning restore RCS1194
