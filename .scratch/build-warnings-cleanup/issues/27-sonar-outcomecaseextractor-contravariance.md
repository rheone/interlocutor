# 27 - S3246: consider making `OutcomeCaseExtractor`'s `TResponse` contravariant

**What to build:** SonarAnalyzer's S3246 ("Add the 'in' keyword to parameter 'TResponse' to make it 'contravariant'") fires on `src/Interlocutor.Abstractions/Pipeline/Cases/OutcomeCaseExtractor.cs(16,43)`. Marking a type parameter `in` is a real public-API/variance decision, not a mechanical fix - verify `TResponse` is used only in input positions throughout the interface (never returned), confirm making it contravariant doesn't conflict with how this repo's `OperationOutcome<...>` case-extraction machinery is documented (see `documentation/` and README.md's result-type composition sections) before changing it, and check whether existing consumers rely on the current (non-variant) generic-constraint behavior anywhere.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] A decision is made and recorded (added `in` after confirming input-only usage and no consumer impact, or suppressed with a reason `in` doesn't fit)
- [ ] `dotnet build` reports zero `S3246` warnings
- [ ] `dotnet test` passes solution-wide, since this is a core pipeline/result-type interface
