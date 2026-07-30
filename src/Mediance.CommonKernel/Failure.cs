namespace Mediance.CommonKernel;

/// <summary>
/// Represents the possible failure outcomes for an operation.
/// </summary>
public readonly union Failure(ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict);
