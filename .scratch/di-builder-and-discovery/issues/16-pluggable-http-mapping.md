> **Status: done** - merged in `72041db`.

## Labels

ready-for-agent

## What to build

Get the transport out of the core, and let a web developer choose their own status conventions without forking.

Two problems today. First, the core carries HTTP knowledge it should not: result type documentation and the glossary describe outcomes in terms of status codes, so a domain concept is defined by a transport. Second, the web package's mapping is hardcoded, so a team that maps a validation failure to one status rather than another has no way to say so. The library and its own documentation currently disagree about which status that is, which is what surfaced the problem.

Introduce a mapper abstraction in the web package that the conversion helpers delegate to, with the current mapping preserved as an overridable default so nothing changes for a developer who does not care. A developer who does care registers their own mapper and both the controller-based and minimal-API conversions follow it, so the two cannot drift.

The conversion helpers themselves stay - a separate, opt-in web package is exactly the right place for a transport conversion. What changes is that its opinion becomes replaceable.

Note that one current mapping deserves scrutiny while implementing: a plain failure maps to a server error status, meaning an ordinary expected failure surfaces as though the server malfunctioned.

## Acceptance criteria

- [ ] A mapper abstraction exists in the web package; both conversion helpers delegate to it
- [ ] The default mapper preserves current behavior for a developer who registers nothing
- [ ] A consumer-registered mapper is used by both the controller-based and minimal-API conversions
- [ ] No module outside the web package references a status code, in code or documentation
- [ ] The core's result type documentation describes outcomes without reference to a transport
- [ ] Custom and aggregate outcome cases are mappable, not only the built-in singular ones
- [ ] The plain-failure-to-server-error mapping is reviewed and its choice recorded

## Blocked by

01 - Reconcile ADRs and glossary
