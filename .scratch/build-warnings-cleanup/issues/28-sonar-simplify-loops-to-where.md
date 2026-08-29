# 28 - S3267: simplify filter-then-act loops using LINQ `Where`

**What to build:** SonarAnalyzer's S3267 ("Loops should be simplified using the 'Where' LINQ method") fires on a `foreach` containing only an `if`-guarded body in `src/Interlocutor.Generator/HandlerDiagnosticAnalyzer.cs(41,33)`, `src/Interlocutor.Abstractions` (one occurrence), and `src/Interlocutor.DependencyInjection.Abstractions` (two occurrences). Rewrite each as `foreach (var x in items.Where(...))` (or a full LINQ chain, if that reads better at the call site) - `Interlocutor.Generator` is a Roslyn incremental source generator, so double-check any perf-sensitive hot path there isn't better left as a loop for allocation reasons before applying the mechanical rewrite; record that reasoning if it is left alone.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Every flagged loop is either rewritten using `Where` or kept as a loop with a recorded performance/allocation reason (especially for the generator's own hot paths)
- [ ] `dotnet build` reports zero `S3267` warnings
- [ ] `dotnet test` passes for `Interlocutor.Generator.Tests` and the other affected projects
