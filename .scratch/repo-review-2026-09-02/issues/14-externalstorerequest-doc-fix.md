# 14 - Clarify `IExternalStoreRequest`'s doc comment

**What to build:** `IExternalStoreRequest`'s XML doc no longer invites the local-transactional-outbox use case its mechanism (suppressing local transaction management entirely) can't actually support — a reader can no longer reasonably conclude this marker gives them outbox-row/domain-write atomicity.

Full technical detail: [findings/13-externalstorerequest-doc-ambiguity.md](../findings/13-externalstorerequest-doc-ambiguity.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] The first remarks paragraph either rules out the local-transactional-outbox use case explicitly, or drops the "outbox" example entirely in favor of examples where the request has no local write to protect
- [x] The doc cross-references `ITransactionalRequest` and `UnitOfWorkBehavior`'s own remarks so the relationship between the markers reads unambiguously

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. IExternalStoreRequest's XML doc now explicitly rules out the local-transactional-outbox use case and cross-references ITransactionalRequest and UnitOfWorkBehavior's own remarks. No source changes were needed this session.
