# 11 - Relax SA1402/SA1649 for test projects in .editorconfig

**What to build:** Test fixture files routinely declare several supporting types (records, marker interfaces, handler stubs) before the `XxxTests` class that actually uses them - legitimate multi-type files where the eponymous test class isn't the first type, and often isn't named after the file's first declaration either. `SA1402` ("file may only contain a single type") and `SA1649` ("file name must match first type") both currently fire on these files, with no `tests/`-scoped relaxation in `.editorconfig` today. Add a test-project-scoped section (matching the repo's existing test-glob conventions, e.g. alongside the `{**Tests/**Tests.cs,**Tests.**/**Tests.cs}` section) setting both rules to `none`, leaving both fully enforced in `src/`.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `.editorconfig` has a test-project-scoped section setting `SA1402` and `SA1649` to `none`
- [x] `dotnet build` reports zero `SA1402`/`SA1649` warnings for any `tests/` project
- [x] `SA1402`/`SA1649` remain at their current (enforced) severity for every `src/` project - unaffected by the new section
- [x] `dotnet test` passes solution-wide with no behavior change

## Comments

Already implemented in commit `56141a9`. Verified: `.editorconfig`'s test-glob section (`[{**Tests/**Tests.cs,**Tests.**/**Tests.cs}]`) sets `dotnet_diagnostic.SA1402.severity`/`SA1649.severity` to `None`, while the general `[*.{cs,vb,fs}]` section still enforces `SA1649` at `Warning` (line 577) for everything else, including `src/`. A full rebuild reports zero `SA1402`/`SA1649` warnings for any `tests/` project, and `dotnet test` passes solution-wide.
