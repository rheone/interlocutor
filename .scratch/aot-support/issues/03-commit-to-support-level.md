# 03 - Commit to a support level and a container promise

Type: grilling
Status: resolved
Blocked by: 01, 02

## Question

Which support level does Interlocutor commit to, and what exactly does it promise across containers?

This is the decision the whole map exists to reach. The options as understood at charting time - the
research may reshape, merge, or eliminate them:

- **(A) Trim-compatible only.** Keep the analyzer-clean libraries and the annotated opt-in scanning
  fallback; drop the Native AOT claim and correct
  [ADR-0008](../../docs/adr/0008-compile-time-handler-discovery.md) accordingly.
- **(B) Generated closed pipeline composition.** Extend the generator to emit closed per-pair behavior
  registrations, unlocking real Native AOT without changing the response type.
- **(C) Reference-type outcomes.** Change `OperationOutcome<...>` from a value type to a reference type,
  sidestepping the instantiation problem at the cost of per-dispatch allocation and possibly the union
  design itself.
- **(D) A hybrid** - for instance trim-compatible everywhere plus AOT on an opt-in generated path.

The decision is not just the letter. It must also settle:

- **The container promise.** Microsoft DI and which others, at which level? Is "works on Microsoft DI,
  degrades gracefully elsewhere" acceptable, or must the promise be uniform? What is actually tested, and
  what is merely believed?
- **The failure mode when a consumer exceeds the promise.** A loud build-time diagnostic, a documented
  runtime exception, or silence. Today's behavior - a clean build followed by a throw on every dispatch -
  is the worst of the three and should not survive whichever option wins.
- **Cost tolerance.** Option C trades allocation for simplicity; option B trades generator complexity for
  keeping the union design. Which cost is this library willing to carry?

Bring the evidence from **Native AOT feasibility** and the prototype into the conversation; do not re-derive
them. Invoke `/grilling` and `/domain-modeling`.

## Answer

**Decided: stay on Option A (trim-compatible, "on the fly") as the shipped support level. Defer Option B
(a generated closed-registration catalog for true Native AOT) as a scoped, well-understood future effort -
not committed to now, not abandoned either.**

If Option B is ever pursued, it must be built as an **opt-in mode alongside** today's mechanism, never as
its replacement - registering both an open-generic and a closed descriptor for the same service type
re-triggers the exact AOT guard closed registration exists to avoid, so a real coexistence design has to
switch between the two per compilation, not add one on top of the other. This preserves
[ADR-0015](../../docs/adr/0015-pipeline-composition-behind-interlocutorpipelinebuilder.md)'s runtime-configurable
pipeline for every consumer who isn't specifically publishing Native AOT, rather than trading it away the
way martinothamar/Mediator did to ship the same capability.

Full reasoning, all three options (A, B, and the A-and-B hybrid) documented in depth, and what's proven
versus still a proposal for each: [ADR-0016](../../docs/adr/0016-trim-compatible-not-aot-compatible.md).
