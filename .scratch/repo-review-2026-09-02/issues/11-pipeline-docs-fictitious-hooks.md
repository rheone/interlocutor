# 11 - Fix `pipeline-and-behaviors.md`'s fictitious execution-order prose

**What to build:** The "Flat Ordered List" section describes pipeline execution in terms of the real `Handle(request, next, ct)` contract instead of the nonexistent `OnIngress()`/`OnEgress()` methods left over from a superseded pre-implementation design doc.

Full technical detail: [findings/10-pipeline-docs-fictitious-hooks.md](../findings/10-pipeline-docs-fictitious-hooks.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] The inbound/outbound bullets describe each behavior calling `next()` on the way in and acting on the awaited result on the way out
- [ ] No reference to `OnIngress`/`OnEgress` remains in this file
- [ ] The corrected prose is consistent with the file's own "Composition Diagram" and "Registering Custom Behaviors" sections
