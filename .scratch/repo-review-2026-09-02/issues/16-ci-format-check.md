# 16 - Add a CI formatting check

**What to build:** `dotnet csharpier check .` runs as part of CI, catching formatting drift in the ~497 files csharpier can parse, while tolerating (not failing the build on) the known parser gap against the three files using preview `union`/`partial union` syntax.

Full technical detail: [findings/16-csharpier-union-parse-gap.md](../findings/16-csharpier-union-parse-gap.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `.github/workflows/ci.yml` runs `dotnet csharpier check .` (or an equivalent scoped invocation) as a CI step
- [ ] The three files using `union`/`partial union` syntax don't fail the build due to the known csharpier parser gap
- [ ] A tracking note records that a csharpier version bump should remove the carve-out once upstream union-syntax support lands
