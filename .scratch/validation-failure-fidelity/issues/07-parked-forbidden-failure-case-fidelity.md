# 07 - Parked: `Forbidden` failure-case fidelity

Status: needs-triage

## Labels

needs-triage

A specific `IPolicyEvaluator` implementation might want to expose which policy (or policies) denied a request and why, beyond the generic `Forbidden` struct's plain `Reason` string. The same treatment given to validation in this feature (issues 01-04) would apply cleanly here: an evaluator-specific case type and companion behavior wherever that evaluator ships, recognized generically by `DefaultOutcomeStatusMapper` via `IForbiddenFailureCase`.

Note: `IForbiddenFailureCase` itself already exists as of issue 08 - `Forbidden` implements it. This ticket is only the remaining work: a second implementation's own case type, its behavior, and the `DefaultOutcomeStatusMapper` branch recognizing the interface.

**Do not implement without explicit maintainer sign-off - there is no second concrete implementation driving this today.** `Interlocutor.Abstractions` ships exactly one `IPolicyEvaluator`/`AuthorizationBehavior` pair; there is nothing to be agnostic *between* yet. If a developer wants this, follow the pattern in `maintainer_docs/adr/0020-validation-failure-case-fidelity.md` and issues 01-03 of `.scratch/validation-failure-fidelity/`.

## Blocked by

None - parked, not sequenced.
