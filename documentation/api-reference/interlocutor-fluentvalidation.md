# Interlocutor.FluentValidation API Reference

Every public type in `Interlocutor.FluentValidation`, grouped by namespace - a [validation-shaped failure case](glossary.md#failure-case-types) preserving full FluentValidation fidelity, plus the pipeline behaviors that produce it.

## Interlocutor.FluentValidation

### `FluentValidationErrors` struct

Represents an immutable failure carrying one or more `ValidationFailure` instances verbatim, with none of their fields discarded.

| Member | Summary |
| --- | --- |
| `public FluentValidationErrors(IReadOnlyList<ValidationFailure> failures)` | Initializes a new instance of the `FluentValidationErrors` struct from the specified validation failures. |
| `public IReadOnlyList<ValidationFailure> Failures` | Gets the validation failures reported by every validator, in the order each validator produced them. |
| `public IReadOnlyList<FieldValidationMessage> FieldMessages` | Gets each failure's `PropertyName` paired with its `ErrorMessage`, in the order `Failures` reports them. |
| `public IReadOnlyList<string> Messages` | Gets each failure's `ErrorMessage`, in the order `Failures` reports them. |
| `public bool Equals(FluentValidationErrors other)` | Determines whether this instance and `other` hold the same `ValidationFailure` content, order-independently. |
| `public override bool Equals(object obj)` |  |
| `public override int GetHashCode()` | Returns a hash code derived from every failure's content, order-independently. |
| `public override string ToString()` | Returns a human-readable representation of this validation failure set. |
| `public static bool operator ==(FluentValidationErrors left, FluentValidationErrors right)` |  |
| `public static bool operator !=(FluentValidationErrors left, FluentValidationErrors right)` |  |

### `FluentValidationErrorsBehavior<TRequest, TResponse>` sealed class

A pipeline behavior that validates a request using all registered `IValidator` instances before passing it to the next handler in the pipeline, preserving full `ValidationFailure` fidelity.

| Member | Summary |
| --- | --- |
| `public FluentValidationErrorsBehavior(IEnumerable<IValidator<TRequest>> validators, IOutcomeCaseRegistry caseRegistry)` | Initializes a new instance of the `FluentValidationErrorsBehavior` class. |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Validates the request using all registered validators. If validation succeeds, the request is forwarded to the next pipeline stage. |

### `ValidationBehavior<TRequest, TResponse>` sealed class

A pipeline behavior that validates a request using all registered `IValidator` instances before passing it to the next handler in the pipeline.

| Member | Summary |
| --- | --- |
| `public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, IOutcomeCaseRegistry caseRegistry)` | Initializes a new instance of the `ValidationBehavior` class. |
| `public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)` | Validates the request using all registered validators. If validation succeeds, the request is forwarded to the next pipeline stage. |

