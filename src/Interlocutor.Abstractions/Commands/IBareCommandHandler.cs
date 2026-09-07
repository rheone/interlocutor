namespace Interlocutor.Abstractions.Commands;

using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Handles a command that produces no data payload - only an outcome.
/// </summary>
/// <typeparam name="TCommand">The command type that this handler processes.</typeparam>
/// <typeparam name="TResult">
/// The <see cref="IUnion"/>-shaped union describing the outcome. Chosen by the handler/call site
/// - may be any public union type in <c>Interlocutor.CommonKernel</c> (<see cref="Outcome"/>,
/// <see cref="Outcome{T1, T2}"/>, etc.).
/// </typeparam>
/// <remarks>
/// <para>
/// Use this handler for commands that mutate state but return only success/failure (no domain data). The result
/// type is a union that lets handlers express what went wrong in a compiler-checked way, so callers must
/// exhaustively handle every outcome case.
/// </para>
/// <para>
/// Unlike <see cref="ICommandHandler{TCommand, TResponse}"/>, which returns domain data, bare commands are often
/// paired with transactions (via <see cref="Markers.ITransactionalRequest"/>) and auditing (via <see cref="Markers.IAuditableRequest"/>).
/// </para>
/// <para>
/// The <typeparamref name="TResult"/> type is chosen at the call site, not at handler registration time.
/// This lets the same command be handled differently depending on what outcome information the caller needs.
/// </para>
/// </remarks>
public interface IBareCommandHandler<in TCommand, TResult>
    where TCommand : ICommand
    where TResult : IUnion
{
    /// <summary>
    /// Handles the specified command and returns the outcome.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The outcome of handling the command.</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}
