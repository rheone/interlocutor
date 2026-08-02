## Labels

ready-for-agent

## What to build

The validation integration package's behavior is entirely commented out — the file has zero non-comment lines, so the package ships as an empty assembly despite being documented as a supported integration.

Restore it against the post-migration vocabulary. It must emit the current validation aggregate type, not the removed pre-migration one.

The architecture test asserting this package's dependency direction is currently commented out with a note that it was disabled during migration. Re-enabling it is the marker that this slice is finished.

## Acceptance criteria

- [ ] A request with a failing validator short-circuits before the handler runs, and the handler is never invoked
- [ ] The failure carries every validator message, de-duplicated, not just the first
- [ ] A request with passing validators, or with no validators at all, reaches the handler unchanged
- [ ] The validation failure maps to the correct HTTP status through the restored mapping
- [ ] The disabled architecture test for this package is re-enabled and passes
- [ ] Tests are written at the behavior seam, red before green

## Blocked by

- 04-classifier-drives-transactions
- 08-restore-http-mapping
