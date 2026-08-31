namespace Interlocutor.Abstractions.Commands;

using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Provides a template method for command handlers that load, authorize, and then
/// act on a domain entity.
/// </summary>
/// <remarks>
/// <para>
/// Subclasses override three steps executed in order:
/// <list type="number">
///   <item><description><see cref="LoadAsync"/> - retrieves the target entity.</description></item>
///   <item><description><see cref="Authorize"/> - checks access permissions against the loaded entity.</description></item>
///   <item><description><see cref="HandleAsync"/> - performs the business operation.</description></item>
/// </list>
/// If loading returns <c>null</c> the pipeline short-circuits with <see cref="NotFound"/>.
/// If authorization returns a <see cref="Failure"/> the pipeline short-circuits with that failure.
/// </para>
/// </remarks>
/// <typeparam name="TCommand">The command type that this handler processes.</typeparam>
/// <typeparam name="TEntity">The domain entity type loaded and used by this handler.</typeparam>
/// <typeparam name="TSuccess">The type of data returned when the command succeeds.</typeparam>
public abstract class ResourceCommandHandler<TCommand, TEntity, TSuccess>
    where TCommand : ICommand<Outcome<TSuccess, NotFound, Failure>>
{
    /// <summary>
    /// Loads the target entity for the given command.
    /// </summary>
    /// <param name="command">The command being handled.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The loaded entity, or <c>null</c> if no matching entity exists.</returns>
    protected abstract Task<TEntity?> LoadAsync(TCommand command, CancellationToken ct);

    /// <summary>
    /// Determines whether the caller is authorized to act on the loaded entity.
    /// </summary>
    /// <param name="entity">The loaded domain entity.</param>
    /// <param name="command">The command being handled.</param>
    /// <returns>A <see cref="Failure"/> if authorization is denied, or <c>null</c> if authorized.</returns>
    protected abstract Failure? Authorize(TEntity entity, TCommand command);

    /// <summary>
    /// Performs the actual business operation on the loaded and authorized entity.
    /// </summary>
    /// <param name="command">The command being handled.</param>
    /// <param name="entity">The loaded entity that passed authorization.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The response, or a failure reason.</returns>
    protected abstract Task<Outcome<TSuccess, NotFound, Failure>> HandleAsync(
        TCommand command,
        TEntity entity,
        CancellationToken ct
    );

    /// <summary>
    /// Executes the load-authorize-handle pipeline for the given command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The response, or a failure reason.</returns>
    public async Task<Outcome<TSuccess, NotFound, Failure>> Handle(TCommand command, CancellationToken ct)
    {
        var entity = await this.LoadAsync(command, ct).ConfigureAwait(false);

        // Short-circuit if the entity does not exist - no need to authorize or process.
        if (entity is null)
        {
            return new NotFound($"{typeof(TEntity).Name} '{this.DescribeKey(command)}' was not found.");
        }

        var authorizationResult = this.Authorize(entity, command);

        // Short-circuit if the caller is not authorized to act on this entity.
        if (authorizationResult is Failure failure)
        {
            return new Outcome<TSuccess, NotFound, Failure>(failure);
        }

        // Entity exists and caller is authorized - delegate to the subclass business logic.
        return await this.HandleAsync(command, entity, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Produces a human-readable description of the entity key for use in error messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation returns the command instance itself. Override to provide
    /// a cleaner representation (e.g., a composite key string or record identifier).
    /// </para>
    /// </remarks>
    /// <param name="command">The command being handled.</param>
    /// <returns>An object whose <c>ToString</c> representation describes the entity key.</returns>
    protected virtual object DescribeKey(TCommand command) => command;
}
