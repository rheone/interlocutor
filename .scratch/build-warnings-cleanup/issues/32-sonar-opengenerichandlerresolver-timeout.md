# 32 - S6444: pass a timeout to the flagged blocking call in `OpenGenericHandlerResolver`

**What to build:** SonarAnalyzer's S6444 ("Pass a timeout to limit the execution time") fires on `src/Interlocutor.Generator/OpenGenericHandlerResolver.cs(274,16)`, on a call (likely a `Regex` match, a `Task.Wait`-family call, or similar) that can run unbounded. Confirm what's actually being called and whether an unbounded wait is a real risk in this context (this is source-generator/reflection code, typically running at build time against a fixed, small compilation, not runtime user input) - if a timeout is genuinely warranted, add one with a sensible bound; if the call is provably bounded by its own input shape (e.g. a regex over a short, compiler-controlled string), suppress with that reasoning recorded instead of adding a timeout that can never realistically trigger.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] The flagged call either has a real, justified timeout added, or is suppressed with a recorded reason why one isn't needed
- [ ] `dotnet build` reports zero `S6444` warnings
- [ ] `dotnet test` passes for `Interlocutor.Generator.Tests`
