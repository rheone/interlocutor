# 32 - S6444: pass a timeout to the flagged blocking call in `OpenGenericHandlerResolver`

**What to build:** SonarAnalyzer's S6444 ("Pass a timeout to limit the execution time") fires on `src/Interlocutor.Generator/OpenGenericHandlerResolver.cs(274,16)`, on a call (likely a `Regex` match, a `Task.Wait`-family call, or similar) that can run unbounded. Confirm what's actually being called and whether an unbounded wait is a real risk in this context (this is source-generator/reflection code, typically running at build time against a fixed, small compilation, not runtime user input) - if a timeout is genuinely warranted, add one with a sensible bound; if the call is provably bounded by its own input shape (e.g. a regex over a short, compiler-controlled string), suppress with that reasoning recorded instead of adding a timeout that can never realistically trigger.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] The flagged call either has a real, justified timeout added, or is suppressed with a recorded reason why one isn't needed
- [x] `dotnet build` reports zero `S6444` warnings
- [x] `dotnet test` passes for `Interlocutor.Generator.Tests`

## Comments

Confirmed the flagged `Regex.Replace` call in `OpenGenericHandlerResolver.Substitute` operates on compiler-controlled input only: `template` is a short source snippet this generator itself emits, and the pattern is a `\b`-bounded alternation of `Regex.Escape`'d type-parameter names (a compilation's own generic arity) with no nested quantifiers to trigger catastrophic backtracking. Suppressed with that reasoning recorded inline rather than adding a timeout that could never realistically fire.
