namespace Mediance.Abstractions.Commands;

/// <summary>
/// Marks a command that produces a typed response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of data returned when the command is handled.</typeparam>
public interface ICommand<TResponse>;

/// <summary>
/// Marks a command that produces no response data.
/// </summary>
/// <remarks>
/// <para>
/// Use the non-generic <see cref="ICommand"/> for fire-and-forget or side-effect-only
/// operations. The handler produces no data payload — only an outcome, expressed as any
/// <c>Mediance.CommonKernel.IOperationOutcome</c>-implementing union (<c>OperationOutcome</c>,
/// <c>OperationOutcome&lt;TSuccess, TFailure&gt;</c>, etc.). The outcome type is chosen by the caller via
/// <see cref="Core.ISender.Send{TResult}(ICommand, CancellationToken)"/>'s type argument, not
/// declared on the command itself.
/// </para>
/// </remarks>
public interface ICommand;
