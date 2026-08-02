## Labels

ready-for-agent

## What to build

The web package's entire contract — mapping an operation outcome to an HTTP action result — is commented out, so the package currently compiles to an assembly containing a public class with no members.

Restore it against the post-migration vocabulary, covering every current case type including the generic and aggregate forms.

## Acceptance criteria

- [ ] Every kernel case type maps to its documented status code, including generic and aggregate forms
- [ ] The success case carries the handler's value; failure cases carry their message or error detail
- [ ] Aggregate failures surface all their entries, not just the first
- [ ] The package no longer exposes an empty public type
- [ ] Tests assert the mapping for each case type, red before green

## Blocked by

- 02-conflicts-and-plural-accessors
