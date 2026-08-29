# 27 - S3246: consider making `OutcomeCaseExtractor`'s `TResponse` contravariant

**What to build:** SonarAnalyzer's S3246 ("Add the 'in' keyword to parameter 'TResponse' to make it 'contravariant'") fires on `src/Interlocutor.Abstractions/Pipeline/Cases/OutcomeCaseExtractor.cs(16,43)`. Marking a type parameter `in` is a real public-API/variance decision, not a mechanical fix - verify `TResponse` is used only in input positions throughout the interface (never returned), confirm making it contravariant doesn't conflict with how this repo's `OperationOutcome<...>` case-extraction machinery is documented (see `documentation/` and README.md's result-type composition sections) before changing it, and check whether existing consumers rely on the current (non-variant) generic-constraint behavior anywhere.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] A decision is made and recorded (added `in` after confirming input-only usage and no consumer impact, or suppressed with a reason `in` doesn't fit)
- [x] `dotnet build` reports zero `S3246` warnings
- [x] `dotnet test` passes solution-wide, since this is a core pipeline/result-type interface

## Comments

Verified `TResponse` appears only in `OutcomeCaseExtractor<TResponse>`'s input `response` position, never returned - `caseType`/`caseValue` are `Type`/`object?`, unrelated to `TResponse`. `OutcomeCaseRegistryBuilder` stores and casts extractors by the exact registered closed type at runtime regardless of static variance, so marking it `in TResponse` is a pure compile-time widening with no consumer-visible behavior change. Added `in`, with the reasoning recorded in the delegate's own `<remarks>`. Full solution `dotnet test` (including `Interlocutor.Architecture.Tests`) passes.
