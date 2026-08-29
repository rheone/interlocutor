# 26 - S3236: remove the caller-info-hiding argument in `DistinctItemSet`

**What to build:** SonarAnalyzer's S3236 ("Remove this argument from the method call; it hides the caller information") fires on `src/Interlocutor.CommonKernel/ResultTypes/Internal/DistinctItemSet.cs(40,50)` - an explicit argument is being passed to a parameter that has a `[CallerMemberName]`/`[CallerFilePath]`/`[CallerLineNumber]` (or equivalent) default, defeating the point of that attribute by hiding the real caller's identity. Read the call site: pass the argument's own caller-info attribute through, or simply omit the argument so the compiler fills it in automatically, unless there's a genuine reason this call needs to report a different caller identity than its own (rare, but record it if so).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] The flagged call either omits the argument (letting the compiler-supplied caller info flow through) or has a recorded reason for deliberately overriding it
- [x] `dotnet build` reports zero `S3236` warnings
- [x] `dotnet test` passes for `Interlocutor.CommonKernel.Tests`

## Comments

`paramName` is a genuine, deliberate override point: each composing type (`Conflicts`, `Failures`, `ValidationErrors`) passes its own field name via `nameof(...)` so a thrown `ArgumentNullException` reports that name instead of this constructor's own `items` parameter - confirmed by reading every call site. Passing it explicitly does mean `ThrowIfNull`'s own `[CallerArgumentExpression]` auto-fill never applies, so the few "unreachable"-message call sites that don't override it were falling back to a `null` `ParamName` instead of `"items"` - fixed that with `paramName ?? nameof(items)` (a real improvement, independent of the Sonar finding), then suppressed S3236 with the override rationale recorded, since removing the argument isn't possible without losing the override capability.
