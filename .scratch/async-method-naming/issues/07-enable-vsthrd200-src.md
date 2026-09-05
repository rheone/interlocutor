# 07 - Re-enable VSTHRD200 for src/ and TutorialProject

**What to build:** The build actually fails if a non-test, non-suffixed async method is introduced in `src/` or `TutorialProject` - the naming rule becomes enforced, not just followed by convention.

**Blocked by:** 02 - Rename existing non-contract violations; 06 - Contract: remove the old non-suffixed dispatch members.

**Status:** ready-for-agent

- [ ] `VSTHRD200`'s severity is raised from `None` to an enforced level (matching how the neighboring `VSTHRD` rules in `.editorconfig` are already configured) for `src/` and `TutorialProject`.
- [ ] The `.editorconfig` comment block describing the old MediatR-style exception is rewritten to reflect that no exception exists anymore - the interfaces are fully suffixed now.
- [ ] A clean solution build of `src/` and `TutorialProject` produces zero `VSTHRD200` diagnostics.
