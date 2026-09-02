namespace Interlocutor.FluentValidation;

using global::FluentValidation.Results;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A pipeline behavior that validates a request using all registered <see cref="global::FluentValidation.IValidator{TRequest}"/> instances
/// before passing it to the next handler in the pipeline, preserving full <see cref="ValidationFailure"/> fidelity.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">
/// The response type, constrained only to <see cref="IOutcome"/> - deliberately unconstrained
/// beyond that. See the Remarks for why, and for the same reasoning applied to <see cref="AuthorizationBehavior{TRequest, TResponse}"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// Same shape as <see cref="ValidationBehavior{TRequest, TResponse}"/> - aggregates every registered
/// validator, short-circuits to <c>next()</c> unchanged when no validators are registered or none report
/// failures - but constructs <see cref="FluentValidationErrors"/> instead of
/// <see cref="Interlocutor.CommonKernel.ResultTypes.ValidationErrors"/>, so every field of every
/// <see cref="ValidationFailure"/> reported by every validator reaches the caller. This is strictly additive:
/// <see cref="ValidationBehavior{TRequest, TResponse}"/> is not modified, deprecated, or touched. A consumer
/// opts into full fidelity by declaring <see cref="FluentValidationErrors"/> as a case in their own
/// <c>Outcome&lt;...&gt;</c> union and registering this behavior instead of (or, though not
/// recommended, alongside) <see cref="ValidationBehavior{TRequest, TResponse}"/>.
/// </para>
/// <para>
/// <typeparamref name="TResponse"/> is constrained only to <see cref="IOutcome"/> for the same
/// reason <see cref="ValidationBehavior{TRequest, TResponse}"/>'s remarks give in full. Constructing the
/// <see cref="FluentValidationErrors"/> case of an unconstrained <typeparamref name="TResponse"/> is
/// delegated to an injected <see cref="IOutcomeCaseRegistry"/> rather than performed reflectively inline, so a
/// consumer with a hand-rolled, non-union <see cref="IOutcome"/> implementation can register a
/// custom factory for it instead of being limited to compiler-generated unions.
/// </para>
/// </remarks>
public sealed class FluentValidationErrorsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IOutcome
{
    private readonly IOutcomeCaseRegistry caseRegistry;
    private readonly IEnumerable<global::FluentValidation.IValidator<TRequest>> validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationErrorsBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The set of FluentValidation validators to validate the request.
    /// If this collection is empty, validation is skipped and the request is forwarded directly
    /// to the next pipeline stage.</param>
    /// <param name="caseRegistry">
    /// Constructs the <see cref="FluentValidationErrors"/> case of <typeparamref name="TResponse"/> when
    /// validation fails.
    /// </param>
    public FluentValidationErrorsBehavior(
        IEnumerable<global::FluentValidation.IValidator<TRequest>> validators,
        IOutcomeCaseRegistry caseRegistry
    )
    {
        this.validators = validators;
        this.caseRegistry = caseRegistry;
    }

    /// <summary>
    /// Validates the request using all registered validators. If validation succeeds,
    /// the request is forwarded to the next pipeline stage.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The next delegate in the pipeline, invoked if validation passes.</param>
    /// <param name="ct">A cancellation token to cancel validation or the inner handler.</param>
    /// <returns>Either the success result from the inner handler, or a <see cref="FluentValidationErrors"/>
    /// failure carrying every <see cref="ValidationFailure"/> from every validator, verbatim.</returns>
    /// <exception cref="InvalidOperationException">
    /// Validation failed, but <typeparamref name="TResponse"/> has no <see cref="FluentValidationErrors"/>
    /// case to construct - use a response type that has one.
    /// </exception>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Short-circuit when no validators are registered - the pipeline proceeds without validation.
        if (!this.validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        // Aggregate every validation failure across every validator, preserving each one verbatim (no
        // projection to just its message) and the order each validator produced it.
        var failures = new List<ValidationFailure>();
        foreach (var validator in this.validators)
        {
            var result = await validator.ValidateAsync(request, ct).ConfigureAwait(false);
            failures.AddRange(result.Errors);
        }

        // Short-circuit if validation passed - forward to handler unchanged.
        if (failures.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        // Validation failed - construct the FluentValidationErrors case with every failure collected above,
        // and return immediately without invoking the handler.
        return this.ConstructFluentValidationErrorsResponse(new FluentValidationErrors(failures));
    }

    private TResponse ConstructFluentValidationErrorsResponse(FluentValidationErrors errors) =>
        this.caseRegistry.ConstructOrThrow<TResponse, FluentValidationErrors>(
            errors,
            $"FluentValidationErrorsBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>",
            "found validation errors",
            $"Use a response type with a {nameof(FluentValidationErrors)} case"
        );
}
