# 22 - S2094: remove or justify empty test-fixture classes/records

**What to build:** SonarAnalyzer's S2094 ("Remove this empty class/record, write its code or make it an 'interface'") fires roughly a dozen times, mostly in `tests/Interlocutor.DependencyInjection.Abstractions.Tests` (the largest concentration), with smaller counts in `tests/Interlocutor.Abstractions.Tests`, `tests/Interlocutor.DependencyInjection.Autofac.Tests`, `tests/TodoTasks.Api.Tests`, and `src/Interlocutor.Generator` (`IsExternalInit.cs` - the standard polyfill shim, which is intentionally empty by design and should be suppressed with that noted, not fixed). Everywhere else, these are deliberately empty marker types used purely for their identity/shape in a generic-constraint or handler-discovery test - verify each is genuinely just a type-identity marker (keep, with a suppression + one-line reason) versus a leftover scaffold nobody finished (remove, or give it the member the test actually needs).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `IsExternalInit.cs` is suppressed with a comment noting it's an intentional, standard compiler-polyfill shim
- [x] Every other flagged empty type is either removed, given real content, or suppressed with a recorded reason (deliberate identity marker for a generic-constraint/discovery test)
- [x] `dotnet build` reports zero `S2094` warnings
- [x] `dotnet test` passes for every affected test project

## Comments

`IsExternalInit.cs` suppressed with a comment noting it's the standard netstandard2.0 init/record polyfill shim, recognized by the compiler via its mere presence. Every other flagged empty type (23 sites across `Interlocutor.Abstractions.Tests`, `Interlocutor.DependencyInjection.Abstractions.Tests`, `Interlocutor.DependencyInjection.Autofac.Tests`, `TodoTasks.Api.Tests`) is a deliberate type-identity marker for a generic-constraint, handler-discovery, or negative-case test - verified each is genuinely just an identity marker, not an unfinished scaffold, and suppressed with a one-line reason at each site or contiguous block.
