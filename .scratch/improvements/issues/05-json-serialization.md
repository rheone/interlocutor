> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: references removed result and case types.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Add JSON serialization support for the C# 15 union types in `Interlocutor.CommonKernel` (`Failure`, `Result<T>`, `Result`, `Success`). The C# `readonly union` types will not serialize or deserialize correctly with default `System.Text.Json` settings.

The implementation approach:

- Custom `JsonConverter<T>` for `Failure` that writes/reads the discriminant and its payload
- Custom `JsonConverter<T>` for `Result<T>` and `Result` that delegates to the inner value or failure
- Wire the converters into `Interlocutor.AspNetCore.ToActionResult()` pipeline so that serialized responses are correct
- Consider a new package `Interlocutor.Text.Json` or add to `Interlocutor.AspNetCore` — pick whichever makes it easiest for consumers to opt in

The union type serialization format should be self-describing — each failure variant includes its discriminator and payload:

```json
// ValidationFailed example
{ "$type": "validationFailed", "errors": { "Email": [{"code": "NotEmpty", "message": "..."}] } }

// RuleViolation example
{ "$type": "ruleViolation", "code": "Order.AlreadyShipped", "message": "Cannot cancel an already-shipped order" }
```

## Acceptance criteria

- [ ] `Failure` serializes and deserializes all 5 variants round-trip
- [ ] `Result<T>` serializes and deserializes success (with value) and failure round-trip
- [ ] `Result` (void) serializes and deserializes `Success` and failure round-trip
- [ ] Converters are wired so `ToActionResult()` returns correctly serialized ProblemDetails
- [ ] Tests cover all variants including edge cases (empty errors dictionary, null reason, etc.)
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
