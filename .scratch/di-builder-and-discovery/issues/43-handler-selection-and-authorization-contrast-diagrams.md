## Labels

ready-for-agent

## What to build

`documentation/handler-selection-and-dispatch.md` currently has zero diagrams despite covering two genuinely decision/process-shaped topics in pure prose. Add two:

**(a) Handler-resolution decision tree - flowchart.** Diagram the rules from "Handler Selection Rules": a closed handler match beats an open generic match; namespace/assembly are never used as a tiebreaker; if more than one handler remains eligible after those rules, it's a build error unless an explicit override is declared (the "Ambiguity Is a Build Error (With an Override Escape Hatch)" section). Model it as a genuine decision tree - the reader should be able to trace "I have handlers X and Y for this request, which one wins?" down the diagram to an answer.

**(b) Authorization model contrast - two small sequence diagrams.** The doc already has a dedicated "Contrast: Resource vs. Non-Resource Authorization" prose section specifically because these two models get conflated. Diagram them side by side:
  - AuthorizationBehavior: pipeline-level, policy-based, runs pre-handler, short-circuits with `Forbidden` before the handler (or any inner behavior) ever runs.
  - ResourceCommandHandler's template method: load the entity first, authorize against the *loaded resource* (not just the request shape), then execute - short-circuits only after the load step, later in the sequence than AuthorizationBehavior's check.

  Each diagram should make clear *where* in its respective sequence the short-circuit can happen - that's the crux of the contrast.

Place (a) near "Handler Selection Rules" and (b) near "Contrast: Resource vs. Non-Resource Authorization".

**Verify by rendering** all three diagrams in both GitHub light and dark modes before calling this done.

## Acceptance criteria

- [ ] Handler-resolution flowchart covers closed-beats-open-generic, no namespace/assembly tiebreak, and ambiguity-is-a-build-error-unless-overridden
- [ ] Two sequence diagrams contrast AuthorizationBehavior vs. ResourceCommandHandler's template method, each showing its short-circuit point
- [ ] All three diagrams render correctly via `node tools/validate-mermaid.mjs` and are confirmed legible in both GitHub light and dark rendering
- [ ] Any manual coloring uses `classDef` with explicit `fill` and `color`
- [ ] Any prose that becomes redundant with the new diagrams is trimmed, not left duplicated

## Blocked by

None - can start immediately
