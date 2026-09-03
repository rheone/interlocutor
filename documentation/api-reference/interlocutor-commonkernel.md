# Interlocutor.CommonKernel API Reference

Every public type in `Interlocutor.CommonKernel`, grouped by namespace - the [result types](../glossary.md#result-types) every handler response is built from.

## Interlocutor.CommonKernel

### `GenerateOutcomeMembersAttribute` sealed class

Marks a partial union declaration for source-generated Match, Switch, per-case TryGetValue, ToString, and IsSuccess members.

| Member | Summary |
| --- | --- |
| `public GenerateOutcomeMembersAttribute()` |  |

### `Outcome` struct

Represents the outcome of a void operation as one of two possible cases: success with no payload, or failure.

| Member | Summary |
| --- | --- |
| `public Outcome(Success value)` |  |
| `public Outcome(Failure value)` |  |
| `public Type? ActiveCaseType` | Gets the active case's declared type, or `null` if no case was ever set. |
| `public bool IsSuccess` | Gets a value indicating whether this result holds the success case (the first declared case type). |
| `public object? Value` |  |
| `public bool Equals(Outcome other)` | Determines whether this result and `other` hold equal case values. |
| `public override bool Equals(object? obj)` | Determines whether this result and `obj` hold equal case values. |
| `public override int GetHashCode()` | Returns a hash code derived from the active case value. |
| `public TResult Match<TResult>(Func<Success, TResult> onCase1, Func<Failure, TResult> onCase2)` | Projects this result to a `TResult` by dispatching to the delegate matching its active case. |
| `public void Switch(Action<Success> onCase1, Action<Failure> onCase2)` | Dispatches to the action matching this result's active case. |
| `public override string ToString()` | Forwards to the active case value's own `ToString` override. |
| `public bool TryGetValue(out Success value)` | Attempts to read this result's value as its Success case. |
| `public bool TryGetValue(out Failure value)` | Attempts to read this result's value as its Failure case. |
| `public static bool operator ==(Outcome left, Outcome right)` | Determines whether `left` and `right` hold equal case values. |
| `public static bool operator !=(Outcome left, Outcome right)` | Determines whether `left` and `right` hold unequal case values. |

### `Outcome<TSuccessResult, TFailureResult>` struct

Represents the outcome of an operation as one of two possible cases: a successful result or a failure result.

| Member | Summary |
| --- | --- |
| `public Outcome(TSuccessResult value)` |  |
| `public Outcome(TFailureResult value)` |  |
| `public Type? ActiveCaseType` | Gets the active case's declared type, or `null` if no case was ever set. |
| `public bool IsSuccess` | Gets a value indicating whether this result holds the success case (the first declared case type). |
| `public object? Value` |  |
| `public bool Equals(Outcome<TSuccessResult, TFailureResult> other)` | Determines whether this result and `other` hold equal case values. |
| `public override bool Equals(object? obj)` | Determines whether this result and `obj` hold equal case values. |
| `public override int GetHashCode()` | Returns a hash code derived from the active case value. |
| `public TResult Match<TResult>(Func<TSuccessResult, TResult> onCase1, Func<TFailureResult, TResult> onCase2)` | Projects this result to a `TResult` by dispatching to the delegate matching its active case. |
| `public void Switch(Action<TSuccessResult> onCase1, Action<TFailureResult> onCase2)` | Dispatches to the action matching this result's active case. |
| `public override string ToString()` | Forwards to the active case value's own `ToString` override. |
| `public bool TryGetValue(out TSuccessResult value)` | Attempts to read this result's value as its TSuccessResult case. |
| `public bool TryGetValue(out TFailureResult value)` | Attempts to read this result's value as its TFailureResult case. |
| `public static bool operator ==(Outcome<TSuccessResult, TFailureResult> left, Outcome<TSuccessResult, TFailureResult> right)` | Determines whether `left` and `right` hold equal case values. |
| `public static bool operator !=(Outcome<TSuccessResult, TFailureResult> left, Outcome<TSuccessResult, TFailureResult> right)` | Determines whether `left` and `right` hold unequal case values. |

### `Outcome<TSuccessResult, TCase2, TCase3>` struct

Represents the outcome of an operation as one of three possible cases: a successful result or one of two result cases.

| Member | Summary |
| --- | --- |
| `public Outcome(TSuccessResult value)` |  |
| `public Outcome(TCase2 value)` |  |
| `public Outcome(TCase3 value)` |  |
| `public Type? ActiveCaseType` | Gets the active case's declared type, or `null` if no case was ever set. |
| `public bool IsSuccess` | Gets a value indicating whether this result holds the success case (the first declared case type). |
| `public object? Value` |  |
| `public bool Equals(Outcome<TSuccessResult, TCase2, TCase3> other)` | Determines whether this result and `other` hold equal case values. |
| `public override bool Equals(object? obj)` | Determines whether this result and `obj` hold equal case values. |
| `public override int GetHashCode()` | Returns a hash code derived from the active case value. |
| `public TResult Match<TResult>(Func<TSuccessResult, TResult> onCase1, Func<TCase2, TResult> onCase2, Func<TCase3, TResult> onCase3)` | Projects this result to a `TResult` by dispatching to the delegate matching its active case. |
| `public void Switch(Action<TSuccessResult> onCase1, Action<TCase2> onCase2, Action<TCase3> onCase3)` | Dispatches to the action matching this result's active case. |
| `public override string ToString()` | Forwards to the active case value's own `ToString` override. |
| `public bool TryGetValue(out TSuccessResult value)` | Attempts to read this result's value as its TSuccessResult case. |
| `public bool TryGetValue(out TCase2 value)` | Attempts to read this result's value as its TCase2 case. |
| `public bool TryGetValue(out TCase3 value)` | Attempts to read this result's value as its TCase3 case. |
| `public static bool operator ==(Outcome<TSuccessResult, TCase2, TCase3> left, Outcome<TSuccessResult, TCase2, TCase3> right)` | Determines whether `left` and `right` hold equal case values. |
| `public static bool operator !=(Outcome<TSuccessResult, TCase2, TCase3> left, Outcome<TSuccessResult, TCase2, TCase3> right)` | Determines whether `left` and `right` hold unequal case values. |

### `Outcome<TSuccessResult, TCase2, TCase3, TCase4>` struct

Represents the outcome of an operation as one of four possible cases: a successful result or one of three result cases.

| Member | Summary |
| --- | --- |
| `public Outcome(TSuccessResult value)` |  |
| `public Outcome(TCase2 value)` |  |
| `public Outcome(TCase3 value)` |  |
| `public Outcome(TCase4 value)` |  |
| `public Type? ActiveCaseType` | Gets the active case's declared type, or `null` if no case was ever set. |
| `public bool IsSuccess` | Gets a value indicating whether this result holds the success case (the first declared case type). |
| `public object? Value` |  |
| `public bool Equals(Outcome<TSuccessResult, TCase2, TCase3, TCase4> other)` | Determines whether this result and `other` hold equal case values. |
| `public override bool Equals(object? obj)` | Determines whether this result and `obj` hold equal case values. |
| `public override int GetHashCode()` | Returns a hash code derived from the active case value. |
| `public TResult Match<TResult>(Func<TSuccessResult, TResult> onCase1, Func<TCase2, TResult> onCase2, Func<TCase3, TResult> onCase3, Func<TCase4, TResult> onCase4)` | Projects this result to a `TResult` by dispatching to the delegate matching its active case. |
| `public void Switch(Action<TSuccessResult> onCase1, Action<TCase2> onCase2, Action<TCase3> onCase3, Action<TCase4> onCase4)` | Dispatches to the action matching this result's active case. |
| `public override string ToString()` | Forwards to the active case value's own `ToString` override. |
| `public bool TryGetValue(out TSuccessResult value)` | Attempts to read this result's value as its TSuccessResult case. |
| `public bool TryGetValue(out TCase2 value)` | Attempts to read this result's value as its TCase2 case. |
| `public bool TryGetValue(out TCase3 value)` | Attempts to read this result's value as its TCase3 case. |
| `public bool TryGetValue(out TCase4 value)` | Attempts to read this result's value as its TCase4 case. |
| `public static bool operator ==(Outcome<TSuccessResult, TCase2, TCase3, TCase4> left, Outcome<TSuccessResult, TCase2, TCase3, TCase4> right)` | Determines whether `left` and `right` hold equal case values. |
| `public static bool operator !=(Outcome<TSuccessResult, TCase2, TCase3, TCase4> left, Outcome<TSuccessResult, TCase2, TCase3, TCase4> right)` | Determines whether `left` and `right` hold unequal case values. |

### `Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5>` struct

Represents the outcome of an operation as one of five possible cases: a successful result or one of four result cases.

| Member | Summary |
| --- | --- |
| `public Outcome(TSuccessResult value)` |  |
| `public Outcome(TCase2 value)` |  |
| `public Outcome(TCase3 value)` |  |
| `public Outcome(TCase4 value)` |  |
| `public Outcome(TCase5 value)` |  |
| `public Type? ActiveCaseType` | Gets the active case's declared type, or `null` if no case was ever set. |
| `public bool IsSuccess` | Gets a value indicating whether this result holds the success case (the first declared case type). |
| `public object? Value` |  |
| `public bool Equals(Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5> other)` | Determines whether this result and `other` hold equal case values. |
| `public override bool Equals(object? obj)` | Determines whether this result and `obj` hold equal case values. |
| `public override int GetHashCode()` | Returns a hash code derived from the active case value. |
| `public TResult Match<TResult>(Func<TSuccessResult, TResult> onCase1, Func<TCase2, TResult> onCase2, Func<TCase3, TResult> onCase3, Func<TCase4, TResult> onCase4, Func<TCase5, TResult> onCase5)` | Projects this result to a `TResult` by dispatching to the delegate matching its active case. |
| `public void Switch(Action<TSuccessResult> onCase1, Action<TCase2> onCase2, Action<TCase3> onCase3, Action<TCase4> onCase4, Action<TCase5> onCase5)` | Dispatches to the action matching this result's active case. |
| `public override string ToString()` | Forwards to the active case value's own `ToString` override. |
| `public bool TryGetValue(out TSuccessResult value)` | Attempts to read this result's value as its TSuccessResult case. |
| `public bool TryGetValue(out TCase2 value)` | Attempts to read this result's value as its TCase2 case. |
| `public bool TryGetValue(out TCase3 value)` | Attempts to read this result's value as its TCase3 case. |
| `public bool TryGetValue(out TCase4 value)` | Attempts to read this result's value as its TCase4 case. |
| `public bool TryGetValue(out TCase5 value)` | Attempts to read this result's value as its TCase5 case. |
| `public static bool operator ==(Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5> left, Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5> right)` | Determines whether `left` and `right` hold equal case values. |
| `public static bool operator !=(Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5> left, Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5> right)` | Determines whether `left` and `right` hold unequal case values. |

### `Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6>` struct

Represents the outcome of an operation as one of six possible cases: a successful result or one of five alternative result cases.

| Member | Summary |
| --- | --- |
| `public Outcome(TSuccessResult value)` |  |
| `public Outcome(TCase2 value)` |  |
| `public Outcome(TCase3 value)` |  |
| `public Outcome(TCase4 value)` |  |
| `public Outcome(TCase5 value)` |  |
| `public Outcome(TCase6 value)` |  |
| `public Type? ActiveCaseType` | Gets the active case's declared type, or `null` if no case was ever set. |
| `public bool IsSuccess` | Gets a value indicating whether this result holds the success case (the first declared case type). |
| `public object? Value` |  |
| `public bool Equals(Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6> other)` | Determines whether this result and `other` hold equal case values. |
| `public override bool Equals(object? obj)` | Determines whether this result and `obj` hold equal case values. |
| `public override int GetHashCode()` | Returns a hash code derived from the active case value. |
| `public TResult Match<TResult>(Func<TSuccessResult, TResult> onCase1, Func<TCase2, TResult> onCase2, Func<TCase3, TResult> onCase3, Func<TCase4, TResult> onCase4, Func<TCase5, TResult> onCase5, Func<TCase6, TResult> onCase6)` | Projects this result to a `TResult` by dispatching to the delegate matching its active case. |
| `public void Switch(Action<TSuccessResult> onCase1, Action<TCase2> onCase2, Action<TCase3> onCase3, Action<TCase4> onCase4, Action<TCase5> onCase5, Action<TCase6> onCase6)` | Dispatches to the action matching this result's active case. |
| `public override string ToString()` | Forwards to the active case value's own `ToString` override. |
| `public bool TryGetValue(out TSuccessResult value)` | Attempts to read this result's value as its TSuccessResult case. |
| `public bool TryGetValue(out TCase2 value)` | Attempts to read this result's value as its TCase2 case. |
| `public bool TryGetValue(out TCase3 value)` | Attempts to read this result's value as its TCase3 case. |
| `public bool TryGetValue(out TCase4 value)` | Attempts to read this result's value as its TCase4 case. |
| `public bool TryGetValue(out TCase5 value)` | Attempts to read this result's value as its TCase5 case. |
| `public bool TryGetValue(out TCase6 value)` | Attempts to read this result's value as its TCase6 case. |
| `public static bool operator ==(Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6> left, Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6> right)` | Determines whether `left` and `right` hold equal case values. |
| `public static bool operator !=(Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6> left, Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6> right)` | Determines whether `left` and `right` hold unequal case values. |

## Interlocutor.CommonKernel.ResultTypes

### `Cancelled` struct

Indicates that an operation was cooperatively cancelled mid-flight.

| Member | Summary |
| --- | --- |
| `public Cancelled(string? Reason = null)` |  |
| `public string? Reason` | An optional human-readable description of why the operation was cancelled. |
| `public void Deconstruct(out string? Reason)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Cancelled other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this cancellation. |
| `public static bool operator ==(Cancelled left, Cancelled right)` |  |
| `public static bool operator !=(Cancelled left, Cancelled right)` |  |

### `Conflict` struct

Represents a conflict error that occurred during command or query execution.

| Member | Summary |
| --- | --- |
| `public Conflict(string Message, string Code)` |  |
| `public string Code` | A machine-readable error code identifying the conflict type. |
| `public string Message` | A human-readable description of the conflict. |
| `public void Deconstruct(out string Message, out string Code)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Conflict other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this conflict. |
| `public static bool operator ==(Conflict left, Conflict right)` |  |
| `public static bool operator !=(Conflict left, Conflict right)` |  |

### `Conflicts` struct

Represents an immutable collection of one or more conflict outcomes.

| Member | Summary |
| --- | --- |
| `public Conflicts(IEnumerable<Conflict> conflicts)` | Initializes a new instance of the `Conflicts` struct from the specified conflict values. |
| `public Conflicts(Conflict conflict)` | Initializes a new instance of the `Conflicts` struct from a single conflict. |
| `public int Count` | Gets the number of distinct conflicts. |
| `public IReadOnlyList<Conflict> Items` | Gets the distinct set of conflicts, in the order they were first encountered. |
| `public IReadOnlyList<string> Messages` | Gets the distinct set of conflict messages, in the order they were first encountered. Semantic projection of `Items`. |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Conflicts other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this conflict set. |
| `public static bool operator ==(Conflicts left, Conflicts right)` |  |
| `public static bool operator !=(Conflicts left, Conflicts right)` |  |

### `Failure` struct

Represents a generic failure outcome consisting of a reason only, with no associated error code.

| Member | Summary |
| --- | --- |
| `public Failure(string? Reason = null)` |  |
| `public string? Reason` | The human-readable reason for the failure, or `null` if none is provided. |
| `public void Deconstruct(out string? Reason)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Failure other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this failure. |
| `public static bool operator ==(Failure left, Failure right)` |  |
| `public static bool operator !=(Failure left, Failure right)` |  |

### `Failure<TErrorCode>` struct

Represents a generic failure outcome consisting of an error code and, optionally, a reason.

| Member | Summary |
| --- | --- |
| `public Failure(TErrorCode ErrorCode, string? Reason = null)` |  |
| `public TErrorCode ErrorCode` | The error code associated with the failure. |
| `public string? Reason` | The human-readable reason for the failure, or `null` if none is provided. |
| `public void Deconstruct(out TErrorCode ErrorCode, out string? Reason)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Failure<TErrorCode> other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this failure. |
| `public static bool operator ==(Failure<TErrorCode> left, Failure<TErrorCode> right)` |  |
| `public static bool operator !=(Failure<TErrorCode> left, Failure<TErrorCode> right)` |  |

### `Failures` struct

Represents an immutable collection of one or more generic failure reasons, with no associated error codes.

| Member | Summary |
| --- | --- |
| `public Failures(IEnumerable<Failure> failures)` | Initializes a new instance of the `Failures` struct from the specified failure values. |
| `public Failures(Failure failure)` | Initializes a new instance of the `Failures` struct from a single failure. |
| `public int Count` | Gets the number of distinct failures. |
| `public IReadOnlyList<Failure> Items` | Gets the distinct set of failures, in the order they were first encountered. |
| `public IReadOnlyList<string> Reasons` | Gets the distinct set of failure reasons, in the order they were first encountered. Semantic projection of `Items`. |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Failures other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this failure set. |
| `public static bool operator ==(Failures left, Failures right)` |  |
| `public static bool operator !=(Failures left, Failures right)` |  |

### `Failures<TErrorCode>` struct

Represents an immutable collection of one or more generic failures, each carrying an error code and, optionally, a reason.

| Member | Summary |
| --- | --- |
| `public Failures(IEnumerable<Failure<TErrorCode>> failures)` | Initializes a new instance of the `Failures` struct from the specified failure values. |
| `public Failures(Failure<TErrorCode> failure)` | Initializes a new instance of the `Failures` struct from a single failure. |
| `public int Count` | Gets the number of distinct failures. |
| `public IReadOnlyList<TErrorCode> ErrorCodes` | Gets the distinct set of error codes present in the original failures, in the order they were first encountered. |
| `public IReadOnlyList<Failure<TErrorCode>> Items` | Gets the distinct set of failures, each with its error code and reason, in the order they were first encountered. |
| `public IReadOnlyList<string> Reasons` | Gets the distinct set of failure reasons, in the order they were first encountered. |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Failures<TErrorCode> other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this failure set. |
| `public static bool operator ==(Failures<TErrorCode> left, Failures<TErrorCode> right)` |  |
| `public static bool operator !=(Failures<TErrorCode> left, Failures<TErrorCode> right)` |  |

### `FieldValidationMessage` struct

A single validation failure message attributed to the property/field it applies to, if any.

| Member | Summary |
| --- | --- |
| `public FieldValidationMessage(string? PropertyName, string Message)` |  |
| `public string Message` | The human-readable validation failure message. |
| `public string? PropertyName` | The name of the property/field that failed, or `null` when the failure isn't tied to a specific property - a cross-field or whole-object rule, for example. |
| `public void Deconstruct(out string? PropertyName, out string Message)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(FieldValidationMessage other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` |  |
| `public static bool operator ==(FieldValidationMessage left, FieldValidationMessage right)` |  |
| `public static bool operator !=(FieldValidationMessage left, FieldValidationMessage right)` |  |

### `Forbidden` struct

Indicates that the caller is not authorized to perform the requested operation.

| Member | Summary |
| --- | --- |
| `public Forbidden(string? Reason = null)` |  |
| `public string? Reason` | An optional description of why access was denied. |
| `public void Deconstruct(out string? Reason)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Forbidden other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this outcome. |
| `public static bool operator ==(Forbidden left, Forbidden right)` |  |
| `public static bool operator !=(Forbidden left, Forbidden right)` |  |

### `IFieldValidationFailureCase` interface

Marks a validation-shaped failure case that can attribute each message to the property/field that failed, beyond the plain message list `IValidationFailureCase` already guarantees.

| Member | Summary |
| --- | --- |
| `IReadOnlyList<FieldValidationMessage> FieldMessages` | Gets each validation failure paired with the property/field it applies to, if any. |

### `IForbiddenFailureCase` interface

Marks a type as a forbidden-shaped failure case usable in an `Outcome` union.

| Member | Summary |
| --- | --- |
| `string? Reason` | Gets the description of why access was denied. |

### `IOutcome` interface

Marks a type as a result union representing an operation's outcome.


### `IOutcomeCase` interface

Implemented by every compiler-generated union`GenerateOutcomeMembersAttribute` emits members for. Exposes the active case's identity directly, so a reflection-based consumer (the outcome classifier, the outcome case registry, AspNetCore's HTTP mapping) doesn't have to infer it from `Value`'s runtime type - which is `null` both when a case legitimately holds a `null` payload (a nullable reference type, or a `Nullable` with no value) and when the union was never assigned a case at all, and so can't tell the two apart on its own.

| Member | Summary |
| --- | --- |
| `Type? ActiveCaseType` | Gets the active case's declared type, or `null` if no case was ever set. |
| `bool IsSuccess` | Gets a value indicating whether the active case is the success case (the first declared case type). |

### `IRateLimitedFailureCase` interface

Marks a type as a rate-limit-shaped failure case usable in an `Outcome` union.

| Member | Summary |
| --- | --- |
| `string? Message` | Gets the human-readable description of the rate limit that was exceeded. |

### `ITimeoutFailureCase` interface

Marks a type as a timeout-shaped failure case usable in an `Outcome` union.

| Member | Summary |
| --- | --- |
| `string? Message` | Gets the human-readable description of the timeout. |

### `IValidationFailureCase` interface

Marks a type as a validation-shaped failure case usable in an `Outcome` union.

| Member | Summary |
| --- | --- |
| `IReadOnlyList<string> Messages` | Gets the validation failure messages carried by this case. |

### `NotFound` struct

Indicates that a requested resource was not found.

| Member | Summary |
| --- | --- |
| `public NotFound(string? Message = null)` |  |
| `public string? Message` | An optional human-readable description of what was not found. |
| `public void Deconstruct(out string? Message)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(NotFound other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this outcome. |
| `public static bool operator ==(NotFound left, NotFound right)` |  |
| `public static bool operator !=(NotFound left, NotFound right)` |  |

### `NotFound<TId>` struct

Indicates that a resource of a specific type and identifier was not found.

| Member | Summary |
| --- | --- |
| `public NotFound(Type EntityType, TId Id, string? Message = null)` |  |
| `public Type EntityType` | The type of entity that was not found. |
| `public TId Id` | The identifier of the resource that was searched for. |
| `public string? Message` | An optional human-readable description of what was not found. |
| `public void Deconstruct(out Type EntityType, out TId Id, out string? Message)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(NotFound<TId> other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this outcome. |
| `public static bool operator ==(NotFound<TId> left, NotFound<TId> right)` |  |
| `public static bool operator !=(NotFound<TId> left, NotFound<TId> right)` |  |

### `OutcomeCaseExtensions` class

Convenience projections over an arbitrary response, checking whether it implements `IOutcomeCase` and returning one of its members, or `null` if it doesn't.

| Member | Summary |
| --- | --- |
| `public static Type? AsOutcomeCaseActiveType(object? response)` | Gets `ActiveCaseType` if `response` implements `IOutcomeCase` and has a non-`null` active case type, or `null` otherwise. |
| `public static bool? AsOutcomeCaseIsSuccess(object? response)` | Gets `IsSuccess` if `response` implements `IOutcomeCase`, or `null` if it doesn't. |

### `RateLimited` struct

Indicates that the caller was throttled by a rate limiter.

| Member | Summary |
| --- | --- |
| `public RateLimited(string LimitKey, TimeSpan? RetryAfter = null, string? Message = null)` |  |
| `public string LimitKey` | Identifies which limiter/bucket was hit (e.g., a policy name or partition key). |
| `public string? Message` | An optional human-readable description of the rate limit that was exceeded. |
| `public TimeSpan? RetryAfter` | An optional hint for how long the caller should wait before retrying. |
| `public void Deconstruct(out string LimitKey, out TimeSpan? RetryAfter, out string? Message)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(RateLimited other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this rate-limit rejection. |
| `public static bool operator ==(RateLimited left, RateLimited right)` |  |
| `public static bool operator !=(RateLimited left, RateLimited right)` |  |

### `Success` struct

Represents a successful outcome with no associated data.

| Member | Summary |
| --- | --- |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Success other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a fixed, human-readable representation of this outcome. |
| `public static bool operator ==(Success left, Success right)` |  |
| `public static bool operator !=(Success left, Success right)` |  |

### `Timeout` struct

Indicates that an operation exceeded its allotted time budget.

| Member | Summary |
| --- | --- |
| `public Timeout(TimeSpan Budget, TimeSpan Elapsed, string? Message = null)` |  |
| `public TimeSpan Budget` | The time budget that was allotted to the operation. |
| `public TimeSpan Elapsed` | The actual time elapsed before the operation was abandoned. |
| `public string? Message` | An optional human-readable description of the timeout. |
| `public void Deconstruct(out TimeSpan Budget, out TimeSpan Elapsed, out string? Message)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(Timeout other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this timeout. |
| `public static bool operator ==(Timeout left, Timeout right)` |  |
| `public static bool operator !=(Timeout left, Timeout right)` |  |

### `ValidationError` struct

Represents a single validation error consisting of a message only, with no associated error code.

| Member | Summary |
| --- | --- |
| `public ValidationError(string Message)` |  |
| `public string Message` | The validation error message. |
| `public void Deconstruct(out string Message)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ValidationError other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this validation error. |
| `public static bool operator ==(ValidationError left, ValidationError right)` |  |
| `public static bool operator !=(ValidationError left, ValidationError right)` |  |

### `ValidationError<TErrorCode>` struct

Represents a single validation error consisting of a message and an associated error code.

| Member | Summary |
| --- | --- |
| `public ValidationError(string Message, TErrorCode ErrorCode)` |  |
| `public TErrorCode ErrorCode` | The error code associated with the validation error. |
| `public string Message` | The validation error message. |
| `public void Deconstruct(out string Message, out TErrorCode ErrorCode)` |  |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ValidationError<TErrorCode> other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this validation error. |
| `public static bool operator ==(ValidationError<TErrorCode> left, ValidationError<TErrorCode> right)` |  |
| `public static bool operator !=(ValidationError<TErrorCode> left, ValidationError<TErrorCode> right)` |  |

### `ValidationErrors` struct

Represents an immutable failure consisting of one or more distinct validation error messages, with no associated error codes.

| Member | Summary |
| --- | --- |
| `public ValidationErrors(IEnumerable<ValidationError> errors)` | Initializes a new instance of the `ValidationErrors` struct from the specified validation errors. |
| `public ValidationErrors(IEnumerable<string> messages)` | Initializes a new instance of the `ValidationErrors` struct from the specified messages. |
| `public ValidationErrors(string message)` | Initializes a new instance of the `ValidationErrors` struct from a single validation error message. |
| `public int Count` | Gets the number of distinct validation error messages. |
| `public IReadOnlyList<ValidationError> Errors` | Gets the distinct set of validation errors, in the order they were first encountered. Semantic projection of `Items`. |
| `public IReadOnlyList<ValidationError> Items` | Gets the distinct set of validation errors, in the order they were first encountered. |
| `public IReadOnlyList<string> Messages` | Gets the distinct set of validation error messages, in the order they were first encountered. |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ValidationErrors other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this validation error set. |
| `public static bool operator ==(ValidationErrors left, ValidationErrors right)` |  |
| `public static bool operator !=(ValidationErrors left, ValidationErrors right)` |  |

### `ValidationErrors<TErrorCode>` struct

Represents an immutable failure consisting of one or more distinct validation errors, each carrying a message and an error code.

| Member | Summary |
| --- | --- |
| `public ValidationErrors(IEnumerable<ValidationError<TErrorCode>> errors)` | Initializes a new instance of the `ValidationErrors` struct from the specified validation errors. |
| `public ValidationErrors(IEnumerable<(string Message, TErrorCode ErrorCode)> errors)` | Initializes a new instance of the `ValidationErrors` struct from the specified message and error code tuples. |
| `public ValidationErrors(string message, TErrorCode errorCode)` | Initializes a new instance of the `ValidationErrors` struct from a single message and error code pair. |
| `public int Count` | Gets the number of distinct validation errors (unique message and error code pairs). |
| `public IReadOnlyList<TErrorCode> ErrorCodes` | Gets the distinct set of error codes present in the original validation errors, in the order they were first encountered. |
| `public IReadOnlyList<ValidationError<TErrorCode>> Errors` | Gets the distinct set of validation errors, each with its message and error code, in the order they were first encountered. Semantic projection of `Items`. |
| `public IReadOnlyList<ValidationError<TErrorCode>> Items` | Gets the distinct set of validation errors, each with its message and error code, in the order they were first encountered. |
| `public IReadOnlyList<string> Messages` | Gets the distinct set of validation error messages, without their error codes, in the order they were first encountered. |
| `public int UniqueErrorCodeCount` | Gets the number of distinct error codes present in the original validation errors. |
| `public int UniqueErrorCount` | Gets the number of distinct validation error messages, irrespective of error code. |
| `public override bool Equals(object obj)` |  |
| `public bool Equals(ValidationErrors<TErrorCode> other)` |  |
| `public override int GetHashCode()` |  |
| `public override string ToString()` | Returns a human-readable representation of this validation error set. |
| `public static bool operator ==(ValidationErrors<TErrorCode> left, ValidationErrors<TErrorCode> right)` |  |
| `public static bool operator !=(ValidationErrors<TErrorCode> left, ValidationErrors<TErrorCode> right)` |  |

