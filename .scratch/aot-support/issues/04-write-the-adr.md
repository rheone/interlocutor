# 04 - Write the ADR and reconcile ADR-0008's standing AOT claim

Type: task
Status: resolved
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

Written as [ADR-0016: Trim-Compatible Now, Native AOT Deferred](../../docs/adr/0016-trim-compatible-not-aot-compatible.md).

**Relationship to ADR-0008: narrower qualification, not supersession.** ADR-0008's actual decision -
moving handler discovery to compile time, per-assembly modules, the generated dispatch switch, build-time
missing/ambiguous-handler diagnostics - is entirely unaffected and still Accepted. Only its Consequences'
"AOT- and trim-compatible" claim and its Decision's "publish ahead-of-time without warnings" claim were
wrong, because pipeline behaviors are closed by the container at dispatch time regardless of how the
handler itself was found - a different mechanism from the one ADR-0008 actually changed. A full
supersession would have implied the whole decision was being replaced, which it was not.

Followed the exact precedent already in this repo: ADR-0003 carries a `Status: Superseded by ADR-0008` line
plus a short pointer note explaining precisely what changed and what didn't. ADR-0008 now carries the same
shape - a pointer note atop the file, status left as `Accepted` since the decision itself stands, naming
ADR-0016 as the correction and stating in one sentence what was wrong.

Added to [docs/README.md](../../docs/README.md)'s ADR index under a new "Deployment: trimming & Native AOT"
theme, since none of the existing themes (result modeling, pipeline & behaviors, handler discovery &
dispatch, DI registration, mediator lifecycle, notifications, HTTP integration) fit a decision about
publish-time guarantees. ADR-0008's own index entry was corrected in the same pass - it carried the
identical overstated claim.
