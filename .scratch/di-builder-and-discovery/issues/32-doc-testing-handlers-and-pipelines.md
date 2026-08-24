> **Status: done** - merged in `a05405b` (tickets 28-32 landed together in this commit).

## Labels

ready-for-agent

## What to build

A `documentation/testing-handlers-and-pipelines.md` covering how a consumer tests code built on Interlocutor - the current README's "Testing" section has no home elsewhere in this batch. Primary source: `docs/PRD.md` §16 (testing strategy).

Cover:

- **Unit-testing handlers directly**, without going through the mediator or pipeline at all - handlers are plain classes with injected dependencies.
- **Integration-testing pipeline composition**: verifying an explicitly-composed chain runs in the expected order, short-circuits correctly, and that transactional behavior actually commits/rolls back.
- **DI startup validation**: how a consumer's own test suite can lean on Interlocutor's fail-fast startup validation (missing handler registrations) rather than reinventing that check.

## Acceptance criteria

- [ ] Unit-testing a handler in isolation is demonstrated with a worked example
- [ ] Integration-testing a composed pipeline (order, short-circuit, transaction commit/rollback) is demonstrated
- [ ] Startup validation's role in a test suite is explained
- [ ] Every code example compiles against the shipped API and shipped test tooling (xunit.v3, NSubstitute, per this repo's own conventions)
- [ ] Follows house conventions from ticket 21

## Blocked by

21 - Establish documentation/ directory; retire CONTEXT.md
