> **Status: done** - implemented alongside the other pipeline-overrides tickets in ADR-0019.

## Labels

ready-for-agent

## What to build

A developer who accidentally declares two override rules that both match the same request type gets a clear, named build-time error when opting into build validation - not silent, order-dependent behavior. This mirrors how the library already treats an ambiguous handler match as a build error rather than a silent pick.

There is no precedence-declaration mechanism in this ticket - two overlapping rules are always an error; a developer resolves it by narrowing one rule's match criteria, not by declaring which one wins. That escape hatch is deliberately out of scope unless real usage later demands it.

Document the new validation case alongside the library's other opt-in build validation checks.

## Acceptance criteria

- [ ] Two override rules whose match criteria both cover the same request type produce a validation problem when build validation is enabled, naming both rules
- [ ] A single request type matched by exactly one override rule produces no validation problem
- [ ] Validation is opt-in, consistent with the library's other build-time checks - it does not run unless requested
- [ ] `documentation/builder-and-registration.md` documents the new validation case alongside the existing ones

## Blocked by

01 - Core override mechanism (Microsoft DI, typed dispatch path)
