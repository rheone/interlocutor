# 04 - Validation-shape mismatch analyzer

**What to build:** `documentation/beginners-guide-todo-api.md` already documents a real, sharp-edged gotcha: `ResourceCommandHandler`'s response shape is fixed to `OperationOutcome<TSuccess, NotFound, Failure>` (now `Outcome<TSuccess, NotFound, Failure>` post-Ticket-01), which has no validation-shaped case. Registering a `FluentValidation` validator for a command shaped like that doesn't fail at compile time - it throws the first time a real validation failure happens at runtime, because `ValidationBehavior` has nowhere to put the result.

Add a second `DiagnosticAnalyzer` to `Interlocutor.Generator` that statically detects this: scan the compilation for `AbstractValidator<TCommand>`/`AbstractValidator<TQuery>` declarations, cross-reference each one's `TCommand`/`TQuery` against that request's declared `ICommand<TResponse>`/`IQuery<TResponse>` response type, and flag it when the response arity has no case implementing `IValidationFailureCase` (or the concrete `ValidationError`/`ValidationErrors` types). This turns the documented runtime surprise into a build-time error, without needing any DI/runtime registration information - everything the check needs is visible in the compilation.

**Blocked by:** Ticket 01 (targets the renamed `Outcome<...>`/`IOutcome`; landing the rename first avoids rework).

**Status:** ready-for-agent

- [ ] A new `DiagnosticAnalyzer` in `Interlocutor.Generator` flags a command/query whose response arity has no `IValidationFailureCase`-shaped case, when an `AbstractValidator<TCommand>`/`AbstractValidator<TQuery>` for that same request type exists in the same compilation.
- [ ] The diagnostic message names the request type, its actual response arity's case types, and what's missing, so the fix (add a validation case, or remove the validator) is obvious without opening documentation.
- [ ] No diagnostic fires for a request with no validator, nor for a request whose response arity already includes a validation-shaped case.
- [ ] Unit tests cover: a `ResourceCommandHandler`-shaped command with a validator and no validation case (fires), the same command with a validation case added (doesn't fire), and a command with no validator at all (doesn't fire regardless of arity).
- [ ] `documentation/beginners-guide-todo-api.md`'s existing warning about this gotcha is updated to note it's now a compile-time diagnostic rather than a runtime surprise.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.

