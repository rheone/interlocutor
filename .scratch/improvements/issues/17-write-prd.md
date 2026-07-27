## Labels

ready-for-agent

## What to build

Write the Product Requirements Document (PRD) for Mediance. The source material is `bootstrapreference/PRD-mediator-result-pipeline.md` — a comprehensive design document that predates the implementation.

The PRD must be updated to reflect the actual implemented state of the project, not the original design. Key changes from the bootstrap PRD that must be reconciled:

1. **Package names**: `Mediance`/`Mediance.Mediation` became `Mediance.CommonKernel`/`Mediance.Abstractions`
2. **TransactionBehavior**: 3 type params → 2 type params with `Lazy<TContext>`
3. **Forbidden HTTP mapping**: `ObjectResult(ProblemDetails)` → `StatusCodeResult(403)` (empty body)
4. **BulkItemValidationBehavior**: fits `IPipelineBehavior<,>` with constrained `TRequest` parameter
5. **Missing features to document as future**: INotification, ISender/IPublisher split, LoggingBehavior, AuthorizationBehavior
6. **Remove references to unimplemented interfaces or features** that were planned but never built (unless they are in the future roadmap)

Place the PRD at `docs/PRD.md` and update `bootstrapreference/PRD-mediator-result-pipeline.md` to cross-reference the active PRD.

## Acceptance criteria

- [ ] PRD at `docs/PRD.md` covers what/why/how of the project
- [ ] All package names and APIs match actual source
- [ ] Future features clearly marked as "planned" with references to issues
- [ ] Incorrect/outdated design information removed or corrected
- [ ] Bootstrap PRD updated with reference to active PRD

## Blocked by

None — can start immediately
