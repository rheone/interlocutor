# 04 - Migrate TutorialProject implementers to the *Async dispatch members

**What to build:** Every feature handler in the runnable worked example switches to implementing the new `*Async` members, and the walkthrough doc that shows this code stays truthful.

**Blocked by:** 01 - Expand: add Async-suffixed dispatch members with forwarding defaults.

**Status:** done - implemented in `454cb03`.

- [ ] Every one of TutorialProject's feature handlers implements the new `*Async` member instead of the old one.
- [ ] TutorialProject builds and its tests pass; the app still runs end-to-end as documented.
- [ ] `documentation/sample-app-walkthrough.md`'s code excerpts that show the changed method are updated to the `*Async` name in the same iteration (per this repo's documentation-maintenance convention - docs don't go stale in a later pass).
