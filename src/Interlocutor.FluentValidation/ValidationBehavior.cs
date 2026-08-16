namespace Interlocutor.FluentValidation;

using System.Reflection;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A pipeline behavior that validates a request using all registered <see cref="global::FluentValidation.IValidator{TRequest}"/> instances
/// before passing it to the next handler in the pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TResponse">
/// The response type, constrained only to <see cref="IOperationOutcome"/> — deliberately unconstrained
/// beyond that. See the Remarks for why, and for the same reasoning applied to <see cref="AuthorizationBehavior{TRequest, TResponse}"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// Validation errors from all validators are aggregated and collected into a <see cref="ValidationErrors"/>
/// aggregate. This behavior preserves every violation from every validator rather than stopping at the first failure.
/// </para>
/// <para>
/// <typeparamref name="TResponse"/> is constrained only to <see cref="IOperationOutcome"/>, not decomposed
/// into a success-type parameter with <see cref="ValidationErrors"/> pinned as the failure case — for exactly
/// the reason, and via exactly the same <see cref="UnionCaseFactory"/> mechanism,
/// <see cref="AuthorizationBehavior{TRequest, TResponse}"/>'s remarks describe in full: the decomposed shape
/// broke Microsoft.Extensions.DependencyInjection's open-generic resolution outright.
/// </para>
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : IOperationOutcome
{
    // Resolved once per closed TResponse — see AuthorizationBehavior's identical field for the full
    // rationale. Null when TResponse has no constructor accepting a single ValidationErrors argument.
    private static readonly ConstructorInfo? ValidationErrorsConstructor = UnionCaseFactory.FindCaseConstructor<
        TResponse,
        ValidationErrors
    >();

    private readonly IEnumerable<global::FluentValidation.IValidator<TRequest>> validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The set of FluentValidation validators to validate the request.
    /// If this collection is empty, validation is skipped and the request is forwarded directly
    /// to the next pipeline stage.</param>
    public ValidationBehavior(IEnumerable<global::FluentValidation.IValidator<TRequest>> validators)
    {
        this.validators = validators;
    }

    /// <summary>
    /// Validates the request using all registered validators. If validation succeeds,
    /// the request is forwarded to the next pipeline stage.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The next delegate in the pipeline, invoked if validation passes.</param>
    /// <param name="ct">A cancellation token to cancel validation or the inner handler.</param>
    /// <returns>Either the success result from the inner handler, or a <see cref="ValidationErrors"/>
    /// failure containing all validation error messages from all validators, de-duplicated.</returns>
    /// <exception cref="InvalidOperationException">
    /// Validation failed, but <typeparamref name="TResponse"/> has no <see cref="ValidationErrors"/> case to
    /// construct — use a response type that has one.
    /// </exception>
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        // Short-circuit when no validators are registered — the pipeline proceeds without validation.
        if (!this.validators.Any())
        {
            return await next();
        }

        // Aggregate all validation failures across every validator.
        // Collect all error messages in order, preserving order of first occurrence.
        var errorMessages = new List<string>();
        foreach (var validator in this.validators)
        {
            var result = await validator.ValidateAsync(request, ct);
            foreach (var error in result.Errors)
            {
                errorMessages.Add(error.ErrorMessage);
            }
        }

        // Short-circuit if validation passed — forward to handler unchanged.
        if (errorMessages.Count == 0)
        {
            return await next();
        }

        // Validation failed — create ValidationErrors aggregate with all messages (de-duplicated)
        // and return immediately without invoking the handler.
        return ConstructValidationErrorsResponse(new ValidationErrors(errorMessages));
    }

    private static TResponse ConstructValidationErrorsResponse(ValidationErrors errors)
    {
        if (ValidationErrorsConstructor is null)
        {
            throw new InvalidOperationException(
                $"ValidationBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}> found validation errors, "
                    + $"but {typeof(TResponse).Name} has no {nameof(ValidationErrors)} case to construct. Use a "
                    + $"response type with a {nameof(ValidationErrors)} case."
            );
        }

        return UnionCaseFactory.Invoke<TResponse, ValidationErrors>(ValidationErrorsConstructor, errors);
    }
}
