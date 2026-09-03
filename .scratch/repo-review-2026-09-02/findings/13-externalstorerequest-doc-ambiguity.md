Status: needs-triage
Severity: LOW
Category: documentation
Source: architecture.md lane, downgraded from MEDIUM to LOW by antagonist lane (documentation-precision issue about a deliberate, working design, not a functional defect)

# `IExternalStoreRequest`'s XML doc invites a use case its mechanism doesn't support

## Summary

The marker's doc comment invites use for "outbox, event bus, saga storage" durability, but the mechanism it actually drives (`UnitOfWorkBehavior` in both persistence packages) *excludes* the request from local transaction management entirely — the opposite of what the classic local-transactional-outbox pattern needs (the outbox row and the domain write must share one local transaction to be atomic).

## Evidence

`src/Interlocutor.Abstractions/Markers/IExternalStoreRequest.cs:3-20`'s first remarks paragraph: "Implement this marker interface on commands when the handler's response should be durably persisted to an external system (outbox, event bus, saga storage) after successful execution." The second paragraph, and both `UnitOfWorkBehavior` implementations' own comments, say the marker suppresses local transaction management for the entire request. A consumer who reaches for this marker because they read "outbox" and want atomicity between a domain write and an outbox-table insert would get no local transaction wrapping either write — silently defeating the atomicity the pattern exists to provide.

This is a documentation-precision issue about a deliberate, working design choice, not a functional defect — nothing breaks; a consumer who misreads the doc is one paragraph away from the correct explanation in the same doc comment.

## Suggested action

Rewrite the first remarks paragraph to explicitly rule out the local-transactional-outbox use case (or, if that use case genuinely doesn't need this marker, drop the "outbox" example and keep only "event bus, saga storage" or similar external-durability examples where the request has no local write to protect). Cross-reference `ITransactionalRequest` and `UnitOfWorkBehavior`'s own remarks so the two markers' relationship reads unambiguously from either side.
