# 04 - Write the ADR and reconcile ADR-0008's standing AOT claim

Type: task
Status: open
Blocked by: 03

## Question

Record the committed support level as an ADR, and settle what happens to the claim already on the books.

[ADR-0008](../../docs/adr/0008-compile-time-handler-discovery.md) asserts that compile-time handler
discovery "enables build-time diagnostics and AOT/trim compatibility". Depending on what
**Commit to a support level and container promise** decides, that sentence is either incomplete (discovery
was necessary but not sufficient - the pipeline path was never addressed) or wrong. Either way it cannot
stand unqualified.

ADRs in this repo are immutable once accepted: a changed decision gets a **new** ADR that supersedes the
old one, and existing ADRs are never edited to reverse a decision (see
[docs/README.md](../../docs/README.md)). So the work is:

- Write the new ADR stating the committed level, the container promise, and the reasoning - including the
  value-type open-generic constraint that forced the decision, so the next reader does not rediscover it
  the hard way.
- Decide the precise relationship to ADR-0008: full supersession, or a narrower qualification that leaves
  discovery's own claim intact while correcting the AOT half.
- Add it to the ADR index in [docs/README.md](../../docs/README.md) under the right theme heading.

Nothing to decide here beyond the supersession relationship - the substance comes from ticket 03. This is
the write-up.

## Answer

_Pending._
