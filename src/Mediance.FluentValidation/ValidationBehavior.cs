namespace Mediance.FluentValidation;

using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;

/// <summary>
/// A pipeline behavior that validates a request using all registered <see cref="global::FluentValidation.IValidator{TRequest}"/> instances
/// before passing it to the next handler in the pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of the request being validated.</typeparam>
/// <typeparam name="TSuccess">The type of the response produced when the request handler succeeds.</typeparam>
/// <remarks>
/// <para>
/// Validation errors from all validators are aggregated and collected into a <see cref="ValidationErrors"/>
/// aggregate. This behavior preserves every violation from every validator rather than stopping at the first failure.
/// </para>
/// <para>
/// Like <see cref="AuthorizationBehavior{TRequest, TSuccess}"/>, this behavior must construct its own
/// failure case without knowing the handler's response shape in general, so it is constrained to
/// responses of the concrete shape <see cref="OperationOutcome{TSuccess, TFailureResult}"/> with
/// <see cref="ValidationErrors"/> as the failure case.
/// </para>
/// </remarks>
public sealed class ValidationBehavior<TRequest, TSuccess>
    : IPipelineBehavior<TRequest, OperationOutcome<TSuccess, ValidationErrors>>
{
    private readonly IEnumerable<global::FluentValidation.IValidator<TRequest>> validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TSuccess}"/> class.
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
    public async Task<OperationOutcome<TSuccess, ValidationErrors>> Handle(
        TRequest request,
        Func<Task<OperationOutcome<TSuccess, ValidationErrors>>> next,
        CancellationToken ct
    )
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
        return new ValidationErrors(errorMessages);
    }
}
