# 03 - Commit to a support level and a container promise

Type: grilling
Status: open
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

_Pending._
