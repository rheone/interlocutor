namespace Interlocutor.Abstractions.Commands;

using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Marks a command that produces a typed response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">
/// The <see cref="IOperationOutcome"/>-shaped union returned when the command is handled. Commands never
/// return a bare DTO - the response must be (or be wrapped in) an outcome union so callers exhaustively
/// handle every case.
/// </typeparam>
public interface ICommand<TResponse>
    where TResponse : IOperationOutcome;

/// <summary>
/// Marks a command that produces no response data.
/// </summary>
/// <remarks>
/// <para>
/// Use the non-generic <see cref="ICommand"/> for fire-and-forget or side-effect-only
/// operations. The handler produces no data payload - only an outcome, expressed as any
/// <c>Interlocutor.CommonKernel.IOperationOutcome</c>-implementing union (<c>OperationOutcome</c>,
/// <c>OperationOutcome&lt;TSuccess, TFailure&gt;</c>, etc.). The outcome type is chosen by the caller via
/// <see cref="Core.ISender.Send{TResult}(ICommand, CancellationToken)"/>'s type argument, not
/// declared on the command itself.
/// </para>
/// </remarks>
public interface ICommand;
