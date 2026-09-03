# 13 - Harden `global.json`'s SDK roll-forward policy

**What to build:** A future major .NET SDK install can no longer silently swap out from under this build without a deliberate `global.json` bump — protecting the preview-only `union` language feature `Interlocutor.CommonKernel`/`Interlocutor.Generator` depend on from an unvalidated compiler change.

Default direction (tighten, not just document) unless whoever picks this up has a specific reason to keep `latestMajor`: change `rollForward` to `latestFeature` or `latestPatch`.

Full technical detail: [findings/12-globaljson-rollforward-risk.md](../findings/12-globaljson-rollforward-risk.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `rollForward` is tightened to `latestFeature` or `latestPatch` — OR, if `latestMajor` is kept deliberately, `global.json` carries a comment explaining why given the preview-feature dependency
- [ ] CI still builds successfully with the resulting policy
