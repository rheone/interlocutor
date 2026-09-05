namespace Interlocutor.Abstractions.Commands;

using Interlocutor.CommonKernel.ResultTypes;

#pragma warning disable SA1402 // Allowing multiple types of same name with different arity

/// <summary>
/// Marks a command that produces a typed response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">
/// The <see cref="IOutcome"/>-shaped union returned when the command is handled. Commands never
/// return a bare DTO - the response must be (or be wrapped in) an outcome union so callers exhaustively
/// handle every case.
/// </typeparam>
/// <remarks>
/// <typeparamref name="TResponse"/> is a deliberate dispatch marker, not something any member of this
/// interface needs directly - it lets the mediator's generic <c>Send&lt;TResponse&gt;</c> overload select
/// the matching handler and pipeline behaviors at compile time. S2326 flags it as "unused" because no
/// member signature here mentions it, but removing it would erase the compile-time link between a command
/// and its response type entirely.
/// </remarks>
#pragma warning disable S2326
public interface ICommand<TResponse>
    where TResponse : IOutcome;
#pragma warning restore S2326

/// <summary>
/// Marks a command that produces no response data.
/// </summary>
/// <remarks>
/// <para>
/// Use the non-generic <see cref="ICommand"/> for fire-and-forget or side-effect-only
/// operations. The handler produces no data payload - only an outcome, expressed as any
/// <c>Interlocutor.CommonKernel.IOutcome</c>-implementing union (<c>Outcome</c>,
/// <c>Outcome&lt;TSuccess, TFailure&gt;</c>, etc.). The outcome type is chosen by the caller via
/// <see cref="Core.ISender.SendAsync{TResult}(ICommand, CancellationToken)"/>'s type argument, not
/// declared on the command itself.
/// </para>
/// </remarks>
public interface ICommand;

#pragma warning restore SA1402
