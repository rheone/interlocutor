namespace Interlocutor.Abstractions.Commands;

using System.Runtime.CompilerServices;
using Interlocutor.Abstractions.Pipeline;
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
///   <item><description><see cref="HandleAsync(TCommand, TEntity, CancellationToken)"/> - performs the business operation.</description></item>
/// </list>
/// If loading returns <c>null</c>, or <see cref="Authorize"/> returns a <see cref="Forbidden"/>, this class ejects
/// the pipeline dispatch with that case via <see cref="Eject.WithCase{TCase}(TCase)"/> rather than constructing
/// <typeparamref name="TResponse"/> itself - see the "Case construction and where it is caught" section below for
/// why that is safe even though this class is a template-method base class, not a pipeline behavior.
/// </para>
/// <para>
/// <b>Generic over <typeparamref name="TResponse"/>, not fixed to a three-case shape.</b> An earlier revision of
/// this class fixed its response to <c>Outcome&lt;TSuccess, NotFound, Forbidden&gt;</c> and constructed
/// <see cref="NotFound"/>/<see cref="Forbidden"/> directly. Fixing <typeparamref name="TCommand"/>'s response to
/// whatever arity a subclass's command actually declares - as long as it has a <see cref="NotFound"/> case for
/// <see cref="LoadAsync"/>'s <c>null</c> result and a <see cref="Forbidden"/> case for a denied
/// <see cref="Authorize"/> - lets a subclass add further cases (e.g. a domain-specific <see cref="Conflict"/>)
/// without abandoning this base class the way <c>TutorialProject</c>'s <c>CompleteTodoTaskCommand</c> remarks
/// describe having to do under the old fixed shape.
/// </para>
/// <para>
/// <b>Case construction and where it is caught.</b> This class ejects <see cref="NotFound"/>/<see cref="Forbidden"/>
/// via <see cref="Eject.WithCase{TCase}(TCase)"/> exactly the way <see cref="AuthorizationBehavior{TRequest, TResponse}"/>
/// does, with no <c>IOutcomeCaseRegistry</c> dependency of its own. Although this class is a template-method base
/// class a concrete handler inherits from - not a pipeline behavior - the ejected
/// <see cref="PipelineEjectCaseException"/> still reaches the same catch site
/// <see cref="AuthorizationBehavior{TRequest, TResponse}"/> relies on: a subclass is registered as
/// <c>ICommandHandler&lt;TCommand, TResponse&gt;</c>, and the mediator's command dispatch wrapper resolves that
/// handler and passes its own <c>HandleAsync</c> method - which, for a subclass, is this class's public
/// <see cref="HandleAsync(TCommand, CancellationToken)"/> - as the innermost delegate to
/// <c>PipelineChainExecutor.ExecuteAsync</c>. That method already wraps the innermost delegate call in a
/// <see langword="try"/>/<see langword="catch"/> for <see cref="PipelineEjectCaseException"/> before it ever
/// reaches a behavior, so an eject thrown from here unwinds no further than that same catch, with no additional
/// catch point needed anywhere in this class or its subclasses. This was proved empirically, not assumed - see
/// <c>ResourceCommandHandlerEjectCaseEndToEndTests</c>, which dispatches through a real <c>Mediator</c> with no
/// pipeline behaviors registered at all, so the only thing that could possibly be catching the eject is the
/// wrapper/executor path described here.
/// </para>
/// </remarks>
/// <typeparam name="TCommand">The command type that this handler processes.</typeparam>
/// <typeparam name="TEntity">The domain entity type loaded and used by this handler.</typeparam>
/// <typeparam name="TResponse">
/// The response type this handler produces. Must declare a <see cref="NotFound"/> case (for a missing entity)
/// and a <see cref="Forbidden"/> case (for a denied <see cref="Authorize"/> check) among its cases, or the
/// eject that follows a short-circuit throws <see cref="InvalidOperationException"/> at the catch site instead
/// of constructing a response - see
/// <c>Interlocutor.Abstractions.Pipeline.Cases.OutcomeCaseRegistryExtensions.ConstructOrThrow{TResponse}(Interlocutor.Abstractions.Pipeline.Cases.IOutcomeCaseRegistry, PipelineEjectCaseException)</c>.
/// </typeparam>
public abstract class ResourceCommandHandler<TCommand, TEntity, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : IUnion
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
    /// <returns>A <see cref="Forbidden"/> if authorization is denied, or <c>null</c> if authorized.</returns>
    protected abstract Forbidden? Authorize(TEntity entity, TCommand command);

    /// <summary>
    /// Performs the actual business operation on the loaded and authorized entity.
    /// </summary>
    /// <param name="command">The command being handled.</param>
    /// <param name="entity">The loaded entity that passed authorization.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The response.</returns>
    protected abstract Task<TResponse> HandleAsync(TCommand command, TEntity entity, CancellationToken ct);

    /// <summary>
    /// Executes the load-authorize-handle pipeline for the given command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The response.</returns>
    /// <exception cref="PipelineEjectCaseException">
    /// Thrown - not returned - when the entity is missing or authorization is denied. Never observed by a
    /// caller of <c>Interlocutor.Abstractions.Core.IMediator</c>; see this type's own remarks for the catch
    /// site that converts it back into an ordinary <typeparamref name="TResponse"/> value.
    /// </exception>
    public async Task<TResponse> HandleAsync(TCommand command, CancellationToken ct)
    {
        var entity = await this.LoadAsync(command, ct).ConfigureAwait(false);

        // Short-circuit if the entity does not exist - no need to authorize or process. Ejects rather than
        // constructing TResponse directly, since TResponse is no longer fixed to a shape this class knows how
        // to construct on its own - see this type's remarks for where the eject is caught and TResponse built.
        if (entity is null)
        {
            throw Eject.WithCase(new NotFound($"{typeof(TEntity).Name} '{this.DescribeKey(command)}' was not found."));
        }

        var authorizationResult = this.Authorize(entity, command);

        // Short-circuit if the caller is not authorized to act on this entity.
        if (authorizationResult is Forbidden forbidden)
        {
            throw Eject.WithCase(forbidden);
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
