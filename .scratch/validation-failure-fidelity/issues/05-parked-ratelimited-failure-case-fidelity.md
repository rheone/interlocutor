# 05 - Parked: `RateLimited` failure-case fidelity

Status: needs-triage

## Labels

needs-triage

A specific `IRateLimiter` implementation - a distributed/Redis-backed limiter, a token-bucket algorithm with its own diagnostic shape - might want to expose more than the generic `RateLimited` struct's `LimitKey`/`RetryAfter`/`Message` (e.g. bucket size, tokens remaining, window boundaries). The same treatment given to validation in this feature (issues 01-04) would apply cleanly here: a technology-specific case type and companion behavior in whatever package ships that limiter, recognized generically by `DefaultOutcomeStatusMapper` via `IRateLimitedFailureCase`.

Note: `IRateLimitedFailureCase` itself already exists as of issue 08 - `RateLimited` implements it. This ticket is only the remaining work: a second implementation's own case type, its behavior, and the `DefaultOutcomeStatusMapper` branch recognizing the interface.

**Do not implement without explicit maintainer sign-off - there is no second concrete implementation driving this today.** `Interlocutor.Abstractions` ships exactly one `IRateLimiter`/`RateLimitedBehavior` pair; there is nothing to be agnostic *between* yet. If a developer wants this, follow the pattern in `maintainer_docs/adr/0020-validation-failure-case-fidelity.md` and issues 01-03 of `.scratch/validation-failure-fidelity/`.

## Blocked by

None - parked, not sequenced.
