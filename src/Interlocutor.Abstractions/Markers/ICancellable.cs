namespace Interlocutor.Abstractions.Markers;

/// <summary>
/// Marks a request whose cooperative cancellation should be converted into a
/// <see cref="Interlocutor.CommonKernel.ResultTypes.Cancelled"/> outcome instead of letting
/// <see cref="System.OperationCanceledException"/> propagate as an unexpected fault.
/// </summary>
/// <remarks>
/// <para>
/// Implement this marker interface on commands/queries whose handlers should treat cancellation as an
/// expected, compiler-checked outcome rather than an exception. The
/// <see cref="Pipeline.CancellationBehavior{TRequest, TResponse}"/> pipeline behavior intercepts these
/// requests and converts a cancellation tied to the ambient <see cref="System.Threading.CancellationToken"/>
/// into <see cref="Interlocutor.CommonKernel.ResultTypes.Cancelled"/>.
/// </para>
/// <para>
/// This marker has no members: every request already carries its own
/// <see cref="System.Threading.CancellationToken"/> through the pipeline's <c>next(CancellationToken)</c>
/// mechanism, so there is nothing for the request itself to declare beyond opting in. Per the "nothing runs
/// unless explicitly added" pipeline philosophy, this behavior does not run for a request that doesn't
/// implement this marker - cancellation is only converted where a request has opted in.
/// </para>
/// </remarks>
public interface ICancellable { }
