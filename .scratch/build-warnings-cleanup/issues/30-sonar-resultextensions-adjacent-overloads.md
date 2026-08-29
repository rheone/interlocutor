# 30 - S4136: group `ToActionResult` overloads together

**What to build:** SonarAnalyzer's S4136 ("All 'ToActionResult' method overloads should be adjacent") fires twice on `src/Interlocutor.AspNetCore/ResultExtensions.cs(47,33)` - some `ToActionResult` overloads (across the different `OperationOutcome<...>` arities this extension class maps to HTTP responses) are separated by other members. Reorder the file so every `ToActionResult` overload sits together, in arity order, without changing any behavior - a pure reordering, not a logic change. Follow the `csharp-code-organization` skill's member-ordering guidance for where the grouped block should sit relative to the rest of the class.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Every `ToActionResult` overload is adjacent to every other overload in `ResultExtensions.cs`, in a sensible arity order
- [ ] No behavior changed - this is a pure reordering
- [ ] `dotnet build` reports zero `S4136` warnings
- [ ] `dotnet test` passes for `Interlocutor.AspNetCore.Tests`
