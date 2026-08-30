# 01 - Prefer extension members in CLAUDE.md

**What to build:** CLAUDE.md's Conventions section states the project's preference for C# 14 extension
members (`extension(receiver) { }` blocks) over classic `this`-parameter extension methods, scoped to
where it actually pays off, so both humans and agents apply the same bar consistently on future code
instead of re-deriving it per PR.

The rule: prefer an `extension(receiver) { }` block **only when two or more members share the same
receiver type** within a static class - that's the case where the block genuinely removes repetition
(the receiver declaration written once instead of once per method). A static class with a single
extension method keeps the classic `this`-parameter syntax; there's no repetition for a block to remove,
and the classic form stays the more widely recognized one for a one-off. `TutorialProject/` is exempt
regardless of member count - it's the Beginner's Guide's worked example, and `documentation/sample-app-walkthrough.md`
walks a reader through its shape, so it optimizes for showing widely-recognized C# rather than the newest
syntax.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] CLAUDE.md's Conventions section documents the conditional preference (2+ members sharing a receiver → extension block; single-method classes stay classic)
- [ ] The `TutorialProject/` exemption is stated explicitly, with the reason (teaching-code fidelity to `documentation/sample-app-walkthrough.md`)
- [ ] Wording is consistent with the existing "Use the newest C# .NET 11 features when appropriate" bullet rather than duplicating or contradicting it
