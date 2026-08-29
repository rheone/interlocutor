# 09 - Disable SA1009/SA1111 in .editorconfig

**What to build:** `SA1009` (closing parenthesis spacing) and `SA1111` (closing parenthesis placement) are both set to `Suggestion` severity in `.editorconfig`, per ticket 05's investigation - left there deliberately, below the build's warning threshold. But csharpier's multi-line-call formatting (the closing paren of a wrapped call lands on its own line) is a fixed, non-configurable part of the formatter, so these two rules can never be satisfied once csharpier reformats a file - `Suggestion` severity was a half-measure, not a resolution. Since the formatter and the analyzer configuration should agree rather than perpetually disagree, disable both outright.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `dotnet_diagnostic.SA1009.severity` and `dotnet_diagnostic.SA1111.severity` are set to `none` in `.editorconfig`
- [x] `dotnet build` reports zero `SA1009`/`SA1111` diagnostics solution-wide
- [x] `dotnet csharpier . --check` (or equivalent) still passes, confirming no residual conflict between the formatter and the analyzer config

## Comments

Already implemented, in commit `56141a9` ("chore: fix mechanical build-warning tickets 09-14,16") - discovered already-done while investigating why this and its siblings still showed `ready-for-agent` despite the code being present; the commit implemented all seven but never flipped their ticket files' Status. Verified against the current tree: `.editorconfig` sets both `dotnet_diagnostic.SA1009.severity` and `dotnet_diagnostic.SA1111.severity` to `None`. A full `dotnet build -t:Rebuild` reports zero `SA1009`/`SA1111` warnings solution-wide, and `dotnet csharpier check .` passes (aside from two pre-existing, unrelated files csharpier's parser can't handle at all - union-type syntax, present before this ticket).
