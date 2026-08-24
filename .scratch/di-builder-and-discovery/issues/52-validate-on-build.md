> **Status: done** - implemented on the `improvements` branch: `InterlocutorBuilder.ValidateOnBuild()` (opt-in flag), `RegistrationProblem`/`RegistrationProblemKind` (`UnhandledRequestType`/`UnmatchedRequiredRule`/`ConflictingLifetime`), and `InterlocutorRegistrationValidationException` (carries the same `RegistrationReport` `DescribeRegistrations()` returns, with `Problems` populated). `DescribeRegistrations()` only computes `Problems` when `ValidateOnBuild()` was called - omitting it runs zero additional checks, same as before. Both DI packages build the report once at `AddInterlocutor()` time and throw, aggregating every problem found, before applying container registrations further; `InterlocutorStartupValidation.ValidateHandlerRegistrations` is untouched.

## Labels

ready-for-agent

## What to build

`AddInterlocutor` today validates nothing beyond null-argument guards - a scanned request type with no registered handler, an unmet required match rule (ticket 51), or two registrations of the same (service type, implementation type) pair at conflicting lifetimes are all invisible until something fails at resolution time, if they're ever caught at all. Add an opt-in check that catches everything answerable from the registration list alone.

- A new `ValidateOnBuild()` builder method. When configured, `AddInterlocutor` checks the accumulated registration list - no live container needed - for:
  - a request type discovered while scanning with no matching `Handler`-kind registration
  - a match rule (ticket 51) marked as requiring at least one match that matched none
  - two registrations of the same (service type, implementation type) pair with different `RegistrationLifetime` values
- All problems found are aggregated into a single dedicated exception type (not a bare `InvalidOperationException`) - every problem in one throw, not a fail-on-first cycle. The exception carries the same `RegistrationReport` object `DescribeRegistrations()` returns (ticket 50), with its `Problems` collection populated, rather than building a second structure.
- `ValidateOnBuild()` deliberately does not attempt anything that needs a live container (an unresolvable constructor dependency, for instance) - that remains the job of the existing, separate, still-opt-in `ValidateHandlerRegistrations(IServiceProvider, assemblies)`, which this ticket leaves untouched.
- Without `ValidateOnBuild()` configured, none of these checks run - consistent with the rest of the builder's "nothing happens unless asked for" behavior; the report itself (ticket 50) is still always buildable via `DescribeRegistrations()` regardless.

## Acceptance criteria

- [ ] `ValidateOnBuild()` is opt-in; omitting it runs zero additional checks, same as today
- [ ] A scanned request type with no registered handler is reported as one problem among potentially several, not thrown immediately
- [ ] A required match rule (ticket 51) that matched nothing is reported as a problem
- [ ] Two registrations of the same (service type, implementation type) pair at different lifetimes is reported as a problem; at the same lifetime, it is not
- [ ] All problems found in one `AddInterlocutor()` call are raised together in a single dedicated exception, not one at a time
- [ ] The thrown exception carries the same report object `DescribeRegistrations()` returns, with `Problems` populated
- [ ] `ValidateHandlerRegistrations(IServiceProvider, assemblies)` is unchanged and continues to pass its existing tests
- [ ] `dotnet build` and `dotnet test` pass for the whole solution

## Blocked by

50 - Registration report, 51 - Type-matching registration (needs the `Require()` flag on a match rule to check)
