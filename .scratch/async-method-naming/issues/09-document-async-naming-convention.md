# 09 - Document the Async naming convention in CLAUDE.md

**What to build:** A reader of `CLAUDE.md` can learn the full, current `Async` naming rule - including its one real carve-out - without having to reconstruct it from `.editorconfig` comments or tribal knowledge.

**Blocked by:** 06 - Contract: remove the old non-suffixed dispatch members; 07 - Re-enable VSTHRD200 for src/ and TutorialProject; 08 - Spike: test-support method enforcement.

**Status:** ready-for-agent

- [ ] `CLAUDE.md` gains a new subsection stating the rule: any method whose return type signals asynchrony (`Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, `IAsyncEnumerable<T>`, etc.) must end in `Async`, checked at build time by `VSTHRD200` in non-test code, with no remaining exceptions for the dispatch-contract verbs.
- [ ] The subsection states the test-method carve-out precisely: `[Fact]`/`[Theory]`-attributed test methods are exempt (named for behavior, not signature); other async methods in test code (helpers, fixtures, factories, local functions) are not exempt.
- [ ] The subsection states whichever enforcement mechanism ticket 08 settled on for test-support methods, as fact.
- [ ] The now-superseded `.editorconfig` comment about a MediatR-style exception (already rewritten in ticket 07) is cross-checked here for consistency with the new CLAUDE.md prose.
