# 30 - S4136: group `ToActionResult` overloads together

**What to build:** SonarAnalyzer's S4136 ("All 'ToActionResult' method overloads should be adjacent") fires twice on `src/Interlocutor.AspNetCore/ResultExtensions.cs(47,33)` - some `ToActionResult` overloads (across the different `OperationOutcome<...>` arities this extension class maps to HTTP responses) are separated by other members. Reorder the file so every `ToActionResult` overload sits together, in arity order, without changing any behavior - a pure reordering, not a logic change. Follow the `csharp-code-organization` skill's member-ordering guidance for where the grouped block should sit relative to the rest of the class.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Every `ToActionResult` overload is adjacent to every other overload in `ResultExtensions.cs`, in a sensible arity order
- [x] No behavior changed - this is a pure reordering
- [x] `dotnet build` reports zero `S4136` warnings
- [x] `dotnet test` passes for `Interlocutor.AspNetCore.Tests`

## Comments

`ResultExtensions.cs` had its catch-all `ToActionResult<TResponse>`/`ToMinimalApiResult<TResponse>` overloads separated from their arity-specific dedicated siblings by the other family's entire overload block. Reordered the file (a pure line-range move, verified the file's total line count didn't change) so all 7 `ToActionResult` overloads sit together, followed by all 7 `ToMinimalApiResult` overloads, then the shared private helpers - no logic changed.
