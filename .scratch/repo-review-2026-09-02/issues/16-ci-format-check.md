# 16 - Add a CI formatting check

**What to build:** `dotnet csharpier check .` runs as part of CI, catching formatting drift in the ~497 files csharpier can parse, while tolerating (not failing the build on) the known parser gap against the three files using preview `union`/`partial union` syntax.

Full technical detail: [findings/16-csharpier-union-parse-gap.md](../findings/16-csharpier-union-parse-gap.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `.github/workflows/ci.yml` runs `dotnet csharpier check .` (or an equivalent scoped invocation) as a CI step
- [x] The three files using `union`/`partial union` syntax don't fail the build due to the known csharpier parser gap
- [x] A tracking note records that a csharpier version bump should remove the carve-out once upstream union-syntax support lands

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. .github/workflows/ci.yml runs `dotnet csharpier check . --syntax-errors-as-warnings`, tolerating the three preview-union-syntax files without failing the build. No source changes were needed this session.
