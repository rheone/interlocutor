# 34 - S8969: remove redundant null-forgiving operators the compiler already proves unnecessary

**What to build:** SonarAnalyzer's S8969 ("Remove this null-forgiving operator; the compiler already knows this expression is not null here") fires on `src/Interlocutor.Abstractions/Commands/ResourceCommandHandler.cs(96,70)` and twice in `tests/Interlocutor.DependencyInjection.Microsoft.Tests`. Confirm nullable reference type flow analysis genuinely proves each flagged expression non-null at that point (not just "Sonar thinks so" - check for a preceding null check, pattern match, or non-nullable-returning call the compiler can see) before removing the `!` - a `!` that's actually load-bearing (masking a real nullable-flow-analysis gap the compiler can't see through) should stay, with a comment explaining why.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Every flagged `!` is either removed (confirmed genuinely redundant under nullable reference type analysis) or kept with a recorded reason
- [x] `dotnet build` reports zero `S8969` warnings, and no new CS8600-family nullable warnings appear as a result of removing any `!`
- [x] `dotnet test` passes for `Interlocutor.Abstractions.Tests` and `Interlocutor.DependencyInjection.Microsoft.Tests`

## Comments

Confirmed all 3 flagged `!` operators are genuinely redundant under nullable flow analysis: `ResourceCommandHandler.DescribeKey`'s `command` parameter is a non-nullable `TCommand` (the compiler treats an unconstrained-nullability generic parameter as non-null from its own non-`?` declaration), and both `ServiceCollectionExtensionsTests.cs` sites follow `Assert.NotNull(descriptor)`, which xunit's `[NotNull]`-attributed signature already lets the compiler narrow past. Removed all three; rebuilt to confirm no new CS8600-family warnings appeared.
