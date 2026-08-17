# 15 - Documentation pass

## Labels

ready-for-agent

**What to build:** Update the documentation set to reflect everything built in tickets 01-14, following CLAUDE.md's house style exactly (GitHub alert conventions, no HTTP status codes outside `aspnetcore-integration.md`, no enumerated counts in prose, no citations to `docs/`, truthful against the shipped API - written after the code compiles, not before):

- `documentation/messages-and-result-types.md`: new section near "IOperationOutcome Constraint" - "IOperationOutcome as an Extension Point" - stating plainly that `IOperationOutcome` is the real extension surface, `OperationOutcome<...>` unions are the library's own built-in implementation of it (not the only legal one), and that any implementing type can participate once taught to `IOutcomeCaseRegistry`; update the "Result Types" walkthrough for the `Failure`/`Failure<TErrorCode>`/`Failures`/`Failures<TErrorCode>` split and the three new case types.
- `documentation/glossary.md`: update `Failures`/`Conflicts`/`ValidationErrors` rows for the dropped `.Values` alias; add rows for `Cancelled`, `Timeout`, `RateLimited`, `IOutcomeCaseRegistry`, `ITimeoutPolicy`, `IRateLimiter`, and the three new markers.
- `documentation/pipeline-and-behaviors.md`: add subsections for `CancellationBehavior`/`TimeoutBehavior`/`RateLimitedBehavior` mirroring the existing behavior subsections; update the canonical-order code sample, mermaid diagram, and rationale for the new eight-behavior order.
- `README.md`: update the Quick Start pipeline-order comment; lead the result-types mention with the "`IOperationOutcome` is the extension point" framing, linking to `messages-and-result-types.md` rather than duplicating detail.

**Blocked by:** 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 11, 12, 13, 14 (docs describe the finished, compiled API)

- [ ] `messages-and-result-types.md` has the new "IOperationOutcome as an Extension Point" section and an updated Result Types walkthrough
- [ ] `glossary.md` reflects the new types and the dropped duplicate projections
- [ ] `pipeline-and-behaviors.md` documents the three new behaviors and the updated canonical order (code sample + diagram + rationale)
- [ ] `README.md`'s pipeline-order comment and result-types framing are updated
- [ ] Every code example in the touched docs compiles against the shipped API (spot-check by pasting into a scratch file or an existing sample)
- [ ] No HTTP status codes appear outside `aspnetcore-integration.md`; no enumerated counts in prose; no citations to `docs/`
- [ ] `dotnet csharpier .` run if any code samples were added/edited inline
