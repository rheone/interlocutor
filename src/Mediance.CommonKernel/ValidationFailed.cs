namespace Mediance.CommonKernel;

/// <summary>
/// Represents a validation failure containing errors keyed by property name.
/// Returned when one or more validation rules are not satisfied for a request.
/// Each entry in the dictionary maps a property name to its corresponding violations.
/// </summary>
/// <param name="errors">A dictionary mapping property names to their validation error
/// codes and messages. Each property may have multiple associated violations.</param>
public sealed record ValidationFailed(IReadOnlyDictionary<string, IReadOnlyList<(string Code, string Message)>> errors);
