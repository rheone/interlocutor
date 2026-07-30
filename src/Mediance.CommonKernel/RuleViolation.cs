namespace Mediance.CommonKernel;

/// <summary>
/// Represents a violation of a business rule.
/// Returned by domain logic or validators when a rule or constraint is not satisfied.
/// </summary>
/// <param name="code">A machine-readable error code identifying the violated rule.</param>
/// <param name="message">A human-readable description of the violation.</param>
public sealed record RuleViolation(string code, string message);
