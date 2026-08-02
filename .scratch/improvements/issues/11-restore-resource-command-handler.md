## Labels

ready-for-agent

## What to build

The documented template-method base class for the common load-then-authorize-then-act command shape is commented out, along with a build-noise directive questioning whether to keep it.

Restore it against the post-migration vocabulary. Its existing test file should be reinstated alongside it.

## Acceptance criteria

- [ ] A handler deriving from the base class runs load, then authorize, then act, in that order
- [ ] A failed load short-circuits with the not-found case and never authorizes or acts
- [ ] A failed authorization short-circuits with the forbidden case and never acts
- [ ] The build-noise directive is gone
- [ ] Tests are red before green

## Blocked by

- 04-classifier-drives-transactions
