## Labels

ready-for-agent

## What to build

The HTTP mapping covers controller-based endpoints only. Minimal APIs are the modern default and need a mapping to the result abstraction they use instead.

Add the minimal-API equivalent alongside the existing mapping, sharing the same status-code decisions so the two cannot drift.

## Acceptance criteria

- [ ] An operation outcome maps to a minimal-API result carrying the same status code its controller-based equivalent produces
- [ ] The status-code decision lives in one place, shared by both mappings
- [ ] Coverage matches the controller-based mapping, case type for case type
- [ ] Tests are red before green

## Blocked by

- 08-restore-http-mapping
