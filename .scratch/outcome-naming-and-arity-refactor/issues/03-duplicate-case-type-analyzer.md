# 03 - Duplicate-case-type analyzer

**What to build:** C# has no generic constraint expressing "these type arguments must all be distinct," so nothing today stops a caller from closing an outcome union with the same type in two case positions - e.g. `Outcome<Success, Failure, Failure>` - which silently collapses two supposedly-independent failure cases into one and defeats the entire point of picking a specific arity for what an operation can actually produce.

Add a Roslyn `DiagnosticAnalyzer` to `Interlocutor.Generator` (it already rides in as a `PrivateAssets="all"` analyzer reference on `Interlocutor.Abstractions`, so no new project or reference shape is needed) that flags any closed generic type implementing `IOutcome` whose type argument list contains the same type more than once. Report it as a build warning at the usage site (the request/handler declaration, or wherever the closed generic appears), not just inside `Interlocutor.CommonKernel` - the check should apply to any `IOutcome`-implementing union a consumer closes, not only the library's own shipped `Outcome<...>` family.

**Blocked by:** Ticket 01 (targets the renamed `IOutcome`; landing the rename first avoids reworking the analyzer's target type name).

**Status:** ready-for-agent

- [ ] A new `DiagnosticAnalyzer` in `Interlocutor.Generator` flags a closed generic type implementing `IOutcome` whose type argument list repeats a type, with a diagnostic ID following this repo's existing analyzer ID numbering convention.
- [ ] The diagnostic message names the repeated type and both positions it appears in, so the fix is obvious without opening documentation.
- [ ] The analyzer fires for both the library's own shipped `Outcome<...>` family and an arbitrary consumer-declared `IOutcome`-implementing union closed the same way - it is not hardcoded to the `Outcome` type name.
- [ ] Unit tests cover: a duplicate in a two-case union, a duplicate in a six-case union, a duplicate involving the success-position type, and a clean (no-duplicate) union of each arity that produces no diagnostic.
- [ ] `documentation/extensibility.md` or `documentation/messages-and-result-types.md` gains a short mention of the diagnostic, consistent with how other analyzer-backed guarantees in this repo are documented.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.

