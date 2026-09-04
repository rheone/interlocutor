# 11 - Fix `pipeline-and-behaviors.md`'s fictitious execution-order prose

**What to build:** The "Flat Ordered List" section describes pipeline execution in terms of the real `Handle(request, next, ct)` contract instead of the nonexistent `OnIngress()`/`OnEgress()` methods left over from a superseded pre-implementation design doc.

Full technical detail: [findings/10-pipeline-docs-fictitious-hooks.md](../findings/10-pipeline-docs-fictitious-hooks.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] The inbound/outbound bullets describe each behavior calling `next()` on the way in and acting on the awaited result on the way out
- [x] No reference to `OnIngress`/`OnEgress` remains in this file
- [x] The corrected prose is consistent with the file's own "Composition Diagram" and "Registering Custom Behaviors" sections

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. documentation/pipeline-and-behaviors.md's Flat Ordered List section now describes execution in terms of the real Handle(request, next, ct) contract (pre-next()/post-next() code) with no remaining OnIngress/OnEgress reference. No source changes were needed this session.
