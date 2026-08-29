# 28 - S3267: simplify filter-then-act loops using LINQ `Where`

**What to build:** SonarAnalyzer's S3267 ("Loops should be simplified using the 'Where' LINQ method") fires on a `foreach` containing only an `if`-guarded body in `src/Interlocutor.Generator/HandlerDiagnosticAnalyzer.cs(41,33)`, `src/Interlocutor.Abstractions` (one occurrence), and `src/Interlocutor.DependencyInjection.Abstractions` (two occurrences). Rewrite each as `foreach (var x in items.Where(...))` (or a full LINQ chain, if that reads better at the call site) - `Interlocutor.Generator` is a Roslyn incremental source generator, so double-check any perf-sensitive hot path there isn't better left as a loop for allocation reasons before applying the mechanical rewrite; record that reasoning if it is left alone.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Every flagged loop is either rewritten using `Where` or kept as a loop with a recorded performance/allocation reason (especially for the generator's own hot paths)
- [x] `dotnet build` reports zero `S3267` warnings
- [x] `dotnet test` passes for `Interlocutor.Generator.Tests` and the other affected projects

## Comments

Rewrote all four flagged loops as `foreach`-over-`Where()`: `NotificationHandlerResolver.NotificationTypeHierarchy` and `InterlocutorPipelineBuilder.OverridePipelineFor` (both filter-then-act shapes), `PipelineOverrideRegistry.TryGetOverride` (rewritten as `FirstOrDefault`, the more idiomatic form for a first-match lookup), and the generator's own `HandlerDiagnosticAnalyzer.ReportMissingHandlers`. For the generator's hot path specifically: confirmed `ImmutableArray<T>.Where` resolves to `System.Linq.ImmutableArrayExtensions`' specialized non-boxing overload (not the general `IEnumerable<T>` one), so the mechanical rewrite carries no allocation regression there - recorded that reasoning inline rather than leaving it as a loop.
