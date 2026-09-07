# 02 - Write the ADR for ResourceCommandHandler's redesign (or record it as rejected)

**What to build:** Once issue 01's spike has a real blast-radius write-up, record the outcome as an ADR either way - accepting the redesign isn't the only valid result of a spike, and the decision not to proceed is exactly as worth recording as the decision to.

- If issue 01 confirms `Eject.WithCase` reaches `ResourceCommandHandler` cleanly (or with an identified, acceptable wrapper-side change): write a new ADR (`0025-resourcecommandhandler-eject-withcase.md`, the next available number after ADR-0024) recording the decision - `ResourceCommandHandler<TCommand, TEntity, TResponse>` generic over `TResponse : IUnion`, ejecting `NotFound`/`Forbidden` instead of fixing them into the shape, and the actual blast radius issue 01 measured. Cross-reference ADR-0023 (this extends the same `Eject.WithCase` mechanism to a second, non-behavior consumer) and note this removes the last shipped generic parameter named after a privileged "success" role, closing out `.scratch/outcome-naming-and-arity-refactor/issue 08`'s deliberate carve-out for this type.
- If issue 01 finds the mechanism doesn't reach this consumer without disproportionate change: no new ADR needed - record instead that the fallback is a plain rename, `TSuccess` -> `TResponse`, keeping the fixed `Outcome<TResponse, NotFound, Forbidden>` shape. Fold a short note into ADR-0023's (or ADR-0020's) Alternatives Considered recording what was spiked and why it stopped at a rename, so it isn't re-proposed without that context.

Nothing to decide in this ticket beyond which of the two outcomes applies - the substance comes from issue 01.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Issue 01's write-up is read and its recommendation is followed, not re-litigated in this ticket
- [ ] If committing to the redesign: new ADR added under `maintainer_docs/adr/`, cross-referencing ADR-0023, added to the ADR index
- [ ] If falling back to a rename: a short Alternatives Considered note added to the relevant existing ADR, citing issue 01's findings, and this ticket records "plain rename" as the shape issue 03 must implement
- [ ] `.scratch/resource-command-handler-case-fidelity/PRD.md`'s Status line is updated to reflect the outcome

## Comments
