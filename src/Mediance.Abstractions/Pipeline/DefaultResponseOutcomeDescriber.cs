namespace Mediance.Abstractions.Pipeline;

using System.Runtime.CompilerServices;
using Mediance.CommonKernel.ResultTypes;

/// <summary>
/// Default <see cref="IResponseOutcomeDescriber"/>. Unwraps compiler-generated unions via
/// <see cref="IUnion"/>, special-cases the <c>Mediance.CommonKernel.ResultTypes</c> sibling case
/// types (<see cref="NotFound"/>, <see cref="Forbidden"/>, <see cref="Conflict"/>,
/// <see cref="ValidationErrors"/>, <see cref="Failure"/>), and
/// falls back to the runtime type name for anything else.
/// </summary>
public sealed class DefaultResponseOutcomeDescriber : IResponseOutcomeDescriber
{
    /// <summary>Gets the shared singleton instance.</summary>
    public static readonly DefaultResponseOutcomeDescriber Instance = new();

    /// <inheritdoc/>
    public string Describe(object? response) =>
        Unwrap(response) switch
        {
            null => "None",
            ValidationErrors => "ValidationError",
            NotFound => "NotFound",
            Forbidden => "Forbidden",
            Conflict => "Conflict",
            Failure => "Failure",
            var value => value.GetType().Name,
        };

    /// <inheritdoc/>
    public bool IsFailure(object? response) =>
        Unwrap(response) is ValidationErrors or NotFound or Forbidden or Conflict or Failure;

    private static object? Unwrap(object? response) => response is IUnion union ? union.Value : response;
}
