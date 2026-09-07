# 06 - Contract: remove the old non-suffixed dispatch members

**What to build:** The old `Handle`/`Send`/`Publish`/`Cancel` members (and the two dispatch-wrapper interfaces' old `Handle`) are deleted from their interfaces entirely, now that nothing implements or calls them. This is the "contract" half of the wide rename - after this ticket, the `*Async` forms are the only forms that exist.

**Blocked by:** 03 - Migrate src/ implementers; 04 - Migrate TutorialProject implementers; 05 - Migrate tests/ fakes.

**Status:** ready-for-agent

- [ ] `Handle`, `Send`, `Publish`, `Cancel` (old, non-suffixed forms) and the two dispatch-wrapper interfaces' old `Handle` member are removed from their interfaces entirely.
- [ ] No production or test code anywhere in the solution references the old member names.
- [ ] A full solution build and test run is green with only the `*Async` names present anywhere in the dispatch contract.
