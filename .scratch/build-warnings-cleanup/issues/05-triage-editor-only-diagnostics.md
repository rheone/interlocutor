# 05 - Triage and fix editor/Sonar-only diagnostics invisible to `dotnet build`

**What to build:** `xUnit2032`, `S6966`, `S2699`, `S1144`, `RCS1213`, `RCS1163`, `IDE0053`, `IDE0039`, `SA1111`, and `SA1009` were reported by the IDE's Problems panel but none of them appear in a clean `dotnet build -t:Rebuild` of the solution - `SA1111` and `SA1009` are explicitly configured as `Suggestion` severity in `.editorconfig` (below the build's warning threshold), the `IDE0xxx` codes are compiler code-style analyzers that default to suggestion-level unless `EnforceCodeStyleInBuild` promotes them, and the `S`-prefixed codes are SonarLint rules with no corresponding analyzer package referenced by the solution - they come from an IDE extension only. There is no CLI source of truth for where these fire, so this ticket starts with locating every occurrence (via the IDE's Problems panel, `dotnet format analyzers`, or by temporarily promoting the relevant `dotnet_diagnostic.*.severity` entries to `warning` and rebuilding) before deciding fixes case by case - some may be genuine bugs (`S2699`: assertion completeness in a test, `S6966`: sync-over-async, `CS`-style unused-member codes `S1144`/`RCS1213`/`RCS1163`), others pure style (`IDE0053`/`IDE0039` expression-bodied/local-function preferences, `SA1111`/`SA1009` parenthesis placement/spacing, `xUnit2032` assertion-form preference).

**Blocked by:** None - can start immediately, independent of tickets 01-04.

**Status:** ready-for-agent

- [ ] Every occurrence of the ten listed codes is located and either fixed or has a recorded, deliberate suppression with a reason
- [ ] Fixes for `S1144`/`RCS1213`/`RCS1163` (unused member/parameter) are verified as genuinely dead - not removing something used via reflection, DI, or a test double
- [ ] `S2699` fixes add a real assertion rather than just satisfying the rule mechanically
- [ ] `dotnet build` and `dotnet test` remain clean after the changes
