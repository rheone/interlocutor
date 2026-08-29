# 26 - S3236: remove the caller-info-hiding argument in `DistinctItemSet`

**What to build:** SonarAnalyzer's S3236 ("Remove this argument from the method call; it hides the caller information") fires on `src/Interlocutor.CommonKernel/ResultTypes/Internal/DistinctItemSet.cs(40,50)` - an explicit argument is being passed to a parameter that has a `[CallerMemberName]`/`[CallerFilePath]`/`[CallerLineNumber]` (or equivalent) default, defeating the point of that attribute by hiding the real caller's identity. Read the call site: pass the argument's own caller-info attribute through, or simply omit the argument so the compiler fills it in automatically, unless there's a genuine reason this call needs to report a different caller identity than its own (rare, but record it if so).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] The flagged call either omits the argument (letting the compiler-supplied caller info flow through) or has a recorded reason for deliberately overriding it
- [ ] `dotnet build` reports zero `S3236` warnings
- [ ] `dotnet test` passes for `Interlocutor.CommonKernel.Tests`
