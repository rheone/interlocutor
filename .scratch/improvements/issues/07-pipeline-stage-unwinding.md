## Labels

ready-for-agent

## What to build

The pipeline stage behavior runs each registered stage's before-hook in order, then the handler, then the after-hooks. Its contract documents that the after-hook runs even on failure.

It does not. If a stage's before-hook throws, the stages that already ran never receive their after-hook, so whatever they acquired leaks.

Make hook invocation symmetric: exactly those stages whose before-hook completed must receive an after-hook, in reverse order, regardless of how the pipeline exits.

The response parameter documentation is also wrong — it claims the value is null when the handler threw, but the response is a value type, so stages receive a zero-initialized value rather than null. Correct it, and remove the stale comment describing stack-ordered iteration that the code does not perform.

## Acceptance criteria

- [ ] Every stage whose before-hook completed receives an after-hook, in reverse order, on success, on a returned failure, and on an exception from the handler or from a later stage's before-hook
- [ ] Stages whose before-hook never ran do not receive an after-hook
- [ ] The original exception propagates unchanged, and a failing after-hook does not mask it
- [ ] Hook documentation matches actual behavior for the value-type response case
- [ ] Tests are written at the behavior seam, red before green

## Blocked by

None — can start immediately
