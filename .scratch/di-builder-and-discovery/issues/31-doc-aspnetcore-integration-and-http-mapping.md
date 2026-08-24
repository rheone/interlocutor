> **Status: done** - merged in `a05405b` (tickets 28-32 landed together in this commit).

## Labels

ready-for-agent

## What to build

A `documentation/aspnetcore-integration.md` covering `Interlocutor.AspNetCore` - the current README's "ASP.NET Core Integration" section has no home elsewhere in this batch, and HTTP mapping is narrow enough in scope (one package) to deserve its own doc. Primary sources: ADR-0012 (HTTP-agnostic core with pluggable status mapping), `docs/PRD.md` §12 (HTTP mapping table).

Cover:

- **`ToActionResult()`** and the pluggable status-mapper it delegates to.
- **The mapping table** from each `OperationOutcome` failure case to an HTTP status/body - verify every case name and status code against current `src/Interlocutor.AspNetCore` source, not the bootstrap PRD's sketch (which used a different case-type set).
- **Why the core stays HTTP-agnostic**: this package is the *only* one in the solution permitted to reference an HTTP status code - restate that rule here explicitly, since every other documentation ticket in this batch is told not to reference HTTP status codes and to link here instead.

## Acceptance criteria

- [ ] `ToActionResult()` and the pluggable mapper are documented
- [ ] The failure-case-to-HTTP-status mapping table is verified against current `src/Interlocutor.AspNetCore` source
- [ ] The "only this package references HTTP status codes" rule is stated explicitly
- [ ] Every code example compiles against the shipped API
- [ ] Follows house conventions from ticket 21

## Blocked by

21 - Establish documentation/ directory; retire CONTEXT.md
