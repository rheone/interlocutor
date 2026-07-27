namespace Mediance.CommonKernel;

public readonly union Failure(ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict);
