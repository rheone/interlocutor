# 01 - Add a best-practices reference placeholder

**What to build:** A new `documentation/best-practices.md`, consumer-facing - "how to use Interlocutor well in your app." Deliberately a lightweight placeholder for now: a short introduction plus a small number of genuinely settled guidelines (e.g. cross-links to the existing arity-choice rules in `messages-and-result-types.md` and the pipeline-ordering rationale in the README, rather than restating them), with room to grow as real usage patterns accumulate. Linked from `documentation/index.md`'s table of contents.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `documentation/best-practices.md` exists, follows the `documentation/` house style (no ADR citations, glossary-linking on first mention, no enumerated counts in prose), and every claim in it is verified against shipped `src/` code
- [ ] It is linked from `documentation/index.md`
- [ ] It reads as an intentional starting point, not a finished reference - explicit that it will grow
