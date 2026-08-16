## Labels

ready-for-agent

## What to build

Let a developer scope a behavior to a subset of requests without a runtime predicate and without a naming convention.

The library already ships the marker interfaces that express these subsets - auditable, requires-policy, transactional, external-store. What is missing is that the behaviors do not constrain themselves to them, so every behavior currently runs for every request and each must check at runtime whether it applies.

Express the subset as a generic constraint on the behavior's request type parameter. Both containers skip open generic registrations whose constraints do not hold, so filtering costs nothing at dispatch time and is verified by the compiler rather than by a string or a lambda. Apply this to the auditing, authorization, and both store-specific transaction behaviors.

The developer-visible outcome: adding the auditing behavior to the chain audits exactly the requests that opted in, and a request with no markers pays for nothing.

This also proves out the flat-ordered-filtered-list model from ticket 03: with constrained behaviors registered, different request types observe different chains drawn from the same list, in the same relative order.

## Acceptance criteria

- [ ] Auditing, authorization, and both transaction behaviors constrain their request type parameter to the appropriate marker
- [ ] A request without a marker never resolves the corresponding behavior, in both containers
- [ ] A request with a marker resolves it, and relative order against unconstrained behaviors is preserved
- [ ] Two different request types drawing different subsets from one registration list each observe the correct chain
- [ ] Registering a constrained behavior twice runs it twice for matching requests
- [ ] No behavior performs a runtime applicability check that the constraint now covers

## Blocked by

03 - Builder API, alongside the legacy overloads
